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
            if (PadiDstm.txId == -1) {
                throw new TxException(uid, "Read operation at PadInt " +
                                          uid + " Failed. No active Transaction"); 
            }
            return remoteObj.Read(PadiDstm.txId);
        }
    
        public void Write(int value) {
            if (PadiDstm.txId == -1) {
                throw new TxException(uid, "Write operation at PadInt " +
                                          uid + " Failed. No active Transaction");
            }
            remoteObj.Read(PadiDstm.txId);
        }
    }

    public class PadiDstm  {
        // Current transaction from this client
        public static int txId;

        public const String urlMaster = "tcp://localhost:9999/MasterServer";
        public const String clientUrl = "tcp://localhost:9910";

        // TCP Channel
        public static TcpChannel channel;

        // MasterServer remote object
        public static IMasterServer _masterServer;

        
        public static bool Init() {
            try {
                txId = -1;
                channel = new TcpChannel(9010);
                ChannelServices.RegisterChannel(channel, true);
                _masterServer = (IMasterServer)Activator.GetObject(typeof(IMasterServer), urlMaster);
                _masterServer.registerClient(clientUrl); 
                return true;
            } catch(Exception e) {
                Console.WriteLine("Init Exception:" + e);
                return false;
            }
        }

        public static bool TxBegin() {
            if (txId != -1) {
                throw new TxException(txId, "Cannot start new transaction." +
                    "Transaction with id" + txId + "is active");
            }
            try {
                txId = _masterServer.TxBegin(clientUrl);
                return true;
            } catch (TxException e){
                Console.WriteLine("Transaction with id " + e.Tid + " cannot begin.");
                return false;
            }   
        }
        
        public static bool TxCommit() {
            if (txId == -1) {
                throw new TxException(txId, "Cannot commit. No active Transaction");
            }
            try { 
                _masterServer.TxCommit(txId);
                return true;
            } catch (TxException e){
                Console.WriteLine("Transaction with id " + e.Tid + " cannot be commited.");
                return false;
            }
        }

        public static bool TxAbort() {
            if (txId == -1) {
                throw new TxException(txId, "Cannot abort. No active Transaction");
            }
            try {
                _masterServer.TxAbort(txId);
                return true;
            } catch (TxException e){
                Console.WriteLine("Transaction with id " + e.Tid + " cannot be aborted.");
                return false;
            }
        }

        public static PadInt CreatePadInt (int uid) {
            IPadInt obj = _masterServer.CreatePadInt(uid);
            if (obj == null) {
                return null;
            } else {
                PadInt localPadInt = new PadInt(uid, this, obj);
                return localPadInt;
            }
        }

        public static PadInt AccessPadInt(int uid) {
            IPadInt padIntObj;
            PadIntInfo obj = _masterServer.AccessPadInt(uid);
            if (obj == null) {
                return null;
            }

            if (!obj.hasPadInt()) { // Catch remoting exception
                IDataServer dataServer = (IDataServer)Activator.GetObject(typeof(IDataServer), obj.ServerUrl);
                padIntObj = dataServer.load(uid);
            } else {
                padIntObj = obj.PadInt;
            }
            PadInt localPadInt = new PadInt(uid, this, padIntObj);
            return localPadInt;
        }

        public static bool Status() {
            try {
                _masterServer.Status();
                return true;
            } catch (TxException e) {
                Console.WriteLine("Status error: " + e);
                return false;
            }
        }

        public static bool Fail(string URL) {
            try {
                IDataServer dataServer = (IDataServer)Activator.GetObject(typeof(IDataServer), URL);
                dataServer.Fail(); // return true;
                return true;
            } catch (Exception e) { //TODO: Improve the catch
                return false;
            }
        }
        public static bool Freeze(string URL) {
            try {
                IDataServer dataServer = (IDataServer)Activator.GetObject(typeof(IDataServer), URL);
                dataServer.Freeze();
                return true;
            } catch (Exception e) { //TODO: Improve the catch
                return false;
            }
        }

        public static bool Recover(string URL) {
            try {
                IDataServer dataServer = (IDataServer)Activator.GetObject(typeof(IDataServer), URL);
                dataServer.Freeze(); 
                return true;
            } catch (Exception e) { //TODO: Improve the catch
                return false;
            }
        }
    }
}
