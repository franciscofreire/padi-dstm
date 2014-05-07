using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PADI_DSTM {


    public class OperationException : ApplicationException {

       
        private String _msg;

       

        public String Msg {
            get {
                return _msg;
            }
        }


        public OperationException(String msg) {
            
            _msg = msg;
        }

        // A constructor is needed for serialization when an 
        // exception propagates from a remoting server to the client.  
        protected OperationException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
            : base(info, context) {

            _msg = info.GetString("_msg");
        }

        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) {

            base.GetObjectData(info, context);
            info.AddValue("_msg", _msg);
        }

    }
}