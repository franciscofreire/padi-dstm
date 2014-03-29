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
        IPadInt AccessPadInt(int uid);
    }

    public interface IDataServer
    {
        IPadInt store(int uid);
        IPadInt load(int uid);
    }
}
