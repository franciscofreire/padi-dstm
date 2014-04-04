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

                public IDataServer remoteServer{
                    get {
                        return _myRemoteServer;
                    }
                    set {
                        _myRemoteServer = value;
                    }
                }

                public String URL{
                    get {
                        return _myURL;
                    }
                    set {
                        _myURL = value;
                    }
                }
            }

            private class ClientInfo {

                private IClient _myRemoteClient;
                private String _myURL;

                public ClientInfo(String url, IClient remoteClient) {
                    _myURL = url;
                    _myRemoteClient = remoteClient;
                }

                public IClient RemoteServer {
                    get { return _myRemoteClient; }
                    set { _myRemoteClient = value; }
                }

                public String URL {
                    get { return _myURL; }
                    set { _myURL = value; }
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

            //ArrayList of Clients
            private ArrayList clients = new ArrayList();

            // Index for RoundRobin
            private int indexLastServer = 0;
            // ATENÇÃO!!! Quando se perde/remove um DataServer, precisa de correcão (-1?)

            public String Status() {
                return "[I'm OK, I never fail!]";
            }


            private void addPadInt(MyPadInt obj)
            {
                int size = padIntsCache.Count;

                if (size == CACHE_SIZE) {
                    // remover o PadInt mais antigo
                    Console.WriteLine("Oldest PadInt removed ");
                    padIntsCache.RemoveAt(0);
                }
                padIntsCache.Add(obj);
            }

            
            public IPadInt CreatePadInt(int uid) {
                Console.WriteLine("Client wants to create PadInt with id " + uid);

                DataServerInfo dServer = (DataServerInfo) dataServers[indexLastServer];
                
                // Round Robin:
                indexLastServer = (indexLastServer + 1) % dataServers.Count;

                IPadInt obj = dServer.remoteServer.store(uid);
                
                if (!padInts.Contains(uid)) {
                    padInts.Add(uid, dServer);
                    MyPadInt myPadInt = new MyPadInt(uid, obj);
                    addPadInt(myPadInt);
                    Console.WriteLine("PadInt stored ");
                    return obj;
                } else {
                    Console.WriteLine("PadInt already exists " );
                    return null;
                }
            }

            // Se o server a que esse objecto pertence estiver em Freeze ou Fail, faz sentido
            // devolvermos o objecto em cache? Não estamos a violar o comportamento suposto?

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
                    Console.WriteLine("PadInt stored in cache " );

                    return padIntInfo;
                }
                else if (padInts.Contains(uid)) {
                    DataServerInfo dServer = (DataServerInfo)padInts[uid];
                    PadIntInfo padIntInfo = new PadIntInfo(dServer.URL);
                    return padIntInfo;
                }
                else { //PadInt nao existe
                    return null;
                }
            }

            public void registerServer(String url) {
                foreach (DataServerInfo server in dataServers) {
                    if (server.URL.Equals(url))
                        return;
                }
                // obter referencia remota e registar servidor
                IDataServer remoteServer = (IDataServer) Activator.GetObject(typeof(IDataServer), url);
                DataServerInfo serverInfo = new DataServerInfo(url, remoteServer);
                dataServers.Add(serverInfo);

                Console.WriteLine("Server " + url + " registered");
            }

            public void registerClient(String url) {
                foreach (ClientInfo client in clients) {
                    if (client.URL.Equals(url))
                        return;
                }
                // obter referencia remota e registar cliente
                //IClient remoteClient = (IClient)Activator.GetObject(typeof(IClient), url);
                ClientInfo clientInfo = new ClientInfo(url, null);
                clients.Add(clientInfo);
                Console.WriteLine("Client " + url + " registered");
            }




            public Hashtable propagateStatus() {
                String serverName, serverStatus;
                Hashtable results = new Hashtable();
                foreach(DataServerInfo server in dataServers){
                    serverName = server.remoteServer.name;
                    serverStatus = server.remoteServer.Status();
                    Console.WriteLine("Server: " + serverName + " status: " + serverStatus);
                    results.Add(serverName, serverStatus);
                }
                return results;
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