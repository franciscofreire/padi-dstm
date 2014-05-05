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

namespace PADI_DSTM {

    namespace DataServer {

        public enum LockType { WRITE, READ };

        public class PadInt : MarshalByRefObject, IPadInt {

            private int id;
            private int value;
            private Server myServer;

            public int Value {
                get { return value; }
                set { this.value = value; }
            }

            public int Id {
                get { return id; }
            }

            public PadInt(int id, Server srv) {
                this.myServer = srv;
                this.id = id;
                this.value = 0;
            }

            public void Write(int txId, int value) {
                lock (myServer.StateLockObj) {
                    if (myServer.isFail) {
                        Console.WriteLine("[!WRITE] Error: DataServer " + myServer.name + " is set to [Fail] mode!");
                        Console.WriteLine("---");
                        while (true) ;
                    } else if (myServer.isFreeze) {
                        lock (SingletonCounter.Instance) {
                            SingletonCounter.Instance.incrementLockCounter();
                            Monitor.Wait(SingletonCounter.Instance);
                        }
                        Console.WriteLine("[!WRITE] Error: DataServer " + myServer.name + " is set to [Freeze] mode!");
                        Console.WriteLine("---");
                    }
                    ServerTransaction transaction = null;

                    if (!myServer.Transactions.ContainsKey(txId)) {
                        transaction = new ServerTransaction(txId, this);
                        myServer.Transactions.Add(txId, transaction);
                        myServer.MasterServer.join(txId, myServer.URL);
                    }
                    transaction = myServer.Transactions[txId];
                    Console.WriteLine("[Write] Tx{0} is Trying to acquire lock for PadInt {1}",
                    txId, id);

                    Lock myLock = myServer.lockManager.getLock(LockType.WRITE, this.id, txId);
                    if (myLock == null) {
                        //abort distributed transaction
                        throw new TxException(txId, "Transaction abort on write due to Deadlock");
                    }
                    Console.WriteLine("[Write] Tx{0} Acquired lock for PadInt {1}",
                        txId, id);
                            
                    transaction.pushLock(myLock);
                    // if not yet saved, save it for future rollback
                    if (!transaction.containsPadInt(this)) {
                        transaction.Add(this);
                    }
                    this.Value = value;
                    Console.WriteLine("[Write] Transaction " + txId + " created in " + myServer.name);
                }
            }

            public int Read(int txId) {
                lock (myServer.StateLockObj) {
                    if (myServer.isFail) {
                        Console.WriteLine("[!READ] Error: DataServer " + myServer.name + " is set to [Fail] mode!");
                        Console.WriteLine("---");
                        while (true) ;

                    } else if (myServer.isFreeze) {
                        lock (SingletonCounter.Instance) {
                            SingletonCounter.Instance.incrementLockCounter();
                            Monitor.Wait(SingletonCounter.Instance);
                        }
                        Console.WriteLine("[!READ] Error: DataServer " + myServer.name + " is set to [Freeze] mode!");
                        Console.WriteLine("---");
                    }

                    ServerTransaction transaction = null;

                    if (!myServer.Transactions.ContainsKey(txId)) {
                        transaction = new ServerTransaction(txId, this);
                        myServer.Transactions.Add(txId, transaction);
                        myServer.MasterServer.join(txId, myServer.URL);
                        Console.WriteLine("[Read] Transaction " + txId + " created in " + myServer.name);
                    } else {
                        transaction = myServer.Transactions[txId];
                    }

                    Console.WriteLine("[Read] Tx{0} is Trying to acquire lock for PadInt {1}",
                    txId, id);
                    Lock myLock = myServer.lockManager.getLock(LockType.READ, id, txId);
                    if(myLock == null) {
                        //abort distributed transaction
                        throw new TxException(txId, "Transaction abort on read due to Deadlock");
                    }

                    Console.WriteLine("[Read] Tx{0} Acquired lock for PadInt {1}",
                        txId, id);
                    transaction.pushLock(myLock);
                    return this.value;
                }
            }
            public override object InitializeLifetimeService() {
                return null;
            }
        }

