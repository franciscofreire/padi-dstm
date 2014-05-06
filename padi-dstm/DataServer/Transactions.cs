﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PADI_DSTM {

    namespace DataServer {

        public class ServerTransaction {

            private Dictionary<PadInt, int> copies;
            private int txId;
            private bool abort;
            private Stack<Lock> locksStack;

            public ServerTransaction(int txId, PadInt Obj) {
                this.txId = txId;
                copies = new Dictionary<PadInt, int>();
                copies.Add(Obj, Obj.Value);
                abort = false;
                locksStack = new Stack<Lock>();
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

            public int getCopiesSize() {
                return copies.Count;
            }

            public int getValue(PadInt padInt) {
                return copies[padInt];
            }

            public Dictionary<PadInt, int>.KeyCollection getCopiesKeys() {
                return copies.Keys;
            }

            public bool containsPadInt(PadInt padInt) {
                return copies.ContainsKey(padInt);
            }

            public void doChanges() {
                /*foreach (KeyValuePair<PadInt, int> entry in copies) {
                    entry.Key.Value = entry.Value;
                }*/
            }
            public void rollback() {
                foreach (KeyValuePair<PadInt, int> entry in copies) {
                    entry.Key.Value = entry.Value;
                }
            }
            public void pushLock(Lock l) {
                locksStack.Push(l);
            }
            // Note: returns null when the stack is empty
            public Lock popLock() {
                try {
                    return locksStack.Pop();
                } catch (InvalidOperationException ioe) {
                    Console.WriteLine("Stack of Tx{0} is empty! {1}", txId, ioe);
                    return null;
                }
            }
        }
    }
}