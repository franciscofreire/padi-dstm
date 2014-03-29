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

                public DataServerInfo(IDataServer remoteServer, String url){
                    _myRemoteServer = remoteServer;
                    _myURL = url;
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
                    return obj;
                } else {
                    return null;
                }
            }


            public IPadInt AccessPadInt(int uid) {
                if (padInts.Contains(uid)) {
                    DataServerInfo serverInfo = (DataServerInfo) padInts[uid];
                    String url = "tcp://localhost:" + serverInfo.getURL() + "/IDataServer";
                    IDataServer remoteServer = (IDataServer) Activator.GetObject(typeof(IDataServer), url);
                    return remoteServer.load(uid);
                } else {
                    return null;
                }
            }

        }

        class Program {
            static void Main(string[] args) {
                // TODO: Master must register his remote reference

                // TODO: Master must receive DataServers data on their regist
            }
        }
    }
}