using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            private Hashtable padInts = new Hashtable();

            public IPadInt store(int uid) {
                if (!padInts.Contains(uid)) {
                    PadInt obj = new PadInt();
                    padInts.Add(uid, obj);
                    return obj;
                }
                return null;
            }

            public IPadInt load(int uid) {
                if (padInts.Contains(uid)) {
                    return (IPadInt) padInts[uid];
                }
                return null;
            }
        }


        class Program {
            static void Main(string[] args) {
            }
        }
    }
}