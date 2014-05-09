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
                timeExpired = false;
            }

            public void acquire(int txId, LockType alockType) {
                lock (lockObject) {
                    while (hasConflictLock(txId)) {
                        Monitor.Wait(lockObject,TimeSpan.FromSeconds(1));
                        if (timeExpired) {
                            timeExpired = false;
                            throw new TimeoutException("Timeout due to deadlock");
                        }
                    }
                    if (holdersTxIds.Count == 0) {
                        holdersTxIds.Add(txId);
                        lockType = alockType;
                    } else if(!holdersTxIds.Contains(txId)) {
                        holdersTxIds.Add(txId);
                    } else if (alockType == LockType.EXCLUSIVE && lockType == LockType.SHARED) {
                        Console.WriteLine("Promoting Tx{0} lock on {1}", txId, padIntId);
                        lockType = LockType.EXCLUSIVE;
                    }
                }
            }

            public void release(int txId) {
                lock(lockObject) {
                    Console.WriteLine("Releasing Tx{0} lock on {1}", txId,padIntId);
                    holdersTxIds.Remove(txId);
                    lockType = LockType.SHARED;
                    Monitor.PulseAll(lockObject);
                }
            }


            private bool hasConflictLock(int txId) {
                return lockType == LockType.EXCLUSIVE &&
                    !holdersTxIds.Contains(txId);
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
                        locks.Add(padIntId, new Lock(padIntId));
                    }
                    foundLock = locks[padIntId];
                }
                

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
