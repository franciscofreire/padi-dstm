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


    public interface IMasterServer {

        IPadInt CreatePadInt(int uid);
        PadIntInfo AccessPadInt(int uid);
        void registerServer(String url);
        void registerNewPrimaryServer(String oldServerUrl, String newServerUrl);
        void registerClient(String url);
        String Status();
        
        int TxBegin(String clientUrl);
        bool TxAbort(int txId);
        bool TxCommit(int txId);
        bool getDecision(int txId);
        bool join(int txId, String url);
    }

    public interface IDataServer {
        
        IPadInt store(int uid);
        IPadInt load(int uid);
        String name{ get; set;}
        bool Fail();
        bool Freeze();
        bool Recover();
        String Status();
        bool isFail { get; }
        bool isFreeze { get; }
        void connect(int port);
        //void receiveAlive();
        bool canCommit(int txId);
        bool doCommit(int txId);
        bool doAbort(int txId);
        bool haveCommited(int txId);
        void receiveHeartBeat(String type);
    }

    public interface IClient {

    }
}
