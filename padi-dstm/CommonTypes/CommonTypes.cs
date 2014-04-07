using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace PADI_DSTM
{
    public interface IPadInt {
        int Read(int txId);
        void Write(int txId, int value);
    }

    [Serializable]
    public class PadIntInfo {
        private IPadInt padInt;
        private String serverUrl;

        public IPadInt PadInt {
            get { return padInt; }
            set { padInt = value; }
        }

        public String ServerUrl {
            get { return serverUrl; }
            set { serverUrl = value; }
        }

        public PadIntInfo(IPadInt padInt) {
            this.padInt = padInt;
            this.serverUrl = null;
        }

        public PadIntInfo(String serverUrl) {
            this.serverUrl = serverUrl;
            this.padInt = null;
        }

        public bool hasPadInt() {
            return padInt != null;
        }

        public bool hasServerUrl() {
            return serverUrl != null;
        }
    }

    [Serializable]
    public class MyTransaction {
        // Transaction Info
        private int _txID;
        private Transaction _tx; // CommittableTransaction ?
        private ArrayList _txObjs;
        ArrayList _participants;

        public MyTransaction(int tid, Transaction tx, ArrayList txObjs, ArrayList p) {
            _txID = tid;
            _tx = tx;
            _txObjs = txObjs;
            _participants = p;
        }

        public int txID {
            get {
                return _txID;
            }
            set {
                _txID = value;
            }
        }

        public Transaction tx {
            get {
                return _tx;
            }
            set {
                _tx = value;
            }
        }

        public ArrayList txObjs {
            get {
                return _txObjs;
            }
            /* set {
                _txObjs = value;
            } */
        }

        public ArrayList Participants {
            get {
                return _participants;
            }
        }
    }

    public interface IMasterServer {

        IPadInt CreatePadInt(int uid);
        PadIntInfo AccessPadInt(int uid);
        void registerServer(String url);
        void registerClient(String url);
        Hashtable propagateStatus();
        String Status();
        
        MyTransaction TxBegin(String clientUrl, ArrayList objs);
        bool TxAbort(MyTransaction t);
        bool TxCommit(MyTransaction t);
        bool getDecision(MyTransaction t);
        bool join(MyTransaction t);
    }

    public interface IDataServer {
        
        IPadInt store(int uid);
        IPadInt load(int uid);
        String name{ get; set;}
        bool Fail();
        bool Freeze();
        bool Recover();
        String Status();
        bool isFail { get; set; }
        bool isFreeze { get; set; }
        
        bool canCommit(MyTransaction t);
        bool doCommit(MyTransaction t);
        bool doAbort(MyTransaction t);
        bool haveCommited(MyTransaction t);
    }

    public interface IClient {

    }
}
