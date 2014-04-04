using System;
using System.Windows.Forms;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;

namespace PADI_DSTM
{
    public class Library  {

        // Delegates for Form's TextBox manipulation
        delegate void ClearTextDel();
        delegate void UpdateTextDel(String msg);

        // TextBox to dump status
        private TextBox _statusBox;

        // Client info
        private int port;
        private String clientUrl;
        
        
        private TcpChannel channel;

        // MasterServer remote object
        private IMasterServer _masterServer;

        public Library(TextBox box, String clientUrl, int port) {
            _statusBox = box;
            this.clientUrl = clientUrl;
            this.port = port;
        }

        public bool Init() {
            channel = new TcpChannel(port);
            ChannelServices.RegisterChannel(channel, true);
            String urlMaster = "tcp://localhost:9999/MasterServer";
            _masterServer = (IMasterServer)Activator.GetObject(typeof(IMasterServer), urlMaster);
            _masterServer.registerClient(clientUrl); 
            return true;
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




        public IPadInt CreatePadInt (int uid) {
           return _masterServer.CreatePadInt(uid);
        }

        public IPadInt AccessPadInt(int uid) {
            PadIntInfo obj = _masterServer.AccessPadInt(uid);
            
            if (!obj.hasPadInt()) {
 
                IDataServer dataServer = (IDataServer)Activator.GetObject(typeof(IDataServer), obj.ServerUrl);
                /*
                if (dataServer.isFail) {
                    //Console.WriteLine("Client " + clientUrl + " can't access PadInt " + uid + ": " + dataServer.name + "is set to Fail!");
                    return null;
                } else if (dataServer.isFreeze) {
                    //Console.WriteLine("Client " + clientUrl + " can't access PadInt " + uid + ": " + dataServer.name + "is set to Freeze! Logging this command.");
                   // dataServer.SaveCommand( ....... )
                    return null;
                } else { */
                    return dataServer.load(uid);
                    // ATENCAO: Objecto pode vir a null (por nao existir!)
                    
                //}
            } else {
                return obj.PadInt;
            }
        }


        public bool Status() {
            // limpa janela do status das cacas anteriores:
            _statusBox.Invoke(new ClearTextDel(_statusBox.Clear));

            String text = "Node " + "MasterServer" + " is set to " + _masterServer.Status() + " Mode.";
            //Console.WriteLine(text);
            String line = text + "\r\n";
            _statusBox.Invoke(new UpdateTextDel(_statusBox.AppendText), new object[] { line });

            Hashtable results = _masterServer.propagateStatus();

            foreach (DictionaryEntry s in results) {
                text = "Node " + s.Key + " is set to " + s.Value + " Mode.";
                //Console.WriteLine(text);
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
