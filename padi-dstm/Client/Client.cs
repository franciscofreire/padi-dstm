using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace PADI_DSTM
{
    namespace Client
    {
        class Client : MarshalByRefObject, IClient
        {
            /// <summary>
            /// The main entry point for the application.
            /// </summary>
            [STAThread]
            static void Main()
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1());
            }
        }
    }
}