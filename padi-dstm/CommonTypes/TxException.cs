using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PADI_DSTM {

    [Serializable]
    public class TxException : ApplicationException {

        private int _tid;
        private String _msg;

        public int Tid {
            get {
                return _tid;
            }
        }

        public String Msg {
            get {
                return _msg;
            }
        }


        public TxException(int tid, String msg) {
            _tid = tid;
            _msg = msg;    
        }

        // A constructor is needed for serialization when an 
        // exception propagates from a remoting server to the client.  
        protected TxException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
            : base(info, context) {              
            
            _tid = info.GetInt32("_tid");
            _msg = info.GetString("_msg");                
        }

        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, 
            System.Runtime.Serialization.StreamingContext context) {

            base.GetObjectData(info, context);
            info.AddValue("_tid", _tid);
            info.AddValue("_msg", _msg);
        }

    }
}
    

