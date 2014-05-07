using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Threading;
using System.Net.Sockets;
using System.Diagnostics;
using System.Timers;

namespace PADI_DSTM {

    namespace DataServer {

        public delegate Lock AsyncGetLockCaller(LockType type, int padIntId, int transactionId);

        class SingletonCounter {

            private int lockcounter;

            public int Lockcounter {
                get { return lockcounter; }
                set { lockcounter = value; }
            }

            private static SingletonCounter instance;
            private SingletonCounter() { }

            public static SingletonCounter Instance {
                get {
                    if (instance == null) {
                        instance = new SingletonCounter();
                    }
                    return instance;
                }
            }

            public void incrementLockCounter() {
                this.lockcounter++;
            }

            public void decrementLockCounter() {
                this.lockcounter--;
            }

        }

        enum State { Normal, Freeze, Fail }

        public class Server : MarshalByRefObject, IDataServer {

            private State _state;
            private Object _stateLockObj;
            private String _name;
            private String _url;
            private bool _isPrimary;
            private int _primaryPort;
            private int _slavePort;
            private IMasterServer _masterServer;
            private IDataServer _primaryServer;
            private IDataServer _slaveServer;
            public String _primaryName;
            private LockManager _lockManager;
            private int _pingCounter = 0;
            private Ping pingService;

            public Object CounterLock = new Object();

            private const String urlMaster = "tcp://localhost:9999/MasterServer";

            private Dictionary<int, ServerTransaction> transactions = 
                new Dictionary<int, ServerTransaction>();

            private Hashtable padInts = new Hashtable();

            public IMasterServer MasterServer {
                get { return _masterServer; }
            }

            public Object StateLockObj {
                get { return _stateLockObj; }
            }

            public LockManager lockManager {
                get { return _lockManager; }
            }

            public Dictionary<int, ServerTransaction> Transactions {
                get { return transactions; }

            }

            public int PingCounter {
                get {                  
                        return _pingCounter;
                    }
                }
            

            public void incrementCounter() { 
                lock(CounterLock) {
                    _pingCounter++;
                    Console.WriteLine("Ping counter incremented");
                }
            }

            public Server(String name, String url, int primaryPort) {
                try {
                    _name = name;
                _url = url;
                _masterServer = (IMasterServer)Activator.GetObject(typeof(IMasterServer), urlMaster);
                _state = State.Normal;
                _stateLockObj = new Object();
                _isPrimary = true;
                _slavePort = 0;
                _lockManager = new LockManager();
                }  catch (RemotingException re) {
                     Console.WriteLine("[Server]:\n" + re);
                     throw new OperationException("Server " + name + "cannot start: MasterServer is not avaiable.");
                 }
            }

            public Server(String name, String url, int primaryPort, int slavePort) {
                try {
                    _name = name;
                    _url = url;
                    _masterServer = (IMasterServer)Activator.GetObject(typeof(IMasterServer), urlMaster);
                    _state = State.Normal;
                    _stateLockObj = new Object();
                    _primaryPort = primaryPort;
                    _slavePort = slavePort;
                    _isPrimary = false;
                    _primaryName = "ServerAt" + primaryPort;
                    _lockManager = new LockManager();
                }  catch (RemotingException re) {
                     Console.WriteLine("[Server]:\n" + re);
                     throw new OperationException("Server " + name + "cannot start: MasterServer is not avaiable.");
                 }
            }

            public void register() {
                try {
                    _masterServer.registerServer(_url);
                } catch (RemotingException re) {
                    Console.WriteLine("[register]:\n" + re);
                    throw new OperationException("Server " + name + "cannot register: MasterServer is not avaiable to registerServer.");
                }
            }

            public String name {
                get { return _name; }
                set { _name = value; }
            }

            public String URL {
                get { return _url; }
                set { _url = value; }
            }

            public bool isFail {
                get { return _state == State.Fail; }
            }

            public bool isNormal {
                get {
                    return _state == State.Normal;
                }
            }

