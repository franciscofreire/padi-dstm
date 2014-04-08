using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Threading;

namespace PADI_DSTM {

    namespace DataServer {
        class PadInt : MarshalByRefObject, IPadInt {


            private int value = 0;
            private Server myServer;

            public PadInt(Server srv) {
                this.myServer = srv;
            }

            public void Write(int txId, int value) {

                if (!myServer.Transactions.ContainsKey(txId)) {
                    ServerTransaction transaction = new ServerTransaction(txId, this);
                    myServer.Transactions.Add(txId, transaction);
                    myServer.MasterServer.join(txId, myServer.URL);
                    transaction.Set(this, value);
                    Console.WriteLine("Transaction :" + txId + "created");

                } else {
                    ServerTransaction tr = (ServerTransaction)myServer.Transactions[txId];
                    tr.Set(this, value);
                }

            }

            public int Value {
                get {
                    return value;
                }
                set {
                    this.value = value;
                }
            }


            public int Read(int txId) {
                //TODO: Transaction control
                // Se Cliente que me contactou nao esta numa transacao
                //   registo-me junto do coordenador
                //
                if (!myServer.Transactions.ContainsKey(txId)) {
                    ServerTransaction transaction = new ServerTransaction(txId, this);
                    myServer.Transactions.Add(txId, transaction);
                    myServer.MasterServer.join(txId, myServer.URL);
                    Console.WriteLine("Transaction " + txId + " created");
                    return this.value;
                } else {
                    ServerTransaction tr = (ServerTransaction)myServer.Transactions[txId];
                    return (int)tr.Copies[this];
                }

            }
            public class ServerTransaction {

                private Hashtable copies;

                public Hashtable Copies {
                    get { return copies; }
                    set { copies = value; }
                }
                private int txId;

                public ServerTransaction(int txId, PadInt Obj) {
                    this.txId = txId;
                    copies = new Hashtable();
                    copies.Add(Obj, Obj.Value);
                }

                public void Add(PadInt Obj) {
                    copies.Add(Obj, Obj.Value);
                }

                public void Set(PadInt Obj, int value) {
                    copies[Obj] = value;
                }
            }


            public class SingletonCounter {

                private int lockcounter;

                public int Lockcounter {
                    get { return lockcounter; }
                    set { lockcounter = value; }
                }

                public void incrementLockCounter() {
                    this.lockcounter++;
                }

                public void decrementLockCounter() {
                    this.lockcounter--;
                }


                private static SingletonCounter instance;
                private SingletonCounter() { }

                public static SingletonCounter Instance {
                    get {
                        if (instance == null) {
                            instance = new SingletonCounter();
                        }

                        return instance;
                    }



                }



            }

            public class Server : MarshalByRefObject, IDataServer {


                private bool _isFail;
                private bool _isFreeze;
                private String _name;
                private String _url;
                IMasterServer _masterServer;
                readonly object key = new object();

                private const String urlMaster = "tcp://localhost:9999/MasterServer";

                public object Key {
                    get { return key; }
                }

                public IMasterServer MasterServer {
                    get { return _masterServer; }
                }

                bool block = true;

                public bool Block {
                    get { return block; }
                    set { block = value; }
                }


                private Hashtable transactions = new Hashtable();

                public Hashtable Transactions {
                    get { return transactions; }

                }
                private Hashtable padInts = new Hashtable();

                public Server(String name, String url) {
                    _name = name;
                    _url = url;
                    _isFail = false;
                    _isFreeze = false;
                    _masterServer = (IMasterServer)Activator.GetObject(typeof(IMasterServer), urlMaster);
                }

                public void register() {
                    _masterServer.registerServer(_url);
                }

                public String name {
                    get { return _name; }
                    set { _name = value; }
                }

                public String URL {
                    get { return _url; }
                    set { _url = value; }
                }

                public bool isFail {
                    get { return _isFail; }
                    set { _isFail = value; }
                }

                public bool isFreeze {
                    get { return _isFreeze; }
                    set { _isFreeze = value; }
                }

                public bool Freeze() {

                    isFreeze = true;
                    Console.WriteLine("[STATUS] Dataserver " + _name + " changed to [Freeze].");
                    Console.WriteLine("---");
                    return true;
                }

                public bool Fail() {
                    isFail = true;
                    Console.WriteLine("[STATUS] Dataserver " + _name + " changed to [Fail].");
                    Console.WriteLine("---");
                    return true;
                }

                public bool Recover() {
                    if (isFail) {
                        isFail = false;
                        Console.WriteLine("[STATUS] Dataserver " + _name + " is recovered, changed to [OK].");
                        Console.WriteLine("---");
                        return true;
                    } else if (isFreeze) {
                        Console.WriteLine("Entrei aqui");



                        lock (key) {
                            Console.WriteLine("YUPI");
                            block = false;
                            Monitor.Pulse(key);
                        }

                        isFreeze = false;
                        Console.WriteLine("[STATUS] Dataserver " + _name + " is recovered, changed to [OK].");
                        // TODO: Read and dispatch logged requests
                        Console.WriteLine("---");
                        return true;
                    } else {
                        return false;
                    }
                }

