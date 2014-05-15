using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace PADI_DSTM {

    namespace DataServer {
        
        public enum LockType { SHARED, EXCLUSIVE };

        public class Lock {
            private int padIntId;
            private List<int> holdersTxIds;
            private LockType lockType;
            private Object lockObject;
            public bool timeExpired;


            public int _lastTransactionThatSetLockToExclusive = -1;
            




            public int PadIntId {
                get { return padIntId; }
                set { padIntId = value; }
            }

            public LockType Type {
                get { return lockType; }
                set { lockType = value; }
            }

            public List<int> HoldersTxIds {
                get { return holdersTxIds; }
                set { holdersTxIds = value; }
            }

            public Lock(int padIntId) {
                this.padIntId = padIntId;
                this.lockType = LockType.SHARED;
                holdersTxIds = new List<int>();
                lockObject = new Object();
            }

            public void acquire(int txId, LockType alockType) {
                lock (lockObject) {
                    while (hasConflictLock(txId,alockType)) {
                        //Console.WriteLine("ha conflito");
                        bool res;
                        res = Monitor.Wait(lockObject,TimeSpan.FromSeconds(15));
                        if (res == false) {
                            throw new TimeoutException("Timeout due to deadlock");
                        }
                    }
                    if (holdersTxIds.Count == 0) {
                        //Console.WriteLine("nao tinha nenhum holder");
                        holdersTxIds.Add(txId);
                        lockType = alockType;
                        //Console.WriteLine(lockType.ToString());
                    } else if(!holdersTxIds.Contains(txId)) {
                        //Console.WriteLine("eu nao era holder");
                        holdersTxIds.Add(txId);
                    } else if (alockType == LockType.EXCLUSIVE && lockType == LockType.SHARED) {
                        Console.WriteLine("Promoting Tx{0} lock on {1}", txId, padIntId);
                        lockType = LockType.EXCLUSIVE;

                        // bug fix:
                        // Sou eu que estou a mudar o lock de shared para exclusivo, só eu posso mexer daqui em diante!
                        _lastTransactionThatSetLockToExclusive = txId;
                    }
                }
            }

            public void release(int txId) {
                lock(lockObject) {
                    Console.WriteLine("Releasing Tx{0} lock on {1}", txId,padIntId);
                    holdersTxIds.Remove(txId);
                    lockType = LockType.SHARED;
                    Monitor.Pulse(lockObject);
                }
            }


            // bug fix was here!
            private bool hasConflictLock(int txId, LockType aLockType) {

                    if (aLockType == LockType.SHARED) {
                        //Console.WriteLine("Shared");
                        //Console.WriteLine("flag: " + _lastTransactionThatSetLockToExclusive);

                        /*
                        Console.WriteLine("---");
                        
                        Console.WriteLine(lockType == LockType.EXCLUSIVE); // true
                        Console.WriteLine((!holdersTxIds.Contains(txId))); // false
                        Console.WriteLine( _lastTransactionThatSetLockToExclusive != txId); // true
                        Console.WriteLine("---");
                        */


                                                                        // Ha conflito se:
                        return (    (lockType == LockType.EXCLUSIVE) && // lock está exclusivo E
                                    ((!holdersTxIds.Contains(txId)) || // não sou holder, OU
                                    (_lastTransactionThatSetLockToExclusive != -1 && _lastTransactionThatSetLockToExclusive != txId))); // lock foi mexido e não fui eu que mudei de shared para locked (=> nao tenho permissao para mexer)
                    } else {
                        if (lockType == LockType.EXCLUSIVE) {
                            //Console.WriteLine("Exclusive");
                            //Console.WriteLine("flag: " + _lastTransactionThatSetLockToExclusive);
                                                                        // Ha conflito se:
                            return ((!holdersTxIds.Contains(txId)) ||  // não sou holder OU
                                    (_lastTransactionThatSetLockToExclusive != -1 && _lastTransactionThatSetLockToExclusive != txId));  // lock foi mexido e não fui eu que mudei de shared para locked (=> nao tenho permissao para mexer)
                        } else {
                            //Console.WriteLine("outro");
                            return !(holdersTxIds.Count == 0 ||
                                holdersTxIds.Contains(txId));
                        }
                    }
            }
        }




        public class LockManager {
            private Dictionary<int, Lock> locks;

            public LockManager() {
                locks = new Dictionary<int, Lock>();
            }

            public void setLock(int padIntId, int txId, LockType lockType) {
                Lock foundLock;
                lock(this) {
                    if (!locks.ContainsKey(padIntId)) {
                        locks.Add(padIntId, new Lock(padIntId)); // cria shared
                    }
                    foundLock = locks[padIntId];
                }
               // Console.WriteLine("locktype: " + lockType.ToString());
                foundLock.acquire(txId, lockType);
            }

            public delegate void LockTimer(Lock l);

            public void timeOut(Lock l) {
                l.timeExpired = true;
            }

            public void unLock(int txId) {
                lock (this) {
                    Dictionary<int, Lock>.ValueCollection theLocks =
                        locks.Values;

                    foreach (Lock l in theLocks) {
                        if (l.HoldersTxIds.Contains(txId)) {
                            l.release(txId);
                        }
                    }
                }
            }

        }
    }
}
