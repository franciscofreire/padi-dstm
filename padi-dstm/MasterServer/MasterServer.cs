﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Threading;
using System.Transactions;
using System.Diagnostics;

namespace PADI_DSTM {

    namespace MasterServer {

        /** Class to store a transaction information
         * - Transaction ID
         * - Client that owns it
         * - Participant Data Servers
         * */
        public class MyTransaction {
            
            private int txId;
            private ArrayList participants;
            private String client;

            public MyTransaction(int txId, String client) {
                this.txId = txId;
                this.client = Client;
                this.participants = new ArrayList();
            }

            public String Client {
                get { return client; }
            }

            public int TxId {
                get { return txId; }
            }

            public ArrayList Participants {
                get { return participants; }
            }
        }

        /* Master Class */
        class Master : MarshalByRefObject, IMasterServer {

            /** Class to store a DataServer information
             * - Remote object to the DataServer
             * - URL of the DataServer
             * - Name of the DataServer
             * - ID of the DataServer
             * */
            private class DataServerInfo {

                private IDataServer _myRemoteServer;
                private String _myURL;
                private String _myName;
                private int _id;

                public DataServerInfo(String url, IDataServer remoteServer, String name, int id) {
                    _myURL = url;
                    _myRemoteServer = remoteServer;
                    _myName = name;
                    _id = id;
                }

                public DataServerInfo(int id) {
                    _myURL = null;
                    _myRemoteServer = null;
                    _myName = null;
                    _id = id;
                }
                public IDataServer remoteServer {
                    get { return _myRemoteServer; }
                    set { _myRemoteServer = value; }
                }

                public String URL {
                    get { return _myURL; }
                    set { _myURL = value; }
                }

                public String Name {
                    get { return _myName; }
                    set { _myName = value; }
                }

                public int Id {
                    get { return _id; }
                    set { _id = value; }
                }

                public override bool Equals(Object obj) {
                    if (obj == null || GetType() != obj.GetType())
                        return false;

                    DataServerInfo dsInfo = (DataServerInfo)obj;
                    return this.Id == dsInfo.Id;
                }

                public override int GetHashCode() {
                    return _id;
                }
            }

            /** Class to store a Client information
             * - Remote object to the Client
             * - URL of the Client
             * */
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

            /** Class that associates PadInts with their ID for Cache purposes
             * - ID of the PadInt
             * - PadInt object
             * */
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

                public MyPadInt(int uid) {
                    this.uid = uid;
                    this.padInt = null;
                }
            }

            private const int CACHE_SIZE = 20;

            // Hashtable with information regarding objects' location
            // Pair (int, ServerInfo)
            private Hashtable padInts = new Hashtable();

            // Hashtable - cache of PadInts
            // Pair (int, IPadInt)
            private ArrayList padIntsCache = new ArrayList();

            // ArrayList of Data Servers (DataServerInfo)
            private ArrayList dataServers = new ArrayList();

            //ArrayList of Clients (ClientInfo)
            private ArrayList clients = new ArrayList();

            // Index for RoundRobin
            private int indexLastServer = 0;
            // ATENÇÃO!!! Quando se perde/remove um DataServer, precisa de correcão (-1?)

            // Transaction Id
            private static int transactionId = 0;

            // Hashtable of tIds and their transactions
            Hashtable clientTransactions = new Hashtable();

            // Voting decision for 2pc (maybe it should be an attribute of MyTransaction)
            private bool _myCommitDecision = true;

            private int id; 

            private void clearCache() {
                padIntsCache.Clear();
            }

            public String Status() {
                try {
                    String text = "MasterServer Status: [OK, I never fail!].\r\n";

                    foreach (DataServerInfo server in dataServers) {
                        text += server.remoteServer.Status() + "\r\n";
                    }
                    return text;
                } catch (RemotingException re) {
                    Console.WriteLine("[Status]:\n" + re);
                    throw new OperationException("Status operation failed.");
                }
            }

