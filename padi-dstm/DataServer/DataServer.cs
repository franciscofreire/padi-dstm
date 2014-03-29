using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;

namespace PADI_DSTM {
    namespace DataServer {
        class DataServer {
            // Hashtable regarding the PadInts stored
            private Hashtable padInts = new Hashtable();

            public IPadInt CreatePadInt(int uid) {
                if (!padInts.Contains(uid)) {
                    PadInt obj = new PadInt();
                    padInts.Add(uid, obj);
                    return obj;
                } else {
                    return null;
                }
            }

            public IPadInt AccessPadInt(int uid) {
                if (padInts.Contains(uid)) {
                    return padInts[uid];
                } else {
                    return null;
                }
            }
        }

        class Program {
            static void Main(string[] args) {
            
            // TODO: Send info to Master
            }
        }
    }
}
