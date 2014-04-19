using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Threading;
using System.Net.Sockets;

namespace PADI_DSTM {

    namespace DataServer {

        class PadInt : MarshalByRefObject, IPadInt {

            private int value = 0;
            private Server myServer;

            public int Value {
                get { return value; }
                set { this.value = value; }
            }

            public PadInt(Server srv) {
                this.myServer = srv;
            }

            public void Write(int txId, int value) {
                if (myServer.isFail) {
                    Console.WriteLine("[!WRITE] Error: DataServer " + myServer.name + " is set to [Fail] mode!");
                    Console.WriteLine("---");
                    while (true) ;
                    //throw new RemotingException("Server is in Fail Mode");

                } else if (myServer.isFreeze) {
                    lock (SingletonCounter.Instance) {
                        SingletonCounter.Instance.incrementLockCounter();
                        Monitor.Wait(SingletonCounter.Instance);
                    }
                    Console.WriteLine("[!WRITE] Error: DataServer " + myServer.name + " is set to [Freeze] mode!");
                    Console.WriteLine("---");
                }
                if (!myServer.Transactions.ContainsKey(txId)) {
                    ServerTransaction transaction = new ServerTransaction(txId, this);
                    myServer.Transactions.Add(txId, transaction);
                    myServer.MasterServer.join(txId, myServer.URL);
                    transaction.Set(this, value);
                    Console.WriteLine("[Write] Transaction " + txId + " created in " + myServer.name);

                } else if (!(((ServerTransaction)myServer.Transactions[txId]).Copies.ContainsKey(this))) {
                    ServerTransaction tr = (ServerTransaction)myServer.Transactions[txId];
                    tr.Add(this);
                    tr.Set(this, value);
                } else {
                    ServerTransaction tr = (ServerTransaction)myServer.Transactions[txId];
                    tr.Set(this, value);
                }

            }

            public int Read(int txId) {
                if (myServer.isFail) {
                    Console.WriteLine("[!READ] Error: DataServer " + myServer.name + " is set to [Fail] mode!");
                    Console.WriteLine("---");
                    while (true) ;
                    //throw new RemotingException("Server is in Fail Mode");

                } else if (myServer.isFreeze) {
                    lock (SingletonCounter.Instance) {
                        SingletonCounter.Instance.incrementLockCounter();
                        Monitor.Wait(SingletonCounter.Instance);
                    }
                    Console.WriteLine("[!READ] Error: DataServer " + myServer.name + " is set to [Freeze] mode!");
                    Console.WriteLine("---");
                }

                if (!myServer.Transactions.ContainsKey(txId)) {
                    ServerTransaction transaction = new ServerTransaction(txId, this);
                    myServer.Transactions.Add(txId, transaction);
                    myServer.MasterServer.join(txId, myServer.URL);
                    Console.WriteLine("[Read] Transaction " + txId + " created in " + myServer.name);
                    return this.value;
                } else if (!(((ServerTransaction)myServer.Transactions[txId]).Copies.ContainsKey(this))) {
                    ServerTransaction tr = (ServerTransaction)myServer.Transactions[txId];
                    tr.Add(this);
                    return this.value;
                } else {
                    ServerTransaction tr = (ServerTransaction)myServer.Transactions[txId];
                    return (int)tr.Copies[this];
                }

            }
            public override object InitializeLifetimeService() {
                return null;
            }
        }

        class ServerTransaction {

            private Hashtable copies;
            private int txId;
            private bool abort;

            public ServerTransaction(int txId, PadInt Obj) {
                this.txId = txId;
                copies = new Hashtable();
                copies.Add(Obj, Obj.Value);
                abort = false;
            }

            public Hashtable Copies {
                get { return copies; }
                set { copies = value; }
            }

            public bool Abort {
                get { return abort; }
                set { abort = value; }
            }

            public void Add(PadInt Obj) {
                copies.Add(Obj, Obj.Value);
            }

            public void Set(PadInt Obj, int value) {
                copies[Obj] = value;
            }

        }


        class SingletonCounter {

            private int lockcounter;

            public int Lockcounter {
                get { return lockcounter; }
                set { lockcounter = value; }
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

            public void incrementLockCounter() {
                this.lockcounter++;
            }

            public void decrementLockCounter() {
                this.lockcounter--;
            }

        }

