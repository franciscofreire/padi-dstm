﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace PADI_DSTM {
    
    namespace DataServer {

        public delegate Lock AsyncGetLockCaller(LockType type, int padIntId, int transactionId);

        public class PadInt : MarshalByRefObject, IPadInt {

            private int id;
            private int value;
            private Server myServer;

            public int Value {
                get { return value; }
                set { this.value = value; }
            }

            public int Id {
                get { return id; }
            }

            public PadInt(int id, Server srv) {
                this.myServer = srv;
                this.id = id;
                this.value = 0;
            }

            public void Write(int txId, int value) {
                lock (myServer.StateLockObj) {
                    if (myServer.isFail) {
                        Console.WriteLine("[!WRITE] Error: DataServer " + myServer.name + " is set to [Fail] mode!");
                        Console.WriteLine("---");
                        while (true) ;
                    } else if (myServer.isFreeze) {
                        lock (SingletonCounter.Instance) {
                            SingletonCounter.Instance.incrementLockCounter();
                            Monitor.Wait(SingletonCounter.Instance);
                        }
                        Console.WriteLine("[!WRITE] Error: DataServer " + myServer.name + " is set to [Freeze] mode!");
                        Console.WriteLine("---");
                    }

                }
                ServerTransaction transaction = null;
                if (!myServer.Transactions.ContainsKey(txId)) {
                    transaction = new ServerTransaction(txId, this);
                    myServer.Transactions.Add(txId, transaction);
                    myServer.MasterServer.join(txId, myServer.URL);
                }
                transaction = myServer.Transactions[txId];
                Console.WriteLine("[Write] Tx{0} is Trying to acquire lock for PadInt {1}",
                txId, id);

                
                myServer.lockManager.setLock(this.id, txId, LockType.EXCLUSIVE);
                
                // to do in abort case
                //myServer.MasterServer.TxAbort(txId);
                //throw new TxException(txId, "Transaction abort on write due to Deadlock");

                Console.WriteLine("[Write] Tx{0} Acquired lock for PadInt {1}",
                    txId, id);

                // if not yet saved, save it for future rollback
                if (!transaction.containsPadInt(this)) {
                    transaction.Add(this);
                }
                this.Value = value;
                Console.WriteLine("[Write] Transaction " + txId + " created in " + myServer.name);

            }

            public int Read(int txId) {
                lock (myServer.StateLockObj) {
                    if (myServer.isFail) {
                        Console.WriteLine("[!READ] Error: DataServer " + myServer.name + " is set to [Fail] mode!");
                        Console.WriteLine("---");
                        while (true) ;

                    } else if (myServer.isFreeze) {
                        lock (SingletonCounter.Instance) {
                            SingletonCounter.Instance.incrementLockCounter();
                            Monitor.Wait(SingletonCounter.Instance);
                        }
                        Console.WriteLine("[!READ] Error: DataServer " + myServer.name + " is set to [Freeze] mode!");
                        Console.WriteLine("---");
                    }
                }

                ServerTransaction transaction = null;

                if (!myServer.Transactions.ContainsKey(txId)) {
                    transaction = new ServerTransaction(txId, this);
                    myServer.Transactions.Add(txId, transaction);
                    myServer.MasterServer.join(txId, myServer.URL);
                    Console.WriteLine("[Read] Transaction " + txId + " created in " + myServer.name);
                } else {
                    transaction = myServer.Transactions[txId];
                }

                Console.WriteLine("[Read] Tx{0} is Trying to acquire lock for PadInt {1}",
                txId, id);
                
                myServer.lockManager.setLock(this.id, txId, LockType.SHARED);

                Console.WriteLine("[Read] Tx{0} Acquired lock for PadInt {1}",
                    txId, id);

                return this.value;

            }
            public override object InitializeLifetimeService() {
                return null;
            }
        }
    }
}
