using System;
using System.Windows.Forms;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Transactions;

namespace PADI_DSTM {

    public class PadInt {

        private int uid;
        private IPadInt remoteObj;
        private PadiDstm padiDstm;

        public PadInt(int uid, PadiDstm padiDstm, IPadInt remoteObj) {
            this.uid = uid;
            this.padiDstm = padiDstm;
            this.remoteObj = remoteObj;
        }

        public int Read() {
            if (padiDstm.TxId == -1) {
                throw new TxException(uid, "Read operation at PadInt " +
                                          uid + " Failed. No active Transaction"); 
            }
            return remoteObj.Read(padiDstm.TxId);
        }
    
        public void Write(int value) {
            if (padiDstm.TxId == -1)
            {
                throw new TxException(uid, "Write operation at PadInt " +
                                          uid + " Failed. No active Transaction");
            }
            remoteObj.Read(padiDstm.TxId);
        }
    }

    public static class PadiDstm  {

        // Delegates for Form's TextBox manipulation
        delegate void ClearTextDel();
        delegate void UpdateTextDel(String msg);

        // TextBox to dump status
        private TextBox _statusBox;

        // All the PadInt objects accessed by this client (target objects for Read/Write)
        private ArrayList _myObjects;

        // Current transaction from this client
        private int txId = -1;

        public int TxId {
            get { return txId; }
        }

        // TCP Channel
        private TcpChannel channel;

        // MasterServer remote object
        private IMasterServer _masterServer;

        public IMasterServer MasterServer
        {
            get { return _masterServer; }
        }

        public bool Init() {
            channel = new TcpChannel(9010);
            ChannelServices.RegisterChannel(channel, true);
            String urlMaster = "tcp://localhost:9999/MasterServer";
            _masterServer = (IMasterServer)Activator.GetObject(typeof(IMasterServer), urlMaster);
            _masterServer.registerClient("tcp://localhost:9910"); 
            return true;
        }

        public bool TxBegin() {
            try {
                txId = _masterServer.TxBegin("tcp://localhost:9910");
            } catch (TxException e){
                Console.WriteLine("Transaction with id " + e.Tid + " cannot begin.");
            }
            return false;
        }
        
        public bool TxCommit() {
            try { 
            _masterServer.TxCommit(txId);
            } catch (TxException e){
                Console.WriteLine("Transaction with id " + e.Tid + " cannot be commited.");
            }
            return false;
        }

        public bool TxAbort() {
            //TODO
            try {
            _masterServer.TxAbort(_tx);
            
            } catch (TxException e){
                Console.WriteLine("Transaction with id " + e.Tid + " cannot be aborted.");
                
            }
            return false;
        }

        public PadInt CreatePadInt (int uid) {
            IPadInt obj = _masterServer.CreatePadInt(uid);
            if (obj == null) {
                return null;
            } else {
                PadInt localPadInt = new PadInt(uid, this, obj);
                return localPadInt;
            }
        }

        public IPadInt AccessPadInt(int uid) {
            PadIntInfo obj = _masterServer.AccessPadInt(uid);
            if (obj == null) {
                return null;
            }
            else if (!obj.hasPadInt()) {
                IDataServer dataServer = (IDataServer)Activator.GetObject(typeof(IDataServer), obj.ServerUrl);
                IPadInt padIntObj = dataServer.load(uid);
                if (padIntObj == null) {
                    return null;
                    //TODO
                }
            } else {
                _myObjects.Add(obj.PadInt);
                return obj.PadInt;
                //TODO
            }
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