            public bool isFreeze {
                get {
                    return _state == State.Freeze;
                }
            }

            public bool Freeze() {
                lock (_stateLockObj) {
                    if (isFail) {
                        while (true) ;
                        //throw new RemotingException("Server is in Fail Mode");
                    }
                    if (isNormal) {
                        lock (_stateLockObj) {
                            _state = State.Freeze;
                        }
                        Console.WriteLine("[FREEZE] Dataserver " + _name + " changed to [Freeze].");
                        Console.WriteLine("---");
                        return true;
                    }
                }
                return false;
            }

            public bool Fail() {
                lock (_stateLockObj) {
                    if (isFreeze) {
                        lock (SingletonCounter.Instance) {
                            SingletonCounter.Instance.incrementLockCounter();
                            Monitor.Wait(SingletonCounter.Instance);
                        }
                    }
                    if (isNormal) {
                        lock (_stateLockObj) {
                            _state = State.Fail;
                        }
                        Console.WriteLine("[FAIL] Dataserver " + _name + " changed to [Fail].");
                        Console.WriteLine("---");
                        return true;
                    } else {
                        return false;
                    }
                }
            }

            public bool Recover() {
                lock (_stateLockObj) {
                    if (isNormal) {
                        return false;
                    } else if (isFreeze) {
                        lock (SingletonCounter.Instance) {
                            while (SingletonCounter.Instance.Lockcounter > 0) {
                                SingletonCounter.Instance.decrementLockCounter();
                                Monitor.Pulse(SingletonCounter.Instance);
                            }
                        }
                    }

                    _state = State.Normal;
                }
                Console.WriteLine("[RECOVER] Dataserver " + _name + " changed to [OK].");
                Console.WriteLine("---");
                return true;
            }

            public String Status() {
                lock (_stateLockObj) {
                    if (isFail) {
                        Console.WriteLine(_name + " Status: [Fail].");
                        return _name + " Status: [Fail].";
                    } else if (isFreeze) {
                        Console.WriteLine(_name + " Status: [Freeze].");
                        return _name + " Status: [Freeze].";
                    } else {
                        Console.WriteLine(_name + " Status: [OK].");
                        return _name + " Status: [OK].";
                    }
                }
            }

            public IPadInt store(int uid) {
                lock (_stateLockObj) {
                    if (isFail) {
                        Console.WriteLine("[!STORE] Error: DataServer " + name + " is set to [Fail] mode!");
                        Console.WriteLine("---");
                        while (true) ;
                        //throw new RemotingException("Server is in Fail Mode");

                    } else if (isFreeze) {
                        lock (SingletonCounter.Instance) {
                            SingletonCounter.Instance.incrementLockCounter();
                            Monitor.Wait(SingletonCounter.Instance);
                        }
                    }
                }
                if (!padInts.Contains(uid)) {

                    PadInt obj = new PadInt(uid, this);
                    padInts.Add(uid, obj);
                    Console.WriteLine("[STORE] DataServer " + name + " stored PadInt " + uid);
                    Console.WriteLine("---");
                    return obj;
                }
                return null;
            }

            private String PortToUrl(int port) {
                return "tcp://localhost:" + port + "/Server";
            }

            public void reportFailure() {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = @"..\..\..\DataServer\bin\Debug\DataServer.exe";
                startInfo.Arguments = (UrlToPort(_url) + 1) + " " + UrlToPort(_url);
                Process p = Process.Start(startInfo);

                Thread.Sleep(TimeSpan.FromSeconds(2));

                if (!_isPrimary) {

                    try {
                        _masterServer.registerNewPrimaryServer(PortToUrl(_primaryPort), _url);

                        /*
                        _masterServer.registerNewPrimaryServer(PortToUrl(_primaryPort), _url);
                        _isPrimary = true;
                        _primaryPort = 0;
                          */
                    } catch (RemotingException re) {
                        Console.WriteLine("[reportFailure]:\n" + re);
                        throw new OperationException("Server " + _url + "cannot reportFailure: MasterServer is not avaiable to registerNewPrimaryServer.");

                    }
               }
            }


