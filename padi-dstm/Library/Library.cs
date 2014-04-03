using System;
using System.Windows.Forms;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace PADI_DSTM
{
    public class Library  {

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

            // limpa janela do status das cacas anteriores:
            _statusBox.Invoke(new ClearTextDel(_statusBox.Clear));
           
            String urlMaster = "tcp://localhost:9999/MasterServer";
            IMasterServer masterServer = (IMasterServer)Activator.GetObject(typeof(IMasterServer), urlMaster);

            String line = masterServer.Status() + "\r\n";
            _statusBox.Invoke(new UpdateTextDel(_statusBox.AppendText), new object[] { line });

            Hashtable results = masterServer.propagateStatus();

            foreach (DictionaryEntry s in results) {
                String text = "Node " + s.Key + " is set to " + s.Value + " Mode.";
                Console.WriteLine(text);
                line = text + "\r\n";
                _statusBox.Invoke(new UpdateTextDel(_statusBox.AppendText), new object[] { line });
            }

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
