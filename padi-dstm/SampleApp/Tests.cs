using System;
using System.Diagnostics;
using PADI_DSTM;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// Conditions:
// 2 servers, one at port 2001 and another at port 2002

class Tests {

    private Process master;
    private Process server1;
    private Process server2;

    public void launchMaster() {
        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.FileName = @"..\..\..\MasterServer\bin\Debug\MasterServer.exe";
        master = Process.Start(startInfo);
    }

    public void launchTwoServers() {
        String portServer1 = "2001";
        String portServer2 = "2002";
        
        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.FileName = @"..\..\..\DataServer\bin\Debug\DataServer.exe";
        startInfo.Arguments = portServer1;
        server1 = Process.Start(startInfo);

        startInfo = new ProcessStartInfo();
        startInfo.FileName = @"..\..\..\DataServer\bin\Debug\DataServer.exe";
        startInfo.Arguments = portServer2;
        server2 = Process.Start(startInfo);
    }

    /* Library tests */

    public bool TestCommandsWithNoTransaction (){
        // Test1: tries to call all the commands before starting a transaction
        // Expected result: exception or error message - must start a transaction
        launchMaster();
        launchTwoServers();
        try {
            PadInt pi_a = PadiDstm.CreatePadInt(0);
            pi_a = PadiDstm.AccessPadInt(0);
            pi_a.Read();
            /*if (pi_a == null) {
                throw new Exception("Trying to access an object before starting a transaction.");
            }*/
            //pi_a.Read(); 
            //pi_a.Write(36);
            //Assert.Fail("No exception thrown!"); // não deve chegar aqui!
            master.Kill();
            server1.Kill();
            server2.Kill();
            return true;
        } catch (TxException e) {
                Console.WriteLine("TestCommandsWithNoTransaction Exception:" + e);
                master.Kill();
                server1.Kill();
                server2.Kill();
                return false;
        }
    }

    public bool TestAccessNonCreatedPadInt() {
        // Test2: tries to access a non-created PadInt
        // Expected result: exception or error message - PadInt 1 does not exist
        launchMaster();
        launchTwoServers();
        try {
            bool res = PadiDstm.TxBegin();
            PadInt pi_b = PadiDstm.CreatePadInt(0);
            pi_b = PadiDstm.AccessPadInt(1);
            if (pi_b == null)
                throw new Exception("Trying to access a non created PadInt.");
            res = PadiDstm.TxCommit();
            //Assert.Fail("No exception thrown!"); // não deve chegar aqui!
            master.Kill();
            server1.Kill();
            server2.Kill();
            return res;
        } catch (Exception e) {
            Console.WriteLine("TestAccessNonCreatedPadInt error: " + e);
            master.Kill();
            server1.Kill();
            server2.Kill();
            return false;
        }
    }

    public bool TestCreateDuplicatePadInt(){
        // Test3: tries to create an already existant PadInt
        // Expected result: exception or error message - PadInt 0 already exists
        launchMaster();
        launchTwoServers();
        try {
            bool res = PadiDstm.TxBegin();
            PadInt pi_c = PadiDstm.CreatePadInt(0);
            PadInt pi_d = PadiDstm.CreatePadInt(0);
            if (pi_d == null)
                throw new Exception("Trying to create duplicate PadInt!");
            res = PadiDstm.TxCommit();
           // Assert.Fail("No exception thrown!"); // não deve chegar aqui!
            master.Kill();
            server1.Kill();
            server2.Kill();
            return res;
        } catch (Exception e) {
            Console.WriteLine("TestCreateDuplicatePadInt error: " + e);
            master.Kill();
            server1.Kill();
            server2.Kill();
            return false;
        }
    }

    /* Round Robin atribution tests */

