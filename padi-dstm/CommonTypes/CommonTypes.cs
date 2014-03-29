using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADI_DSTM
{
    interface IPadInt {
        int Read();
        void Write(int value);
    }

    interface IMasterServer
    {
        IPadInt CreatePadInt(int uid);
        IPadInt AccessPadInt(int uid);
    }

    interface IDataServer
    {
    
    }
}
