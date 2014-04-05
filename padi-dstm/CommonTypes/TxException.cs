using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PADI_DSTM {

    [Serializable()]
            public class TxException : ApplicationException {

            private int _tid;
            

            public int Tid {
                get {
                    return _tid;
                }
                set {
                    _tid = value;
                }
            }


            public TxException(int tid) {
                _tid = tid;
                
            }

                // A constructor is needed for serialization when an 
                // exception propagates from a remoting server to the client.  
                protected TxException(System.Runtime.Serialization.SerializationInfo info,
                    System.Runtime.Serialization.StreamingContext context) {
                
                _tid = info.GetInt32("_tid");
                
                }
            }
}
    