    /*
    public bool TestCreateTwoPadIntsOneServerDown() {
        // Test4: Creates two PadInts when one server is down
        // Expected result: both padInts assigned to the same server (2002)
        launchMaster();
        launchTwoServers();
        try {
            bool res = PadiDstm.TxBegin();
            res = PadiDstm.Fail("tcp://localhost:2001/Server");
            PadInt pi_d = PadiDstm.CreatePadInt(1);
            PadInt pi_e = PadiDstm.CreatePadInt(2);
            res = PadiDstm.TxCommit();
                master.Kill();
                server1.Kill();
                server2.Kill();
            return res;
        } catch (TxException e) {
            Console.WriteLine("TestCreateTwoPadIntsOneServerDown error: " + e);
            master.Kill();
            server1.Kill();
            server2.Kill();
            return false;
        }
    }
    */

    /*
    public bool TestCreateTwoPadIntsAfterFailedServerRecover(){
        // Test5: Recovers the failed server and tries to create another pair of PadInts
        // Expected result: one padInt assigned to each server
        launchMaster();
        launchTwoServers();
        try {
            bool res = PadiDstm.TxBegin();
            res = PadiDstm.Fail("tcp://localhost:2001/Server");
            res = PadiDstm.Recover("tcp://localhost:2001/Server"); // vinha do metodo anterior
            PadInt pi_f = PadiDstm.CreatePadInt(3);
            PadInt pi_g = PadiDstm.CreatePadInt(4);
            res = PadiDstm.TxCommit();
            master.Kill();
            server1.Kill();
            server2.Kill();
            return res;
        } catch (TxException e) {
            Console.WriteLine("TestCreateTwoPadIntsAfterFailedServerRecover error: " + e);
            master.Kill();
            server1.Kill();
            server2.Kill();
            return false;
        }
    }
    */

    public bool TestCreateTwoPadIntsBothServersFailed(){
        // Test6: Fails both servers and tries to create two padInts
        // Expected result: exception or error message
        launchMaster();
        launchTwoServers();
        try {
            bool res = PadiDstm.TxBegin();
            res = PadiDstm.Fail("tcp://localhost:2001/Server");
            res = PadiDstm.Fail("tcp://localhost:2002/Server");
            PadInt pi_h = PadiDstm.CreatePadInt(5);
            PadInt pi_i = PadiDstm.CreatePadInt(6);
            if ((pi_i == null) || (pi_h == null)) {
                throw new Exception("No data servers avaiable, can't create PadInts.");
            }
            res = PadiDstm.TxCommit();
            //Assert.Fail("No exception thrown!"); // não deve chegar aqui!
            master.Kill();
            server1.Kill();
            server2.Kill();
            return res;
        } catch (Exception e) {
            Console.WriteLine("TestCreateTwoPadIntsBothServersFailed error: " + e);
            master.Kill();
            server1.Kill();
            server2.Kill();
            return false;
        }
    }

    /*
    public bool TestCreateTwoPadIntsOneServerFrozen(){
        // Test7: Recovers both servers, freezes one and tries to create two padInts
        // Expected result: both padInts assigned to the same server
        launchMaster();
        launchTwoServers();
        try {
            bool res = PadiDstm.TxBegin();
            //res = PadiDstm.Recover("tcp://localhost:2001/Server"); // continuacao do metodo anterior
            //res = PadiDstm.Recover("tcp://localhost:2002/Server"); // continuacao do metodo anterior
            res = PadiDstm.Freeze("tcp://localhost:2001/Server");
            PadInt pi_j = PadiDstm.CreatePadInt(7);
            PadInt pi_k = PadiDstm.CreatePadInt(8);
            res = PadiDstm.TxCommit();
            master.Kill();
            server1.Kill();
            server2.Kill();
            return res;
        } catch (TxException e) {
            Console.WriteLine("TestCreateTwoPadIntsOneServerFrozen error: " + e);
            master.Kill();
            server1.Kill();
            server2.Kill();
            return false;
        }
    }
    */

    /* DataServers status tests */

