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

namespace PADI_DSTM
{
    public class PadInt {
        private int uid;
        private IPadInt remoteObj;
        private PadiDstm padiDstm;

        public PadInt(int uid, PadiDstm padiDstm) {
            this.uid = uid;
            this.padiDstm = padiDstm;
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
            _masterServer.registerClient("tcp://localhost:9999"); 
            return true;
        }

        public bool TxBegin() {
            try {
                _tx = _masterServer.TxBegin(clientUrl, _myObjects);
            } catch (TxException e){
                Console.WriteLine("Transaction with id " + e.Tid + " cannot begin.");
            }
            return false;
        }
        
        public bool TxCommit() {
            try { 
            _masterServer.TxCommit(_tx);
            } catch (TxException e){
                Console.WriteLine("Transaction with id " + e.Tid + " cannot be commited.");
            }
            return false;
        }

        public bool TxAbort() {
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
                PadInt localPadInt = new PadInt(uid, this);
                _myObjects.Add(obj);
                return obj;
            }
        }

        public IPadInt AccessPadInt(int uid) {
            PadIntInfo obj = _masterServer.AccessPadInt(uid);
            if (obj == null) {
                // vem a null porque nao existe na tabela padInts do master sequer!
                // excepção!
                _accessedObj = null;
                return null;
            }
            else if (!obj.hasPadInt()) {
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
                IPadInt padIntObj = dataServer.load(uid);
                if (padIntObj == null) {
                    // ATENCAO: Objecto pode vir a null (por nao existir - server nao responde (freeze?)!)
                    // excepção!
                    _accessedObj = null;
                    return null;
                } else {
                    _accessedObj = padIntObj;
                    _myObjects.Add(padIntObj);
                    return padIntObj;
                }
                //}
            } else {
                _accessedObj = obj.PadInt;
                _myObjects.Add(obj.PadInt);
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