        enum State { Normal, Freeze, Fail }

        public class Server : MarshalByRefObject, IDataServer {

            private State _state;
            private String _name;
            private String _url;
            IMasterServer _masterServer;

            private const String urlMaster = "tcp://localhost:9999/MasterServer";

            private Hashtable transactions = new Hashtable();
            private Hashtable padInts = new Hashtable();

            public IMasterServer MasterServer {
                get { return _masterServer; }
            }

            public Hashtable Transactions {
                get { return transactions; }

            }

            public Server(String name, String url) {
                _name = name;
                _url = url;
                _masterServer = (IMasterServer)Activator.GetObject(typeof(IMasterServer), urlMaster);
                _state = State.Normal;
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
                get { return _state == State.Fail; }
            }

            public bool isNormal {
                get { return _state == State.Normal; }
            }

            public bool isFreeze {
                get { return _state == State.Freeze; }
            }

            public bool Freeze() {
                if (isFail) {
                    while (true) ;
                    //throw new RemotingException("Server is in Fail Mode");
                }
                if (isNormal) {
                    _state = State.Freeze;
                    Console.WriteLine("[FREEZE] Dataserver " + _name + " changed to [Freeze].");
                    Console.WriteLine("---");
                    return true;
                }
                return false;
            }

            public bool Fail() {
                if (isFreeze) {
                    lock (SingletonCounter.Instance) {
                        SingletonCounter.Instance.incrementLockCounter();
                        Monitor.Wait(SingletonCounter.Instance);
                    }
                }
                if (isNormal) {
                    _state = State.Fail;
                    Console.WriteLine("[FAIL] Dataserver " + _name + " changed to [Fail].");
                    Console.WriteLine("---");
                    return true;
                } else {
                    return false;
                }
            }

            public bool Recover() {
                if (isNormal) {
                    return false;
                } else if (isFreeze) {
                    lock (SingletonCounter.Instance) {
                        while (SingletonCounter.Instance.Lockcounter > 0) {
                            SingletonCounter.Instance.decrementLockCounter();
                            Monitor.Pulse(SingletonCounter.Instance);
                        }
                    }
                }
                _state = State.Normal;
                Console.WriteLine("[RECOVER] Dataserver " + _name + " changed to [OK].");
                Console.WriteLine("---");
                return true;
            }

            public String Status() {
                if (isFail) {
                    Console.WriteLine(_name + " Status: [Fail].");
                    return _name + " Status: [Fail].";
                } else if (isFreeze) {
                    Console.WriteLine(_name + " Status: [Freeze].");
                    return _name + " Status: [Freeze].";
                } else {
                    Console.WriteLine(_name + " Status: [OK].");
                    return _name + " Status: [OK].";
                }
            }

            public IPadInt store(int uid) {
                if (isFail) {
                    Console.WriteLine("[!STORE] Error: DataServer " + name + " is set to [Fail] mode!");
                    Console.WriteLine("---");
                    while (true) ;
                    //throw new RemotingException("Server is in Fail Mode");

                } else if (isFreeze) {
                    lock (SingletonCounter.Instance) {
                        SingletonCounter.Instance.incrementLockCounter();
                        Monitor.Wait(SingletonCounter.Instance);
                    }
                }
                if (!padInts.Contains(uid)) {

                    PadInt obj = new PadInt(this);
                    padInts.Add(uid, obj);
                    Console.WriteLine("[STORE] DataServer " + name + " stored PadInt " + uid);
                    Console.WriteLine("---");
                    return obj;
                }
                return null;
            }


