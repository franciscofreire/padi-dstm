﻿using System;
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
                return true;
            }

            public bool Fail() {
                isFail = true;
                return true;
            }

            public bool Recover() {
                if (isFail) {
                    isFail = false;
                    return true;
                } else if (isFreeze) {
                    isFreeze = false;
                    // TODO: Read and dispatch logged requests
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
                    Console.WriteLine("DataServer " + name + " is set to Fail Mode!");
                    return null;

                } else if (isFreeze) {
                    Console.WriteLine("DataServer " + name + " is set to Freeze Mode!");
                    return null;

                } else {
                    if (!padInts.Contains(uid)) {
                        PadInt obj = new PadInt();
                        padInts.Add(uid, obj);
                        return obj;
                    }
                    return null;
                }
            }


            public IPadInt load(int uid) {
                if (isFail) {
                    Console.WriteLine("DataServer " + name + " is set to Fail Mode!");
                    return null;

                } else if (isFreeze) {
                    Console.WriteLine("DataServer " + name + " is set to Freeze Mode!");
                    return null;

                } else {
                    if (padInts.Contains(uid)) {
                        return (IPadInt)padInts[uid];
                    }
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
                System.Console.WriteLine("Started " + name1);
                String url = "tcp://localhost:" + port + "/" + name1;

                Server server2 = new Server(name2);
                RemotingServices.Marshal(server2, name2, typeof(IDataServer));
                String url2 = "tcp://localhost:" + port + "/" + name2;
                System.Console.WriteLine("Started " + name2);

                String urlMaster = "tcp://localhost:9999/MasterServer";
                IMasterServer masterServer = (IMasterServer) Activator.GetObject(typeof(IMasterServer), urlMaster);
                masterServer.registerServer(url);
                masterServer.registerServer(url2);

                System.Console.ReadKey();
            }
        }
    }
}