    /*
    public bool TestAccessPadIntOnFrozenSever(){
        // Test8: Tries to access a padInt on the frozen server
        // Fails the 2002 server first to grant assignment to 2001 server
        // Expected result: delay, exception or error message?
        launchMaster();
        launchTwoServers();
        try {
            bool res = PadiDstm.TxBegin();
            // res = PadiDstm.Recover("tcp://localhost:2001/Server"); // continuacao do metodo anterior
            res = PadiDstm.Fail("tcp://localhost:2002/Server");
            PadInt pi_l = PadiDstm.CreatePadInt(9); // assigned to 2001
            res = PadiDstm.Recover("tcp://localhost:2002/Server");
            res = PadiDstm.Freeze("tcp://localhost:2001/Server");
            PadInt pi_m = PadiDstm.AccessPadInt(9); // fica à espera do Recover
            res = PadiDstm.TxCommit();
            master.Kill();
            server1.Kill();
            server2.Kill();
            return res;
        } catch (TxException e) {
            Console.WriteLine("TestAccessPadIntOnFrozenSever error: " + e);
            master.Kill();
            server1.Kill();
            server2.Kill();
            return false;
        }
    }
    */

    public bool TestAccessPadIntOnFailedServer(){    
        // Test9: Tries to access, read and write a padInt on the failed server
        // Fails the 2001 server first to grant assignment to 2002 server
        // Expected result: exception or error message?
        launchMaster();
        launchTwoServers();
        try {
            bool res = PadiDstm.TxBegin();
            // res = PadiDstm.Recover("tcp://localhost:2001/Server"); // continuacao do metodo anterior
            res = PadiDstm.Fail("tcp://localhost:2001/Server");
            PadInt pi_n = PadiDstm.CreatePadInt(10); // assigned to 2002
            res = PadiDstm.Recover("tcp://localhost:2001/Server");
            res = PadiDstm.Fail("tcp://localhost:2002/Server");
            pi_n = PadiDstm.AccessPadInt(10);
            Console.WriteLine(pi_n.Read());
            pi_n.Write(5);
            Console.WriteLine(pi_n.Read());
            res = PadiDstm.TxCommit();
            //Assert.Fail("No exception thrown!"); // não deve chegar aqui!
            master.Kill();
            server1.Kill();
            server2.Kill();
            return res;
        } catch (TxException e) {
            Console.WriteLine("TestAccessPadIntOnFailedServer error: " + e);
            master.Kill();
            server1.Kill();
            server2.Kill();
            return false;
        }
    }

    /*
    public bool TestReadPadIntOnFrozenServer(){
        // Test10: Issues access, read and write requests for a padInt on a frozen server, that will be recovered later
        // Fails the 2001 server first to grant assignment to 2002 server
        // Expected result: delay
        launchMaster();
        launchTwoServers();
        try {
            bool res = PadiDstm.TxBegin();
            // res = PadiDstm.Recover("tcp://localhost:2002/Server"); // continuacao do metodo anterior
            res = PadiDstm.Fail("tcp://localhost:2001/Server");
            PadInt pi_o = PadiDstm.CreatePadInt(11); // assigned to 2002
            res = PadiDstm.Recover("tcp://localhost:2001/Server");
            res = PadiDstm.Freeze("tcp://localhost:2002/Server");
            pi_o = PadiDstm.AccessPadInt(11);
            Console.WriteLine(pi_o.Read());
            pi_o.Write(5);
            Console.WriteLine(pi_o.Read());
            res = PadiDstm.Recover("tcp://localhost:2002/Server");
            Console.WriteLine(pi_o.Read());
            res = PadiDstm.TxCommit();
            master.Kill();
            server1.Kill();
            server2.Kill();
            return res;
        } catch (TxException e) {
            Console.WriteLine("TestReadPadIntOnFrozenServer error: " + e);
            master.Kill();
            server1.Kill();
            server2.Kill();
            return false;
        }
    }
    */

    /* Transactions tests */