            public IPadInt load(int uid) {
                if (isFail) {
                    Console.WriteLine("[!LOAD] Error: DataServer " + name + " is set to [Fail] mode!");
                    Console.WriteLine("---");
                    while (true) ;
                    //throw new RemotingException("Server is in Fail Mode");

                } else if (isFreeze) {
                    lock (SingletonCounter.Instance) {
                        SingletonCounter.Instance.incrementLockCounter();
                        Monitor.Wait(SingletonCounter.Instance);
                    }
                }
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



            // 2PC INTERFACE COMMANDS

            // commando para ele se juntar aos participantes de uma transacção!!!
            // aqui ou no master? :\


            public bool canCommit(int TxId) {
                if (isFail) {
                    Console.WriteLine("[!canCommit] Error: DataServer " + name + " is set to [Fail] mode!");
                    Console.WriteLine("---");
                    while (true) ;
                    //throw new RemotingException("Server is in Fail Mode");

                } else if (isFreeze) {
                    lock (SingletonCounter.Instance) {
                        SingletonCounter.Instance.incrementLockCounter();
                        Monitor.Wait(SingletonCounter.Instance);
                    }
                }
                Console.WriteLine("[canCommit] Master Request with id " + TxId);
                Console.WriteLine("---");

                if (!this.Transactions.ContainsKey(TxId)) {
                    Console.WriteLine("Error transaction not found");
                    return false;

                }


                return true;
            }

            public bool doCommit(int TxId) {
                if (isFail) {
                    Console.WriteLine("[!doCommit] Error: DataServer " + name + " is set to [Fail] mode!");
                    Console.WriteLine("---");
                    while (true) ;
                    //throw new RemotingException("Server is in Fail Mode");

                } else if (isFreeze) {
                    lock (SingletonCounter.Instance) {
                        SingletonCounter.Instance.incrementLockCounter();
                        Monitor.Wait(SingletonCounter.Instance);
                    }
                }
                Console.WriteLine("[doCommit] Master Request with id " + TxId);
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

                return true;
            }

            public bool doAbort(int TxId) {
                if (isFail) {
                    Console.WriteLine("[!doAbort] Error: DataServer " + name + " is set to [Fail] mode!");
                    Console.WriteLine("---");
                    while (true) ;
                    //throw new RemotingException("Server is in Fail Mode");

                } else if (isFreeze) {
                    lock (SingletonCounter.Instance) {
                        SingletonCounter.Instance.incrementLockCounter();
                        Monitor.Wait(SingletonCounter.Instance);
                    }
                }
                Console.WriteLine("[doAbort] Master Request with id " + TxId);
                Console.WriteLine("---");

                if (!this.Transactions.ContainsKey(TxId)) {
                    Console.WriteLine("Error transaction not found : " + TxId);
                    return false;
                }

                transactions.Remove(TxId);
                return true;
            }

            public bool haveCommited(int TxId) {
                if (isFail) {
                    Console.WriteLine("[!haveCommited] Error: DataServer " + name + " is set to [Fail] mode!");
                    Console.WriteLine("---");
                    while (true) ;
                    //throw new RemotingException("Server is in Fail Mode");

                } else if (isFreeze) {
                    lock (SingletonCounter.Instance) {
                        SingletonCounter.Instance.incrementLockCounter();
                        Monitor.Wait(SingletonCounter.Instance);
                    }
                }
                Console.WriteLine("[haveCommitted] Master Request with id " + TxId);
                Console.WriteLine("---");
                return true;
            }

            public override object InitializeLifetimeService() {
                return null;
            }

        }

        class Program {

            static void Main(string[] args) {
                int port = 0;
                TcpChannel channel = null;

                if (args.Count() == 0) {
                    Console.WriteLine("Invoked with no argumments.");
                    Console.WriteLine("Trying to assign port in range 2001...65535");
                    port = 2001;
                    while (channel == null) {
                        try {
                            channel = new TcpChannel(port);
                        } catch (SocketException) {
                            port++;
                        }
                    }
                } else if (args.Count() == 1) {
                    try {
                        port = Convert.ToInt32(args[0]);
                        channel = new TcpChannel(port);
                    } catch (FormatException fe) {
                        Console.WriteLine("Malformed Args: " + args[0]);
                        Console.WriteLine(fe);
                        Console.WriteLine("Press any key to exit...");
                        Console.ReadKey();
                        return;
                    }
                } else {
                    Console.WriteLine("usage: DataServer.exe " + "<port>");
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                    return;
                }


                ChannelServices.RegisterChannel(channel, false);
                String name = "Server";
                String url = "tcp://localhost:" + port + "/" + name;

                String ServerName = name + "At" + port;
                Server server = new Server(ServerName, url);
                RemotingServices.Marshal(server, name, typeof(IDataServer));
                server.register();
                Console.WriteLine("Started " + ServerName + "...");
                Console.WriteLine("---");
                Console.ReadKey();
            }
        }
    }
}