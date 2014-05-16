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

            private TcpChannel _channel;
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
            private int _Id = 0;
            private int _lastTransactionId = 0;

            public int Id {
                get { return _Id; }
                set { _Id = value; }
            }

            public Object CounterLock = new Object();

            private const String urlMaster = "tcp://localhost:9999/MasterServer";

            private Dictionary<int, ServerTransaction> transactions = 
                new Dictionary<int, ServerTransaction>();

            private Hashtable padInts = new Hashtable();

            public Hashtable PadInts {
                get { return padInts; }
                set { padInts = value; }
            }

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
                    //Console.WriteLine("Ping counter incremented");
                }
            }

            public Server(TcpChannel channel, String name, String url, int primaryPort) {
                try {
                    _channel = channel;
                    _name = name;
                    _url = url;
                    _masterServer = (IMasterServer)Activator.GetObject(typeof(IMasterServer), urlMaster);
                    _state = State.Normal;
                    _stateLockObj = new Object();
                    _isPrimary = true;
                    _slavePort = 0;
                    _lockManager = new LockManager();
                } catch (RemotingException re) {
                    Console.WriteLine("[Server]:\n" + re);
                    throw new OperationException("Server " + name + "cannot start: MasterServer is not avaiable.");
                }
            }

            public Server(TcpChannel channel,String name, String url, int primaryPort, int slavePort, int Id) {
                try {
                    _channel = channel;
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
                    _Id = Id;
                }  catch (RemotingException re) {
                     Console.WriteLine("[Server]:\n" + re);
                     throw new OperationException("Server " + name + "cannot start: MasterServer is not avaiable.");
                 }
            }

            public void register() {
                try {
                      _Id = _masterServer.registerServer(_url);
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

            private void killProcess() {
                Thread.Sleep(1000);
                Process.GetCurrentProcess().Kill();
            }

            public bool Fail() {
                pingService.Fail();

                RemotingServices.Disconnect(this);
                ChannelServices.UnregisterChannel(_channel);
                Thread t = new Thread(killProcess);
                t.Start();
                return true;
                /*lock (_stateLockObj) {
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
                }*/
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

                    if (_isPrimary) {
                        _slaveServer.store(uid);
                    }
                    

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
                /*
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = @"..\..\..\DataServer\bin\Debug\DataServer.exe";
                startInfo.Arguments = (UrlToPort(_url) + 1) + " " + UrlToPort(_url);
                Process p = Process.Start(startInfo);
                */
                //Thread.Sleep(TimeSpan.FromSeconds(2));

                _masterServer.registerNewPrimaryServer(_url, _Id);
                // lança no master;
                _isPrimary = true;
                _primaryPort = 0;
            }

            public void receiveUpdateAll(Hashtable mypadInts) {
                padInts = mypadInts;
                Console.WriteLine("I got your Padint !");
                      
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

                if (_isPrimary) {
                    transaction.updatetobackup();
                    Dictionary<int, int> updates= new Dictionary<int,int>();
                    updates = transaction.Valuestobackup;
                    _slaveServer.receiveupdatefromprimary(updates,TxId);

                }

                //transaction.
                //Let's release the locks (reverse order?)
                _lockManager.unLock(TxId);
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
                //Let's release the locks (reverse order?)
                _lockManager.unLock(TxId);

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
            
            public void receiveupdatefromprimary(Dictionary<int, int> updatetobackup, int Tid)
            {
                foreach (KeyValuePair<int, int> entry in updatetobackup) {
                    if (padInts.Contains(entry.Key)) {
                        PadInt p = (PadInt) padInts[entry.Key];
                        p.Value = entry.Value;
                        Console.WriteLine("<Debug Mode> Padint with id: " + p.Id + "has now the value: " + p.Value);
                    }                     
                }
                _lastTransactionId = Tid;
                Console.WriteLine("Received update from primary according to the transaction id: " + Tid);
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
                    _slaveServer.receiveUpdateAll(PadInts);

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
                private System.Timers.Timer _tfailSend;
                private static int lastCounterValue;

                public Ping(IDataServer otherServer, Server myServer) {
                    _otherServer = otherServer;
                    _myServer = myServer;

                    _tSend = new System.Timers.Timer();
                    _tSend.Elapsed += new ElapsedEventHandler(SendPing);
                    _tSend.Interval = 5000;


                    _tReceive = new System.Timers.Timer();
                    _tReceive.Elapsed += new ElapsedEventHandler(Receive);
                    _tReceive.Interval = 7500;

                    _tfailSend = new System.Timers.Timer();
                    _tfailSend.Elapsed += new ElapsedEventHandler(FailSend);
                    _tfailSend.Interval = 10000;

                    lastCounterValue = _myServer.PingCounter;
                    //receivedHeartBeat=false;
                }

                public void Fail()
                {
                    StopSend();
                    StopReceive();
                    StopFailSend();
                    RemotingServices.Disconnect(_myServer);
                    ChannelServices.UnregisterChannel(_myServer._channel);
                }

                private void FailSend(object sender, ElapsedEventArgs e) {
                    StopSend();
                    StopReceive();
                    StopFailSend();
                    

                               
                
              
                    
                }

                public void changeDataServer(IDataServer server) {
                    _otherServer = server;
                }
                private void Receive(object source, ElapsedEventArgs e) {
                   // Console.WriteLine("[Receive]: Counter = "+ _myServer.PingCounter);
                    if (lastCounterValue == _myServer.PingCounter) {
                        StopSend();
                        StopReceive();
                        StopFailSend();
                        Console.WriteLine("Failed Heartbeats not received the server is down!!!");
                        _myServer.reportFailure();
                       
                    }
                    lastCounterValue = _myServer.PingCounter;
                    
                    //Console.WriteLine("Last counter value = " + lastCounterValue);
                }

                private void SendPing(object source, ElapsedEventArgs e) {
                    if (_myServer._isPrimary) { return; }         
                    try {
                        Console.WriteLine("Trying to Send Ping");
                        _otherServer.receiveHeartBeat("ping");
                    } catch (RemotingException) {

                        Console.WriteLine("Remote Exception");
                        StopSend();
                        StopReceive();
                        _myServer.reportFailure();

                    }
                        
                    
            
                }

                public void StartSend() {
                    _tSend.Start();
                }

                public void StopSend() {

                    _tSend.Stop();
                   
                }
                public void StartReceive() {
                    _tReceive.Start();
                    
                }

                public void StopReceive() {
                    _tReceive.Stop();
                  
                }

                public void StartFailSend() {
                    _tfailSend.Start();
                }
                public void StopFailSend() {
                    _tfailSend.Stop();
                   
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

               // Console.SetWindowSize(49, 18);

                int port = 0;
                TcpChannel channel = null;
                bool isPrimary = false;
                int primaryPort = 0;
                int Id = 0;


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
                        Id = Convert.ToInt32(args[0]);
                        isPrimary = false;
                        primaryPort = Convert.ToInt32(args[1]);
                        port = primaryPort + 1;

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
                        server = new Server(channel, ServerName, url, primaryPort);
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
                        server = new Server(channel,ServerName, url, primaryPort, port, Id);
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
              
               
                server = null;
                
            }
        }
    }
}