    public bool TestReadPadIntAfterWritingTransactionAborted() {
        // Test11: Locally writes on a PadInt, and aborts this transaction. Another transaction will read the PadInt.
        // Expected result: The PadInt must have the initial values, no modifications.
        launchMaster();
        launchTwoServers();
        try {
            bool res = PadiDstm.TxBegin();
            PadInt pi_p = PadiDstm.CreatePadInt(12);
            pi_p = PadiDstm.AccessPadInt(12);
            Console.WriteLine(pi_p.Read());
            Assert.AreEqual(pi_p.Read(), 0); // tem que estar a zero, valor inicial
            res = PadiDstm.TxCommit();

            res = PadiDstm.TxBegin();
            pi_p.Write(100);
            Console.WriteLine(pi_p.Read());
            Assert.AreEqual(pi_p.Read(), 100); // tem que estar a 100, modificação local
            res = PadiDstm.TxAbort(); // falha a transacção

            res = PadiDstm.TxBegin();
            Assert.AreEqual(pi_p.Read(), 0); // tem que estar a zero, valor inicial
            res = PadiDstm.TxCommit();
            
            master.Kill();
            server1.Kill();
            server2.Kill();
            return res;
    } catch (Exception e) {
            Console.WriteLine("TestReadPadIntAfterWritingTransactionAborted error: " + e);
            master.Kill();
            server1.Kill();
            server2.Kill();
            return false;
        }
    }

    public bool TestReadPadIntAfterWritingTransactionCommited() {
        // Test11: Locally writes on a PadInt, and aborts this transaction. Another transaction will read the PadInt.
        // Expected result: The PadInt must have the initial values, no modifications.
        launchMaster();
        launchTwoServers();
        try {
            bool res = PadiDstm.TxBegin();
            PadInt pi_p = PadiDstm.CreatePadInt(12);
            pi_p = PadiDstm.AccessPadInt(12);
            Console.WriteLine(pi_p.Read());
            Assert.AreEqual(pi_p.Read(), 0); // tem que estar a zero, valor inicial
            res = PadiDstm.TxCommit();

            res = PadiDstm.TxBegin();
            pi_p.Write(100);
            Console.WriteLine(pi_p.Read());
            Assert.AreEqual(pi_p.Read(), 100); // tem que estar a 100, modificação local
            res = PadiDstm.TxCommit(); // conclui a transacção

            res = PadiDstm.TxBegin();
            Assert.AreEqual(pi_p.Read(), 100); // tem que estar a 100, valor actualizado
            res = PadiDstm.TxCommit();
            
            master.Kill();
            server1.Kill();
            server2.Kill();
            return res;
        } catch (Exception e) {
            Console.WriteLine("TestReadPadIntAfterWritingTransactionCommited error: " + e);
            master.Kill();
            server1.Kill();
            server2.Kill();
            return false;
        }
    }

    static void Main(string[] args) {
        PadiDstm.Init();
        
        Tests tests = new Tests();
        
        Assert.AreEqual(tests.TestCommandsWithNoTransaction(), false); // falha
        Assert.AreEqual(tests.TestAccessNonCreatedPadInt(), false); // falha
        Assert.AreEqual(tests.TestCreateDuplicatePadInt(), false); // falha
        //Assert.AreEqual(tests.TestCreateTwoPadIntsOneServerDown(), true);
        //Assert.AreEqual(tests.TestCreateTwoPadIntsAfterFailedServerRecover(), true);
        Assert.AreEqual(tests.TestCreateTwoPadIntsBothServersFailed(), false); // falha
        //Assert.AreEqual(tests.TestCreateTwoPadIntsOneServerFrozen(), false);
        //Assert.AreEqual(tests.TestAccessPadIntOnFrozenSever(), true); // fica em espera (devolve true?)
        Assert.AreEqual(tests.TestAccessPadIntOnFailedServer(), false); // falha
        //Assert.AreEqual(tests.TestReadPadIntOnFrozenServer(), true); // fica em espera (devolve true?)
        
        Assert.AreEqual(tests.TestReadPadIntAfterWritingTransactionAborted(), true);
        //Assert.AreEqual(tests.TestReadPadIntAfterWritingTransactionCommited(), true);

    }
}
