using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace PADI_DSTM
{
    public class Library : IDataServer {

        delegate void ClearTextDel();
        delegate void UpdateTextDel(String msg);

        private TextBox _statusBox;

        public Library(TextBox box) {
            _statusBox = box;
        }

        public bool Init() {  //public?
        //TODO
            return false;
        }

        public bool TxBegin() { 
        //TODO
            return false;
        }
        
        public bool TxCommit() {
        //TODO
            return false;
        }

        public bool TxAbort() {
        //TODO
            return false;
        }

        public bool Status() {
            //TODO
            
            //IMasterServer masterServer = (IMasterServer)Activator.GetObject(typeof(IMasterServer), urlMaster), 
            //IDataServer dataServer1 = (IDataServer)Activator.GetObject(typeof(IDataServer), urlDServer1);
            //IDataServer dataServer2 = (IDataServer)Activator.GetObject(typeof(IDataServer), urlDServer2);
            // ...
            // fazer os servidores registarem-se com a library tambem?
            // e Library mantem um arrayList com os nodes?

            // limpa janela do status das cacas anteriores:
            _statusBox.Invoke(new ClearTextDel(_statusBox.Clear));
            

            //foreach ( server in nodes ) {
                //String text = "Node " + server.name + " is set to " + server.Status() + " Mode.";
                //Console.WriteLine(text);
                //String line = text + "\r\n";
                //_statusBox.Invoke(new UpdateTextDel(_statusBox.AppendText), new object[] { line });
            //}

            return false; // ?
        }
        
        public bool Fail(string URL) {
            IDataServer dataServer = (IDataServer)Activator.GetObject(typeof(IDataServer), URL);
            return dataServer.Fail(); // return true;
        }

        public bool Freeze(string URL) {
            IDataServer dataServer = (IDataServer)Activator.GetObject(typeof(IDataServer), URL);
            return dataServer.Freeze(); // return true;
        }

        public bool Recover(string URL) {
            IDataServer dataServer = (IDataServer)Activator.GetObject(typeof(IDataServer), URL);
            return dataServer.Recover(); //returns true if success, false if the server was not in Fail or Freeze
        }
    }
}
