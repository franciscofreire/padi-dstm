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

    public interface IMasterServer
    {
        IPadInt CreatePadInt(int uid);
        void AccessPadInt(String client, int uid);
        void registerServer(String url);
    }

    public interface IDataServer
    {
        IPadInt store(int uid);
        IPadInt load(int uid);
    }

    public interface IClient {
        void sendUrl(int uid, String url);
        void sendPadInt(int uid, IPadInt padint);
    }

}
