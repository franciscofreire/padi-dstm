using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;

namespace PADI_DSTM {
    namespace DataServer {
        class ServerTransaction {
            private String Client;
            private Hashtable copies;

            public ServerTransaction(String Client, PadInt Obj) {
                this.Client = Client;
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


        class PadInt : MarshalByRefObject, IPadInt {
            private int value;

            public int Read() {
                //TODO: Transaction control
                // Se Cliente que me contactou nao esta numa transacao
                //   registo-me junto do coordenador
                //
                return value;
            }

            public void Write(int value) {
                //TODO: Transaction control
                // Se Cliente que me contactou nao esta numa transacao
                //   registo-me junto do coordenador
                this.value = value;
            }

            public int Value {
                get {
                    return Value;
                }
                set {
                    Value = value;
                }
            }
        }

        class Server : MarshalByRefObject, IDataServer {

            private bool _isFail;
            private bool _isFreeze;
            private String _name;

            private Hashtable padInts = new Hashtable();

            // TODO: I need to have a log, for Freeze+Recover commands!!!

            public Server(String name) {
                _name = name;
                _isFail = false;
                _isFreeze = false;
            }

            public String name {
                get { return _name; }
                set { _name = value; }
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
                    Console.WriteLine("[!STORE] Error: DataServer " + name + " is set to [Fail] mode!");
                    Console.WriteLine("---");
                    
                    return null;

                } else if (isFreeze) {
                    Console.WriteLine("[!STORE] Error: DataServer " + name + " is set to [Freeze] mode!");
                    Console.WriteLine("---");
                    return null;

                } else {
                    if (!padInts.Contains(uid)) {
                        
                        PadInt obj = new PadInt();
                        padInts.Add(uid, obj);
                        Console.WriteLine("[STORE] DataServer " + name + " stored PadInt " + uid);
                        Console.WriteLine("---");
                        return obj;
                    }
                    Console.WriteLine("[!STORE] DataServer " + name + " cannot store PadInt " + uid + ": already exists.");
                    Console.WriteLine("---");
                    return null;
                }
            }


            public IPadInt load(int uid) {
                if (isFail) {
                    Console.WriteLine("[!LOAD] Error: DataServer " + name + " is set to [Fail] mode!");
                    Console.WriteLine("---");
                    return null;

                } else if (isFreeze) {
                    Console.WriteLine("[!LOAD] Error: DataServer " + name + " is set to [Freeze] mode!");
                    Console.WriteLine("---");
                    return null;

                } else {
                    if (padInts.Contains(uid)) {
                        Console.WriteLine("[LOAD] DataServer " + name + " load PadInt " + uid);
                        Console.WriteLine("---");
                        return (IPadInt)padInts[uid];
                        
                    }
                    Console.WriteLine("[!LOAD] DataServer " + name + " cannot load PadInt " + uid + ": does not exist.");
                    Console.WriteLine("---");
                    return null;
                }
            }
        }


        class Program {

            // Como identificar um server
            // Variar endpoint, porta ou ambos?
            static void Main(string[] args) {
                int port = 9995;
                TcpChannel channel = new TcpChannel(port);
                ChannelServices.RegisterChannel(channel, true);

                String name1 = "DataServer1";
                String name2 = "DataServer2";

                Server server = new Server(name1);
                RemotingServices.Marshal(server, name1, typeof(IDataServer));
                System.Console.WriteLine("Started " + name1 + "...");
                String url = "tcp://localhost:" + port + "/" + name1;

                Server server2 = new Server(name2);
                RemotingServices.Marshal(server2, name2, typeof(IDataServer));
                String url2 = "tcp://localhost:" + port + "/" + name2;
                System.Console.WriteLine("Started " + name2 + "...");
                Console.WriteLine("---");

                String urlMaster = "tcp://localhost:9999/MasterServer";
                IMasterServer masterServer = (IMasterServer) Activator.GetObject(typeof(IMasterServer), urlMaster);
                masterServer.registerServer(url);
                masterServer.registerServer(url2);

                System.Console.ReadKey();
            }
        }
    }
}