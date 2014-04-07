using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Threading;
using System.Transactions;

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
                    get {return _myRemoteServer;}
                    set {_myRemoteServer = value;}
                }

                public String URL{
                    get { return _myURL;}
                    set {_myURL = value; }
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

            // Transaction Id
            private static int transactionId = 0;

            // Hashtable of clientUrls and their transactions
            Hashtable clientTransactions = new Hashtable();

            // Voting decision for 2pc
            private bool _myCommitDecision = true;


            public String Status() {
                String text = "[I'm OK, I never fail!]";
                Console.WriteLine("Server: " + "MasterServer" + " status: " + text);
                 return text;
            }

            public Hashtable propagateStatus() {
                String serverName, serverStatus;
                Hashtable results = new Hashtable();
                foreach (DataServerInfo server in dataServers) {
                    serverName = server.remoteServer.name;
                    serverStatus = server.remoteServer.Status();
                    Console.WriteLine("Server: " + serverName + " status: " + serverStatus);
                    results.Add(serverName, serverStatus);
                }
                Console.WriteLine("---");
                return results;
            }

            public bool join(MyTransaction t) {
                //TODO
                return false;
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
                Console.WriteLine("[CREATE] Client wants to create PadInt with id " + uid);
                if (!padInts.Contains(uid)) {
                    DataServerInfo dServer = (DataServerInfo)dataServers[indexLastServer];
                    while (dServer.remoteServer.isFail) {
                        Console.WriteLine("[CREATE] DataServer " + dServer.remoteServer.name + " is set to [Fail]: Passing his turn on Round Robin");
                        indexLastServer = (indexLastServer + 1) % dataServers.Count; // salta um índice
                        dServer = (DataServerInfo)dataServers[indexLastServer];
                    }
                    if (dServer.remoteServer.isFreeze) {
                        Console.WriteLine("[CREATE] DataServer " + dServer.remoteServer.name + "is set to [Freeze]: Logging this command.");
                        // dServer.SaveCommand( ....... )
                        Console.WriteLine("---");
                        return null;
                    }  
                    IPadInt obj = dServer.remoteServer.store(uid);
                    // obj nao vem nunca a null porque controlámos isso nos ifs anteriores...
                    // Round Robin:
                    indexLastServer = (indexLastServer + 1) % dataServers.Count;
                    padInts.Add(uid, dServer);
                    MyPadInt myPadInt = new MyPadInt(uid, obj);
                    addPadInt(myPadInt);
                    Console.WriteLine("[CREATE] PadInt " + uid + " stored on " + dServer.remoteServer.name );
                    Console.WriteLine("---");
                    return obj;
                } else {
                    Console.WriteLine("[!CREATE] Error: PadInt " + uid + " already exists." );
                    Console.WriteLine("---");
                    return null;
                }
            }

            // Se o server a que esse objecto pertence estiver em Freeze ou Fail, faz sentido
            // devolvermos o objecto em cache? Não estamos a violar o comportamento suposto?
            //
            // mais: ao devolver o url ao client, nao devemos tambem ir buscar o objecto e por na cache?
            //
            // Se a cache contem o PadInt
            //  retornamos PadInt
            // Caso contrario
            //  retornamos url
            public PadIntInfo AccessPadInt(int uid) {
                Console.WriteLine("[ACCESS] Client requests PadInt with id " + uid);
                if (padIntsCache.Contains(uid)) {
                    IPadInt obj = (IPadInt) padIntsCache[uid];
                    PadIntInfo padIntInfo = new PadIntInfo(obj);
                    Console.WriteLine("[ACCESS] PadInt " + uid + " returned from the cache. " );
                    Console.WriteLine("---");
                    return padIntInfo;
                }
                else if (padInts.Contains(uid)) {
                    DataServerInfo dServer = (DataServerInfo)padInts[uid];
                    PadIntInfo padIntInfo = new PadIntInfo(dServer.URL);
                    Console.WriteLine("[ACCESS] Returned " + dServer.remoteServer.name + "'s URL, to further access PadInt " + uid +".");
                    Console.WriteLine("---");
                    IPadInt padInt = dServer.remoteServer.load(uid);
                    MyPadInt myPadInt = new MyPadInt(uid, padInt);
                    addPadInt(myPadInt);
                    return padIntInfo;
                }
                else { //PadInt nao existe
                    Console.WriteLine("[!ACCESS] Error: PadInt " + uid + " does not exist.");
                    Console.WriteLine("---");
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
                Console.WriteLine("Server " + remoteServer.name + " registered.");
                Console.WriteLine("---");
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
                Console.WriteLine("Client " + url + " registered.");
                Console.WriteLine("---");
            }

            /* ------------------------ 2PC ------------------------------ */
                /*            ... E transacções ...                    */

            // interlocked -> ver: msdn.microsoft.com/en-us/library/dd78zt0c.aspx
            public int TxBegin(String clientUrl) {
                int txId;
                Console.WriteLine("[TxBegin] Client request");
                lock (this) {
                    txId = transactionId;
                    Interlocked.Increment(ref transactionId);
                    Transaction tx = new CommittableTransaction();
                    MyTransaction t = new MyTransaction(txId);
                    clientTransactions.Add(clientUrl, t);
                    Console.WriteLine("---");
                    return txId;
                }
            }

            public bool TxCommit(int txId) {
                Console.WriteLine("[TxCommit] Client request");
                MyTransaction t; //TODO: get the right transaction 
                 foreach (IDataServer p in t.Participants){
                     _myCommitDecision = _myCommitDecision && p.canCommit(t);
                 }
                 if (_myCommitDecision) {
                     Console.WriteLine("[TxCommit] Every Server voted Yes");
                     foreach (IDataServer p in t.Participants) {
                         p.doCommit(t);
                     }
                     foreach (IDataServer p in t.Participants) {
                         if (!p.haveCommited(t)) {
                             Console.WriteLine("[TxCommit] Some server failed to commit! Need rollback and abort.");
                             _myCommitDecision = false;
                             // atencao: se algum ja fez commit mesmo, como é que agora aborta? rollback?
                             p.doAbort(t);
                         }
                     }
                  } else {
                      Console.WriteLine("[TxCommit] Some Server voted No.");
                      _myCommitDecision = false;
                      TxAbort(t);
                  }
                 Console.WriteLine("---");  
                return false;
            }


            public bool TxAbort(MyTransaction t) {
                Console.WriteLine("[TxAbort] Client Request.");
                _myCommitDecision = false;
                foreach (IDataServer p in t.Participants) {
                    p.doAbort(t);
                }
                Console.WriteLine("---");  
                return false;
            }

            public bool getDecision(MyTransaction t) {
                Console.WriteLine("[getDecision] Server Request.");
                Console.WriteLine("---");  
                return _myCommitDecision;
            }
        }

        class Program {
            static void Main(string[] args) {
                TcpChannel channel = new TcpChannel(9999);
                ChannelServices.RegisterChannel(channel, true);

                Master master = new Master();

                RemotingServices.Marshal(master, "MasterServer", typeof(IMasterServer));

                System.Console.WriteLine("Started Master Server...");
                Console.WriteLine("---");
                System.Console.ReadKey();
            }
        }
    }
}