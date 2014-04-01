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


            // Para associar PadInt com Id (usado para a cache)
            private class MyPadInt {
                private int uid;
                private IPadInt padInt;

                
                public int Uid {
                    get { return uid; }
                    set { uid = value; }
                }

                public IPadInt PadInt {
                    get { return padInt; }
                    set { padInt = value; }
                }

                public override bool Equals(Object obj) {
                    if (obj == null || GetType() != obj.GetType())
                        return false;

                    MyPadInt p = (MyPadInt)obj;
                    return (this.Uid == p.Uid);
                }
                public override int GetHashCode() {
                    return uid;
                }

                public MyPadInt(int uid, IPadInt obj) {
                    this.uid = uid;
                    this.padInt = obj;
                }

            }


            private const int CACHE_SIZE = 20;

            // Hashtable with information regarding objects' location
            private Hashtable padInts = new Hashtable();

            // Hashtable - cache of PadInts
            private ArrayList padIntsCache = new ArrayList();


            // ArrayList of Data Servers
            private ArrayList dataServers = new ArrayList();

            // Index for RoundRobin
            private int indexLastServer = 0;
            // ATENÇÃO!!! Quando se perde/remove um DataServer, precisa de correcão (-1?)
            
            private void addPadInt(MyPadInt obj)
            {
                int size = padIntsCache.Count;

                if (size == CACHE_SIZE) {
                    // remover o PadInt mais antigo
                    padIntsCache.RemoveAt(0);
                }
                padIntsCache.Add(obj);
            }


            public IPadInt CreatePadInt(int uid) {
                Console.WriteLine("Client wants to create PadInt with id " + uid);

                DataServerInfo dServer = (DataServerInfo) dataServers[indexLastServer];
                
                // Round Robin:
                indexLastServer = (indexLastServer + 1) % dataServers.Count;

                IPadInt obj = dServer.getRemoteServer().store(uid);
                
                if (!padInts.Contains(uid)) {
                    padInts.Add(uid, dServer);
                    MyPadInt myPadInt = new MyPadInt(uid, obj);
                    addPadInt(myPadInt);
                    return obj;
                } else {
                    return null;
                }
            }


            // Se a cache contem o PadInt
            //  retornamos PadInt
            // Caso contrario
            //  retornamos url
            public PadIntInfo AccessPadInt(String client, int uid) {
                Console.WriteLine("Client " + client);
                Console.WriteLine("Requests PadInt with id " + uid);

                if (padIntsCache.Contains(uid)) {
                    IPadInt obj = (IPadInt) padIntsCache[uid];
                    PadIntInfo padIntInfo = new PadIntInfo(obj);
                    return padIntInfo;
                }
                else if (padInts.Contains(uid)) {
                    DataServerInfo dServer = (DataServerInfo)padInts[uid];
                    PadIntInfo padIntInfo = new PadIntInfo(dServer.getURL());
                    return padIntInfo;
                }
                else { //PadInt nao existe
                    return null;
                }
            }

            public void registerServer(String url) {
                foreach (DataServerInfo server in dataServers) {
                    if (server.getURL().Equals(url))
                        return;
                }
                // obter referencia remota e registar servidor
                IDataServer remoteServer = (IDataServer) Activator.GetObject(typeof(IDataServer), url);
                DataServerInfo serverInfo = new DataServerInfo(url, remoteServer);
                dataServers.Add(serverInfo);
                Console.WriteLine("Server " + url + " registered");
            }
        
        
        }

        class Program {
            static void Main(string[] args) {
                TcpChannel channel = new TcpChannel(9999);
                ChannelServices.RegisterChannel(channel, false);

                Master master = new Master();

                RemotingServices.Marshal(master, "MasterServer", typeof(IMasterServer));

                System.Console.WriteLine("Started Master Server...");
                System.Console.ReadKey();
            }
        }
    }
}