        public class ServerTransaction {

            private Dictionary<PadInt, int> copies;
            private int txId;
            private bool abort;
            private Stack<Lock> locksStack;

            public ServerTransaction(int txId, PadInt Obj) {
                this.txId = txId;
                copies = new Dictionary<PadInt, int>();
                copies.Add(Obj, Obj.Value);
                abort = false;
                locksStack = new Stack<Lock>();
            }

            public bool Abort {
                get { return abort; }
                set { abort = value; }
            }

            public void Add(PadInt Obj) {
                copies.Add(Obj, Obj.Value);
            }

            public void Set(PadInt Obj, int value) {
                copies[Obj] = value;
            }

            public int getCopiesSize() {
                return copies.Count;
            }

            public int getValue(PadInt padInt) {
                return copies[padInt];
            }

            public Dictionary<PadInt, int>.KeyCollection getCopiesKeys() {
                return copies.Keys;
            }

            public bool containsPadInt(PadInt padInt) {
                return copies.ContainsKey(padInt);
            }

            public void doChanges() {
                /*foreach (KeyValuePair<PadInt, int> entry in copies) {
                    entry.Key.Value = entry.Value;
                }*/
            }
            public void rollback() {
                foreach (KeyValuePair<PadInt, int> entry in copies) {
                    entry.Key.Value = entry.Value;
                }
            }
            public void pushLock(Lock l) {
                locksStack.Push(l);
            }
            // Note: returns null when the stack is empty
            public Lock popLock() {
                try {
                    return locksStack.Pop();
                } catch (InvalidOperationException ioe) {
                    Console.WriteLine("Stack of Tx{0} is empty! {1}", txId, ioe);
                    return null;
                }
            }
        }

        public class Lock {
            private LockType type;
            private int padIntId;
            private int transactionId;
            private int id;

            public int Id {
                get { return id; }
                set { id = value; }
            }

            public int PadIntId {
                get { return padIntId; }
                set { padIntId = value; }
            }

            public LockType Type {
                get { return type; }
                set { type = value; }
            }

            public int TransactionId {
                get { return transactionId; }
                set { transactionId = value; }
            }

            public Lock(int id, LockType type, int padIntId, int transactionId) {
                this.id = id;
                this.type = type;
                this.padIntId = padIntId;
                this.transactionId = transactionId;
            }

            public Lock(int transactionId) {
                this.transactionId = transactionId;
            }

            public override bool Equals(Object obj) {
                if (obj == null || GetType() != obj.GetType())
                    return false;
                Lock l = (Lock)obj;
                return l.TransactionId == this.TransactionId;
            }

            public override int GetHashCode() {
                return transactionId;
            }
        }

        public class LockManager {
            private Dictionary<int, List<Lock>> locksGranted;
            private int idGenerator;

            public LockManager() {
                locksGranted = new Dictionary<int, List<Lock>>();
                idGenerator = 0;
            }

            // Possible Approachs (timeout): throw an exception
            public Lock getLock(LockType type, int padIntId, int transactionId) {
                lock (this) {
                    if (!locksGranted.ContainsKey(padIntId)) {
                        locksGranted[padIntId] = new List<Lock>();
                    }
                    List<Lock> padIntLocks = locksGranted[padIntId];
                    Lock l = new Lock(idGenerator++, type, padIntId, transactionId);

                    while (!canHaveLock(l, padIntLocks)) {
                        Monitor.Wait(this);
                    }
                    // Nice! Now we have the lock
                    padIntLocks.Add(l);
                    return l;
                }
            }

            private bool canHaveLock(Lock l, List<Lock> grantedList) {
                foreach (Lock el in grantedList) {
                    if (el.Type == LockType.WRITE) {
                        if (l.Equals(el)) {
                            return true;
                        } else
                            return false;
                    }
                }
                // we only have read locks, it's ok
                return true;
            }

