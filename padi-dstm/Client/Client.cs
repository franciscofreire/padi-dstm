using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;

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

                
                int port = 9990;
                TcpChannel channel = new TcpChannel(port);
                ChannelServices.RegisterChannel(channel, true);
                Client client = new Client();
                RemotingServices.Marshal(client, "Client", typeof(IClient));
                System.Console.WriteLine("Started Client...");
                String url = "tcp://localhost:" + port + "/Client";
                String urlMaster = "tcp://localhost:9999/MasterServer";
                IMasterServer masterServer = (IMasterServer)Activator.GetObject(typeof(IMasterServer), urlMaster);
                masterServer.registerClient(url);
                

            }
        }
    }
}