            public IPadInt load(int uid) {
                lock (_stateLockObj) {
                    if (isFail) {
                        Console.WriteLine("[!LOAD] Error: DataServer " + name + " is set to [Fail] mode!");
                        Console.WriteLine("---");
                        while (true) ;
                        //throw new RemotingException("Server is in Fail Mode");

                    } else if (isFreeze) {
                        lock (SingletonCounter.Instance) {
                            SingletonCounter.Instance.incrementLockCounter();
                            Monitor.Wait(SingletonCounter.Instance);
                        }
                    }
                }
                if (padInts.Contains(uid)) {
                    Console.WriteLine("[LOAD] DataServer " + name + " load PadInt " + uid);
                    Console.WriteLine("---");
                    return (IPadInt)padInts[uid];

                }
                // este caso nunca acontece (o master testa o mesmo)
                Console.WriteLine("[!LOAD] DataServer " + name + " cannot load PadInt " + uid + ": does not exist.");
                Console.WriteLine("---");
                return null;

            }



            // 2PC INTERFACE COMMANDS

            // commando para ele se juntar aos participantes de uma transacção!!!
            // aqui ou no master? :\


            public bool canCommit(int TxId) {
                lock (_stateLockObj) {
                    if (isFail) {
                        Console.WriteLine("[!canCommit] Error: DataServer " + name + " is set to [Fail] mode!");
                        Console.WriteLine("---");
                        while (true) ;
                        //throw new RemotingException("Server is in Fail Mode");

                    } else if (isFreeze) {
                        lock (SingletonCounter.Instance) {
                            SingletonCounter.Instance.incrementLockCounter();
                            Monitor.Wait(SingletonCounter.Instance);
                        }
                    }
                }
                Console.WriteLine("[canCommit] Master Request with id " + TxId);
                Console.WriteLine("---");

                if (!this.Transactions.ContainsKey(TxId)) {
                    Console.WriteLine("Error transaction not found");
                    return false;
                }

                return true;
            }

            public bool doCommit(int TxId) {
                lock (_stateLockObj) {
                    if (isFail) {
                        Console.WriteLine("[!doCommit] Error: DataServer " + name + " is set to [Fail] mode!");
                        Console.WriteLine("---");
                        while (true) ;

                    } else if (isFreeze) {
                        lock (SingletonCounter.Instance) {
                            SingletonCounter.Instance.incrementLockCounter();
                            Monitor.Wait(SingletonCounter.Instance);
                        }
                    }
                }
                Console.WriteLine("[doCommit] Master Request with id " + TxId);
                Console.WriteLine("---");
                if (!this.Transactions.ContainsKey(TxId)) {
                    Console.WriteLine("Error transaction not found : " + TxId);
                    return false;
                }

                ServerTransaction transaction = transactions[TxId];
                
                //Let's release the locks (reverse order/LIFO)
                Lock l = transaction.popLock();
                while (l != null) {
                    _lockManager.releaseLock(l);
                    l = transaction.popLock();
                }
                return true;
            }

            private int UrlToPort(String url) {
                String[] aux = url.Split(':');
                String[] aux2 = aux[2].Split('/');
                return Convert.ToInt32(aux2[0]);
            }

            public void receiveHeartBeat(String type) {                
               if (type.Equals("ping")) {
                   incrementCounter();
                   Console.WriteLine("Received Ping " + PingCounter + " " + type);    
               }
            }

