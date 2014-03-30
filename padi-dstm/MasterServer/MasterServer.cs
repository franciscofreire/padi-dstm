using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;

namespace PADI_DSTM {
    namespace MasterServer {
        class Master : MarshalByRefObject, IMasterServer {

            private class DataServerInfo {

                private IDataServer _myRemoteServer;
                private String _myURL;

                public DataServerInfo(String url, IDataServer remoteServer){
                    _myURL = url;
                    _myRemoteServer = remoteServer;
                }

                public IDataServer getRemoteServer(){
                    return _myRemoteServer;
                }

                public String getURL(){
                    return _myURL;
                }
            }
            


            // Hashtable with information regarding objects' location
            private Hashtable padInts = new Hashtable();

            // Hashtable - cache of PadInts
            private Hashtable padIntsCache = new Hashtable();


            // ArrayList of Data Servers
            private ArrayList dataServers = new ArrayList();

            // Index for RoundRobin
            private int indexLastServer = 0;
            // ATENÇÃO!!! Quando se perde/remove um DataServer, precisa de correcão (-1?)
            
            // Queue with the Data Server, for the Round Robin algorithm
            //private Queue roundRobin = new Queue();


            public IPadInt CreatePadInt(int uid) {
                // Round Robin:
                //DataServer dataServer = roundRobin.Dequeue(); // Retira do topo
                //roundRobin.Queue(dataServer); // Coloca no fim

                DataServerInfo dServer = (DataServerInfo) dataServers[indexLastServer];
                
                // Round Robin:
                indexLastServer = (indexLastServer + 1) % dataServers.Count;

                IPadInt obj = dServer.getRemoteServer().store(uid);
                
                // register channel
                //ChannelServices.RegisterChannel(new TcpChannel(/* ?? */), false); // ?

                // retrieve remote server proxy
                //String url = "tcp://localhost:" + dServer.getURL() + "/IDataServer";
                //IDataServer remoteServer = (IDataServer)Activator.GetObject(typeof(IDataServer), url);
                //IPadInt obj = remoteServer.CreatePadInt(uid);
                
                if (!padInts.Contains(uid)) {
                    padInts.Add(uid, dServer);
                    padIntsCache.Add(uid, obj);
                    return obj;
                } else {
                    return null;
                }
            }


            // Se a cache contem o PadInt
            //  retornamos PadInt
            // Caso contrario
            //  retornamos url
            public void AccessPadInt(String client, int uid) {
                IClient clientRef = (IClient)Activator.GetObject(typeof(IClient), client);
                
                if (padIntsCache.Contains(uid)) {
                    IPadInt obj = (IPadInt) padIntsCache[uid];
                    clientRef.sendPadInt(uid, obj);
                } else if (padInts.Contains(uid)) {
                    DataServerInfo dServer = (DataServerInfo) padInts[uid];
                    clientRef.sendUrl(uid, dServer.getURL());
                }
            }

            public void registerServer(String url) {
                foreach (DataServerInfo server in dataServers) {
                    if (server.getURL().Equals(url))
                        return;
                }
                IDataServer remoteServer = (IDataServer) Activator.GetObject(typeof(IDataServer), url);
                DataServerInfo serverInfo = new DataServerInfo(url, remoteServer);
                dataServers.Add(serverInfo);
                Console.WriteLine("Server " + url + " registered");
            }
        
        
        }

        class Program {
            static void Main(string[] args) {
                TcpChannel channel = new TcpChannel(9999);
                ChannelServices.RegisterChannel(channel, true);

                Master master = new Master();

                RemotingServices.Marshal(master, "MasterServer", typeof(IMasterServer));

                System.Console.WriteLine("Started Master Server...");
                System.Console.ReadKey();
            }
        }
    }
}