            public bool join(int txId, String url) {
                Console.WriteLine("[Join]: {0} is participant in Tx{1}", url, txId);
                MyTransaction tr;
                //TODO testar se a transacao e servidor existem 
                tr = (MyTransaction)clientTransactions[txId];
                DataServerInfo dsInfo = null;

                foreach (DataServerInfo ds in dataServers) {
                    if (ds.URL.Equals(url))
                        dsInfo = ds;
                }

                if (!tr.Participants.Contains(dsInfo)) {
                    tr.Participants.Add(dsInfo);
                    return true;
                } else {
                    return false;
                }
            }

            private void addPadInt(MyPadInt obj) {
                int size = padIntsCache.Count;
                if (size == CACHE_SIZE) {
                    // remover o PadInt mais antigo
                    Console.WriteLine("Oldest PadInt removed ");
                    padIntsCache.RemoveAt(0);
                }
                padIntsCache.Add(obj);
            }
            

            public IPadInt CreatePadInt(int uid) {
                try {
                    Console.WriteLine("[CREATE] Client wants to create PadInt with id " + uid);
                    if (!padInts.Contains(uid)) {

                        if (dataServers.Count == 0) {
                            Console.WriteLine("[!CREATE] Error: There are no available DataServers");
                            Console.WriteLine("---");
                            return null;
                        }
                        DataServerInfo dServer = (DataServerInfo)dataServers[indexLastServer];
                        while (dServer.remoteServer.isFail) {
                            Console.WriteLine("[CREATE] DataServer " + dServer.Name /*dServer.remoteServer.name*/ + " is set to [Fail]: Passing his turn on Round Robin");
                            indexLastServer = (indexLastServer + 1) % dataServers.Count; // salta um índice
                            dServer = (DataServerInfo)dataServers[indexLastServer];
                        }
                        IPadInt obj = dServer.remoteServer.store(uid);
                        // obj nao vem nunca a null porque controlámos isso nos ifs anteriores...
                        // Round Robin:
                        indexLastServer = (indexLastServer + 1) % dataServers.Count;
                        padInts.Add(uid, dServer);
                        MyPadInt myPadInt = new MyPadInt(uid, obj);
                        addPadInt(myPadInt);
                        Console.WriteLine("[CREATE] PadInt " + uid + " stored on " + dServer.Name /*dServer.remoteServer.name*/);
                        Console.WriteLine("---");
                        return obj;
                    } else {
                        Console.WriteLine("[!CREATE] Error: PadInt " + uid + " already exists.");
                        Console.WriteLine("---");
                        return null;
                    }
                } catch (RemotingException re) {
                    Console.WriteLine("[CreatePadInt]:\n" + re);
                    throw new OperationException("CreatePadInt operation at PadInt " + uid + "failed.");
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
                try {
                    Console.WriteLine("[ACCESS] Client requests PadInt with id " + uid);
                    if (padIntsCache.Contains(new MyPadInt(uid))) {
                        int index = padIntsCache.IndexOf(new MyPadInt(uid));
                        MyPadInt myObj = (MyPadInt)padIntsCache[index];
                        IPadInt obj = myObj.PadInt;
                        PadIntInfo padIntInfo = new PadIntInfo(obj);
                        Console.WriteLine("[ACCESS] PadInt " + uid + " returned from the cache. ");
                        Console.WriteLine("---");
                        return padIntInfo;
                    } else if (padInts.Contains(uid)) {
                        DataServerInfo dServer = (DataServerInfo)padInts[uid];
                        PadIntInfo padIntInfo = new PadIntInfo(dServer.URL);
                        Console.WriteLine("[ACCESS] Returned " + dServer.Name /*dServer.remoteServer.name*/ + "'s URL, to further access PadInt " + uid + ".");
                        Console.WriteLine("---");
                        IPadInt padInt = dServer.remoteServer.load(uid);
                        MyPadInt myPadInt = new MyPadInt(uid, padInt);
                        addPadInt(myPadInt);
                        return padIntInfo;
                    } else { //PadInt nao existe
                        Console.WriteLine("[!ACCESS] Error: PadInt " + uid + " does not exist.");
                        Console.WriteLine("---");
                        return null;
                    }
                } catch (RemotingException re) {
                    Console.WriteLine("[AccessPadInt]:\n" + re);
                    throw new OperationException("AccessPadInt operation at PadInt " + uid + "failed.");
                }
            }

            public void registerNewPrimaryServer(String newServerUrl, int Id) {
                Console.WriteLine(" Register New Primary Server ");
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = @"..\..\..\DataServer\bin\Debug\DataServer.exe";
                startInfo.Arguments = (Id + " " + UrlToPort(newServerUrl));
                Process p = Process.Start(startInfo);
                Thread.Sleep(TimeSpan.FromSeconds(2));
                DataServerInfo dsInfoFound = null;
                DataServerInfo dsInfoOld = new DataServerInfo(Id);

                foreach (DataServerInfo ds in dataServers) {
                     if (ds.Equals(dsInfoOld))
                        dsInfoFound = ds;
                }
                if (dsInfoFound == null) {
                    //nothing to do here, server does not exists
                    return;
                }
                try {
                    //Creating DataServer Reference
                    IDataServer remoteServerRef = (IDataServer)Activator.GetObject(typeof(IDataServer), newServerUrl);
                    dsInfoFound.remoteServer = remoteServerRef;
                    dsInfoFound.URL = remoteServerRef.URL;
                    dsInfoFound.Name = remoteServerRef.name;    // <----- Atenção aqui (RTT), ha alternativa?
                } catch (RemotingException re) {
                    Console.WriteLine("[registerNewPrimaryServer]:\n" + re);
                    return;
                }
                this.clearCache();
                Console.WriteLine("[registerNewPrimaryServer]: Operation Succeed!\n");
            }





            private int UrlToPort(String url) {
                String[] aux = url.Split(':');
                String[] aux2 = aux[2].Split('/');
                return Convert.ToInt32(aux2[0]);
            }

            private String PortToUrl(int port) {
                return "tcp://localhost:" + port + "/Server";
            }

            public int registerServer(String url) {
                foreach (DataServerInfo server in dataServers) {
                    if (server.URL.Equals(url))
                        return -1;
                }
                // obter referencia remota e registar servidor
                String[] aux = url.Split(':');
                String[] aux2 = aux[2].Split('/');
                int primary = Convert.ToInt32(aux2[0]);
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = @"..\..\..\DataServer\bin\Debug\DataServer.exe";
                startInfo.Arguments = (primary + 1) + " " + primary;
                Process p = Process.Start(startInfo);
                try {
                    IDataServer remoteServer = (IDataServer)Activator.GetObject(typeof(IDataServer), url);
                    //DataServerInfo serverInfo = new DataServerInfo(url, remoteServer, remoteServer.name,id); // <----- Atenção aqui (RTT), ha alternativa? 
                    // Quero criar um DataServerInfo cujo Id seja a sua porta... não o id do master (que é sempre zero)
                    DataServerInfo serverInfo = new DataServerInfo(url, remoteServer, remoteServer.name, primary); // <----- Atenção aqui (RTT), ha alternativa? 
                    dataServers.Add(serverInfo);
                    Console.WriteLine("Server " + serverInfo.Name /*remoteServer.name*/ + " registered.");
                    Console.WriteLine("---");
                    lock (this) {
                        Interlocked.Increment(ref id);
                    }
                    return id;
                } catch (RemotingException re) {
                    Console.WriteLine("[registerNewServer]:\n" + re);
                    return -1;
                }
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

            // interlocked -> ver: msdn.microsoft.com/en-us/library/dd78zt0c.aspx
            public int TxBegin(String clientUrl) {
                int txId;
                Console.WriteLine("[TxBegin] Client request");
                lock (this) {
                    txId = transactionId;
                    Interlocked.Increment(ref transactionId);
                    Transaction tx = new CommittableTransaction();
                    MyTransaction t = new MyTransaction(txId, clientUrl);
                    clientTransactions.Add(txId, t);
                    Console.WriteLine("---");
                    return txId;
                }
            }

            public bool TxCommit(int txId) {
                int faulty = 0;
                // Auxiliar ArrayList to save the DataServer to whom we successfully issued the canCommit
                ArrayList pServers = new ArrayList();
                Console.WriteLine("[TxCommit] Client request");
                if (!clientTransactions.ContainsKey(txId)) {
                    throw new TxException(txId, "Transaction with id " + txId + "does not exists!");
                }
                try {
                    lock (this) {
                        MyTransaction t = (MyTransaction)clientTransactions[txId];
                        _myCommitDecision = true;
                        for (int i = 0 ; i < t.Participants.Count; ++i){
                            faulty = i;
                            DataServerInfo ds = (DataServerInfo)t.Participants[i];
                            _myCommitDecision = _myCommitDecision && ds.remoteServer.canCommit(t.TxId);
                            pServers.Add(t.Participants[i]);
                        }
                        // Compara o tamanho dos arrays para saber se foi possivel fazer o canCommit a todos
                        // se não for igual, aos que fizeram canCommit manda agora abortar e retorna false
                        if (pServers.Count != t.Participants.Count) {
                            foreach (DataServerInfo pt in pServers) {
                                pt.remoteServer.doAbort(txId);
                            }
                            return false;
                        }
                        if (_myCommitDecision) {
                            Console.WriteLine("[TxCommit] Every Server voted Yes");
                            // try to commit infinite times
                            foreach (DataServerInfo p in t.Participants) {
                                while (true) {
                                    p.remoteServer.doCommit(t.TxId);
                                    break;
                                }
                            }
                        } else {
                            Console.WriteLine("[TxCommit] Some Server voted No.");
                            _myCommitDecision = false;
                            TxAbort(txId);
                        }
                    }
                    Console.WriteLine("---");
                    return true;
                } catch (RemotingException re) {
                    Console.WriteLine("[TxCommit]:\n" + re);
                    throw new TxException(txId, "TxCommit transaction with id " + txId + "failed. canCommit voting failed.");
                } catch (Exception) {
                    Console.WriteLine("TxCommit can not complete. One participant DataServer is failed.");
                    Console.WriteLine("The faulty DataServer will be removed from the participants of the Transaction.");
                    Console.WriteLine("The current transaction will be aborted!");
                    MyTransaction t = (MyTransaction)clientTransactions[txId];
                    t.Participants.RemoveAt(faulty);
                    TxAbort(txId);
                    throw new OperationException("TxCommit can not be executed. Server does not respond. This transaction was automatically aborted!");
                }
            }

            public bool TxAbort(int txId) {
                lock (this) {
                    Console.WriteLine("[TxAbort] Request.");
                    if (!clientTransactions.ContainsKey(txId)) {
                        throw new TxException(txId, "Transaction with id " + txId + "does not exists!");
                    }
                    MyTransaction t = (MyTransaction)clientTransactions[txId];
    
                    foreach (DataServerInfo p in t.Participants) {
                        try {
                            p.remoteServer.doAbort(t.TxId);
                        } catch (RemotingException re) {
                            Console.WriteLine("[TxAbort]:\n" + re);
                            throw new TxException(txId, "TxAbort transaction with id " + txId + "failed.");
                        } catch (Exception) {
                            //Console.WriteLine("TxAbort had one participant DataServer failed. Aborting the remaining...");
                        }
                    }
                    Console.WriteLine("---");
                    return true;
                }
            }
 
            public bool getDecision(int txId) {
                Console.WriteLine("[getDecision] Server Request.");
                Console.WriteLine("---");
                return _myCommitDecision;
            }

            public override object InitializeLifetimeService() {
                return null;
            }

        }
        
        class Program {
            static void Main(string[] args) {
                TcpChannel channel = new TcpChannel(9999);
                ChannelServices.RegisterChannel(channel, false);
                Master master = new Master();
                RemotingServices.Marshal(master, "MasterServer", typeof(IMasterServer));
                System.Console.WriteLine("Started Master Server...");
                Console.WriteLine("---");
                System.Console.ReadKey();
            }
        }
    }
}