            public bool doAbort(int TxId) {
                lock (_stateLockObj) {
                    if (isFail) {
                        Console.WriteLine("[!doAbort] Error: DataServer " + name + " is set to [Fail] mode!");
                        Console.WriteLine("---");
                        while (true) ;
                        //throw new RemotingException("Server is in Fail Mode");

                    } else if (isFreeze) {
                        lock (SingletonCounter.Instance) {
                            SingletonCounter.Instance.incrementLockCounter();
                            Monitor.Wait(SingletonCounter.Instance);
                        }
                    }
                }
                Console.WriteLine("[doAbort] Master Request with id " + TxId);
                Console.WriteLine("---");

                if (!this.Transactions.ContainsKey(TxId)) {
                    Console.WriteLine("Error transaction not found : " + TxId);
                    return false;
                }

                ServerTransaction transaction = Transactions[TxId];
                transaction.rollback();
                //Let's release the locks (reverse order/LIFO)
                Lock l = transaction.popLock();
                while (l != null) {
                    _lockManager.releaseLock(l);
                    l = transaction.popLock();
                }
                transactions.Remove(TxId);
                return true;
            }

            public bool haveCommited(int TxId) {
                lock (_stateLockObj) {
                    if (isFail) {
                        Console.WriteLine("[!haveCommited] Error: DataServer " + name + " is set to [Fail] mode!");
                        Console.WriteLine("---");
                        while (true) ;
                        //throw new RemotingException("Server is in Fail Mode");

                    } else if (isFreeze) {
                        lock (SingletonCounter.Instance) {
                            SingletonCounter.Instance.incrementLockCounter();
                            Monitor.Wait(SingletonCounter.Instance);
                        }
                    }
                }
                Console.WriteLine("[haveCommitted] Master Request with id " + TxId);
                Console.WriteLine("---");
                return true;
            }

            public override object InitializeLifetimeService() {
                return null;
            }

            public void makeConnection(int primaryPort) {

                try {
                    String url = "tcp://localhost:" + primaryPort + "/" + "Server";
                    _primaryServer = (IDataServer)Activator.GetObject(typeof(IDataServer), url);
                    _primaryServer.connect(_slavePort);
                    pingService = new Ping(_primaryServer, this);

                    pingService.StartSend();
                } catch (RemotingException re) {
                     Console.WriteLine("[makeConnection]:\n" + re);
                     throw new OperationException("Server " + name + "cannot makeConnection: MasterServer is not avaiable to connect.");
                 }

            }

            public void connect(int slavePort) {
                try {
                    _slavePort = slavePort;

                    _slaveServer = (IDataServer)Activator.GetObject(typeof(IDataServer), PortToUrl(_slavePort));

                    Console.WriteLine("Backup ServerAt{0} registered", slavePort);

                    if (_isPrimary) {
                        Console.WriteLine(" Connected with the Slave at: " + PortToUrl(_slavePort));
                        
                        pingService = new Ping(_slaveServer, this);
                         
                        pingService.StartReceive();
                    }
                } catch (RemotingException re) {
                    Console.WriteLine("[connect]:\n" + re);
                    throw new OperationException("Server " + name + "cannot connect: SlaveServer is not avaiable.");

                }
            }

            public class Ping {

                private IDataServer _otherServer;
                private Server _myServer;
                private System.Timers.Timer _tSend;
                private System.Timers.Timer _tReceive;
                private static int lastCounterValue;

                public Ping(IDataServer otherServer, Server myServer) {
                    _otherServer = otherServer;
                    _myServer = myServer;

                    _tSend = new System.Timers.Timer();
                    _tSend.Elapsed += new ElapsedEventHandler(SendPing);
                    _tSend.Interval = 3000;

                    _tReceive = new System.Timers.Timer();
                    _tReceive.Elapsed += new ElapsedEventHandler(Receive);
                    _tReceive.Interval = 10000;

                    lastCounterValue = _myServer.PingCounter;
                }

                public void changeDataServer(IDataServer server) {
                    _otherServer = server;
                }
                private void Receive(object source, ElapsedEventArgs e) {
                    Console.WriteLine("[Receive]: Counter = "+ _myServer.PingCounter);
                    if (lastCounterValue == _myServer.PingCounter) {
                        _tSend.Stop();
                        Console.WriteLine("Failed Heartbeats not received the server is down!!!");
                        _myServer.reportFailure();
                    }
                    lastCounterValue = _myServer.PingCounter;
                    //Console.WriteLine("Last counter value = " + lastCounterValue);
                }