            public void releaseLock(Lock l) {
                lock (this) {
                    List<Lock> padIntLocks = locksGranted[l.PadIntId];
                    padIntLocks.Remove(l);
                    if (l.Type == LockType.WRITE) {
                        //let's release the next thread
                        Monitor.Pulse(this);
                    }
                }
            }
            
            public void releaseLock(int txId, Server s) {
                lock (this) {
                    ServerTransaction tr = s.Transactions[txId];
                    foreach (PadInt p in tr.getCopiesKeys()) {
                        List<Lock> padIntLocks = locksGranted[p.Id];
                        if (padIntLocks != null) {
                            int index = padIntLocks.IndexOf(new Lock(txId));
                            if (index != -1) {
                                Lock l = padIntLocks[index];
                                Console.WriteLine("lock in " + l.PadIntId + "by " + l.TransactionId + " is being removed");
                                padIntLocks.Remove(new Lock(txId));
                                if (l.Type == LockType.WRITE) {
                                    //let's release next thread
                                    Monitor.Pulse(this);
                                }
                            }
                        }
                    }
                }
            }
        }


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
            public String _primaryName;
            private LockManager _lockManager;

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

            public Server(String name, String url, int primaryPort) {
                _name = name;
                _url = url;
                _masterServer = (IMasterServer)Activator.GetObject(typeof(IMasterServer), urlMaster);
                _state = State.Normal;
                _stateLockObj = new Object();
                _isPrimary = true;
                _slavePort = 0;
                _lockManager = new LockManager();
            }

            public Server(String name, String url, int primaryPort, int slavePort) {
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
            }

            public void register() {
                _masterServer.registerServer(_url);
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
                Console.WriteLine("[canCommit] Master Request with id " + TxId);
                Console.WriteLine("---");

                if (!this.Transactions.ContainsKey(TxId)) {
                    Console.WriteLine("Error transaction not found");
                    return false;
                }

                return true;
            }

            public bool doCommit(int TxId) {
                //lock (_stateLockObj) {
                if (isFail) {
                    Console.WriteLine("[!doCommit] Error: DataServer " + name + " is set to [Fail] mode!");
                    Console.WriteLine("---");
                    while (true) ;
                    //throw new RemotingException("Server is in Fail Mode");

                } else if (isFreeze) {
                    lock (SingletonCounter.Instance) {
                        SingletonCounter.Instance.incrementLockCounter();
                        Monitor.Wait(SingletonCounter.Instance);
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

            public bool doAbort(int TxId) {
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
                Console.WriteLine("[haveCommitted] Master Request with id " + TxId);
                Console.WriteLine("---");
                return true;
            }

            public override object InitializeLifetimeService() {
                return null;
            }

            public void makeConnection(int primaryPort) {
                String url = "tcp://localhost:" + primaryPort + "/" + "Server";
                _primaryServer = (IDataServer)Activator.GetObject(typeof(IDataServer), url);
                _primaryServer.connect(_slavePort);
            }

            public void connect(int slavePort) {
                //TODO Create remote reference to slave
                _slavePort = slavePort;

                Console.WriteLine("Backup ServerAt{0} registered", slavePort);
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
                    server = new Server(ServerName, url, primaryPort);
                    RemotingServices.Marshal(server, name, typeof(IDataServer));
                    server.register();
                    Console.WriteLine("Started " + ServerName + " (Primary)...");
                } else {
                    server = new Server(ServerName, url, primaryPort, port);
                    RemotingServices.Marshal(server, name, typeof(IDataServer));
                    server.makeConnection(primaryPort);
                    Console.WriteLine("Started " + ServerName + " (Backup of " + server._primaryName + ")...");
                }

                Console.WriteLine("---");
                Console.ReadKey();
            }
        }
    }
}