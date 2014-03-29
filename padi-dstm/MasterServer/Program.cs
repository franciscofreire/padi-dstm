using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PADI_DSTM
{
    namespace MasterServer
    {
        class Master : MarshalByRefObject, IMasterServer
        {
            private Hashtable padInts = new Hashtable();


            

        }

        class Program
        {
            static void Main(string[] args)
            {
                // TODO: Master must register his remote reference
            }
        }
    }
}