                private void SendPing(object source, ElapsedEventArgs e) {
                    Console.WriteLine("Sending Ping");
                    int i;
                    //for (i = 0; i < 3; i++) {
                      //  try {
                    _otherServer.receiveHeartBeat("ping");
                        //    break;
                        //} catch (RemotingException) {
                          //  ; // do nothing
                        //}
                    //}
                    //if (i == 3) { 
                    //Im the new Master


                    //}
                }

                public void StartSend() {
                    _tSend.Start();
                }

                public void StartReceive() {
                    _tReceive.Start();
                }
            }


            public void startPing() {
                if (_isPrimary) {
                    pingService = new Ping(_slaveServer, this);
                } else {
                    pingService = new Ping(_primaryServer, this);
                }
            }
        }

        class Program {

            static void Main(string[] args) {

                Console.SetWindowSize(49, 18);

                int port = 0;
                TcpChannel channel = null;
                bool isPrimary = false;
                int primaryPort = 0;


                if (args.Count() == 0) {
                    Console.WriteLine("Invoked with no argumments.");
                    Console.WriteLine("Trying to assign port in range 2001...65535");
                    port = 2001;
                    isPrimary = true;
                    while (channel == null) {
                        try {
                            channel = new TcpChannel(port);
                        } catch (SocketException) {
                            port++;
                        }
                    }
                } else if (args.Count() == 1) {
                    Console.WriteLine("Invoked with one argument (port).");
                    Console.WriteLine("Trying to assign received port to Primary Server.");
                    try {
                        port = Convert.ToInt32(args[0]);
                        isPrimary = true;

                        while (channel == null) {
                            try {
                                channel = new TcpChannel(port);
                            } catch (SocketException) {
                                port++;
                            }
                        }

                    } catch (FormatException fe) {
                        Console.WriteLine("Malformed Args: {0}", args[0]);
                        Console.WriteLine(fe);
                        Console.WriteLine("Press any key to exit...");
                        Console.ReadKey();
                        return;
                    }
                } else if (args.Count() == 2) {
                    Console.WriteLine("Invoked with two arguments.");
                    try {
                        port = Convert.ToInt32(args[0]);
                        isPrimary = false;
                        primaryPort = Convert.ToInt32(args[1]);

                        while (channel == null) {
                            try {
                                channel = new TcpChannel(port);
                            } catch (SocketException) {
                                port++;
                            }
                        }

                    } catch (FormatException fe) {
                        Console.WriteLine("Malformed Args: {0} {1}", args[0], args[1]);
                        Console.WriteLine(fe);
                        Console.WriteLine("Press any key to exit...");
                        Console.ReadKey();
                        return;
                    }
                }


                ChannelServices.RegisterChannel(channel, false);
                String name = "Server";
                String url = "tcp://localhost:" + port + "/" + name;

                String ServerName = name + "At" + port;
                Server server = null;


                if (isPrimary) {
                    try {
                        server = new Server(ServerName, url, primaryPort);
                    } catch (OperationException) {
                        return;
                    }
                    RemotingServices.Marshal(server, name, typeof(IDataServer));
                    try {
                        server.register();
                    } catch (RemotingException) {
                        return;
                    }
                    Console.WriteLine("Started " + ServerName + " (Primary)...");
                } else {
                    try {
                        server = new Server(ServerName, url, primaryPort, port);
                    } catch (OperationException) {
                        return;
                    }
                    RemotingServices.Marshal(server, name, typeof(IDataServer));
                    try {
                        server.makeConnection(primaryPort);

                    } catch (RemotingException) {
                        return;
                 }
                    Console.WriteLine("Started " + ServerName + " (Backup of " + server._primaryName + ")...");
                }

                Console.WriteLine("---");
                Console.ReadKey();
            }
        }
    }
}