using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace PADI_DSTM {

    namespace DataServer {
        
        public enum LockType { WRITE, READ };

        public class Lock {
            private LockType type;
            private int padIntId;
            private int transactionId;
            private int id;

            public int Id {
                get { return id; }
                set { id = value; }
            }

            public int PadIntId {
                get { return padIntId; }
                set { padIntId = value; }
            }

            public LockType Type {
                get { return type; }
                set { type = value; }
            }

            public int TransactionId {
                get { return transactionId; }
                set { transactionId = value; }
            }

            public Lock(int id, LockType type, int padIntId, int transactionId) {
                this.id = id;
                this.type = type;
                this.padIntId = padIntId;
                this.transactionId = transactionId;
            }

            public Lock(int transactionId) {
                this.transactionId = transactionId;
            }

            public override bool Equals(Object obj) {
                if (obj == null || GetType() != obj.GetType())
                    return false;
                Lock l = (Lock)obj;
                return l.TransactionId == this.TransactionId;
            }

            public override int GetHashCode() {
                return transactionId;
            }
        }

        public class LockManager {
            private Dictionary<int, List<Lock>> locksGranted;
            private int idGenerator;

            public LockManager() {
                locksGranted = new Dictionary<int, List<Lock>>();
                idGenerator = 0;
            }

            // Possible Approachs (timeout): throw an exception
            public Lock getLock(LockType type, int padIntId, int transactionId) {
                lock (this) {
                    if (!locksGranted.ContainsKey(padIntId)) {
                        locksGranted[padIntId] = new List<Lock>();
                    }
                    List<Lock> padIntLocks = locksGranted[padIntId];
                    Lock l = new Lock(idGenerator++, type, padIntId, transactionId);

                    while (!canHaveLock(l, padIntLocks)) {
                        Monitor.Wait(this);
                    }
                    // Nice! Now we have the lock
                    padIntLocks.Add(l);
                    return l;
                }
            }

            private bool canHaveLock(Lock l, List<Lock> grantedList) {
                foreach (Lock el in grantedList) {
                    if (el.Type == LockType.WRITE) {
                        if (l.Equals(el)) {
                            return true;
                        } else
                            return false;
                    }
                }
                // we only have read locks, it's ok
                return true;
            }

            public void releaseLock(Lock l) {
                lock (this) {
                    List<Lock> padIntLocks = locksGranted[l.PadIntId];
                    padIntLocks.Remove(l);
                    if (l.Type == LockType.WRITE) {
                        //let's release the next thread
                        Monitor.Pulse(this);
                    }
                }
            }

            public void releaseLock(int txId, Server s) {
                lock (this) {
                    ServerTransaction tr = s.Transactions[txId];
                    foreach (PadInt p in tr.getCopiesKeys()) {
                        List<Lock> padIntLocks = locksGranted[p.Id];
                        if (padIntLocks != null) {
                            int index = padIntLocks.IndexOf(new Lock(txId));
                            if (index != -1) {
                                Lock l = padIntLocks[index];
                                Console.WriteLine("lock in " + l.PadIntId + "by " + l.TransactionId + " is being removed");
                                padIntLocks.Remove(new Lock(txId));
                                if (l.Type == LockType.WRITE) {
                                    //let's release next thread
                                    Monitor.Pulse(this);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