                public String Status() {
                    if (isFail) {
                        return "[Fail]";
                    } else if (isFreeze) {
                        return "[Freeze]";
                    } else {
                        return "[OK]";
                    }
                }


                public IPadInt store(int uid) {
                    if (isFail) {
                        //terminar thread 
                        Console.WriteLine("[!STORE] Error: DataServer " + name + " is set to [Fail] mode!");
                        Console.WriteLine("---");

                        return null;

                    } else if (isFreeze) {
                        lock (key) {
                            Console.WriteLine("Dentro do store");

                            while (block)
                                Monitor.Wait(key);

                            block = true;

                        }

                        Console.WriteLine("[!STORE] Error: DataServer " + name + " is set to [Freeze] mode!");
                        Console.WriteLine("---");
                        return null;

                    } else {
                        if (!padInts.Contains(uid)) {

                            PadInt obj = new PadInt(this);
                            padInts.Add(uid, obj);
                            Console.WriteLine("[STORE] DataServer " + name + " stored PadInt " + uid);
                            Console.WriteLine("---");
                            return obj;
                        }
                        // este caso nunca acontece (o master testa o mesmo)
                        Console.WriteLine("[!STORE] DataServer " + name + " cannot store PadInt " + uid + ": already exists.");
                        Console.WriteLine("---");
                        return null;
                    }
                }


                public IPadInt load(int uid) {
                    if (isFail) {
                        //terminar thread
                        Thread.CurrentThread.Abort();
                        Console.WriteLine("[!LOAD] Error: DataServer " + name + " is set to [Fail] mode!");
                        Console.WriteLine("---");
                        Thread.CurrentThread.Abort();
                        return null;
                    } else if (isFreeze) {
                        lock (SingletonCounter.Instance) {
                            SingletonCounter.Instance.incrementLockCounter();
                            Monitor.Wait(SingletonCounter.Instance);
                        }

                        Console.WriteLine("[!LOAD] Error: DataServer " + name + " is set to [Freeze] mode!");
                        Console.WriteLine("---");
                        return null;

                    } else {
                        if (padInts.Contains(uid)) {
                            Console.WriteLine("[LOAD] DataServer " + name + " load PadInt " + uid);
                            Console.WriteLine("---");
                            return (IPadInt)padInts[uid];

                        }
                        // este caso nunca acontece (o master testa o mesmo)
                        Console.WriteLine("[!LOAD] DataServer " + name + " cannot load PadInt " + uid + ": does not exist.");
                        Console.WriteLine("---");
                        return null;
                    }
                }



                // 2PC INTERFACE COMMANDS

                // commando para ele se juntar aos participantes de uma transacção!!!
                // aqui ou no master? :\


                public bool canCommit(int TxId) {
                    Console.WriteLine("[canCommit] Master Request with id: ." + TxId);
                    Console.WriteLine("---");
                    if (!this.Transactions.ContainsKey(TxId)) {
                        Console.WriteLine("Error transaction not found");
                        return false;

                    }


                    return true;
                }

                public bool doCommit(int TxId) {
                    Console.WriteLine("[doCommit] Master Request. with id: " + TxId);
                    Console.WriteLine("---");
                    if (!this.Transactions.ContainsKey(TxId)) {
                        Console.WriteLine("Error transaction not found : " + TxId);
                        return false;
                    }

                    ServerTransaction transaction = (ServerTransaction)transactions[TxId];

                    foreach (DictionaryEntry pair in transaction.Copies) {
                        PadInt padInt = (PadInt)pair.Key;
                        int value = (int)pair.Value;
                        padInt.Value = value;
                    }

                    return false;
                }

                public bool doAbort(int TxId) {
                    Console.WriteLine("[doAbort] Master Request. id: " + TxId);
                    Console.WriteLine("---");

                    if (!this.Transactions.ContainsKey(TxId)) {
                        Console.WriteLine("Error transaction not found : " + TxId);
                        return false;
                    }

                    transactions.Remove(TxId);
                    return true;
                }

                public bool haveCommited(int TxId) {
                    Console.WriteLine("[haveCommitted] Master Request." + TxId);
                    Console.WriteLine("---");
                    return true;
                }

            }

            class Program {
                
                static void Main(string[] args) {
                    int port = 9995;
                    TcpChannel channel = new TcpChannel(port);
                    ChannelServices.RegisterChannel(channel, true);

                    String name1 = "DataServer1";
                    String name2 = "DataServer2";
                    String url1 = "tcp://localhost:" + port + "/" + name1;
                    String url2 = "tcp://localhost:" + port + "/" + name2;

                    Server server = new Server(name1, url1);
                    RemotingServices.Marshal(server, name1, typeof(IDataServer));
                    server.register();
                    System.Console.WriteLine("Started " + name1 + "...");

                    Server server2 = new Server(name2, url2);
                    RemotingServices.Marshal(server2, name2, typeof(IDataServer));
                    server2.register();
                    System.Console.WriteLine("Started " + name2 + "...");
                    Console.WriteLine("---");
                    System.Console.ReadKey();
                }
            }
        }
    }
}