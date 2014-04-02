using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADI_DSTM
{
    public interface IPadInt {
        int Read();
        void Write(int value);
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
            this.serverUrl = null;
        }

        public bool hasPadInt() {
            return padInt != null;
        }

        public bool hasServerUrl() {
            return serverUrl != null;
        }
    }


    public interface IMasterServer
    {
        IPadInt CreatePadInt(int uid);
        PadIntInfo AccessPadInt(String client, int uid);
        void registerServer(String url);
        void registerClient(String url);
    }

    public interface IDataServer
    {
        IPadInt store(int uid);
        IPadInt load(int uid);
    }

    public interface IClient {

    }


}
