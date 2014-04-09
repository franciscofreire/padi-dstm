using System;
using PADI_DSTM;

// Conditions:
// 2 servers, one at port 2001 and another at port 2002

class Tests {
    static void Main(string[] args) {
        bool res;
        PadInt pi_a, pi_b, pi_c, pi_d, pi_e, pi_f, pi_g, pi_h, pi_i, pi_j, pi_k, pi_l, pi_m, pi_n, pi_o;

        PadiDstm.Init();

        /* Library commands */

        // Test1: tries to call all the commands before starting a transaction
        // Expected result: exception or error message - must start a transaction
        pi_a = PadiDstm.CreatePadInt(0);
        pi_a = PadiDstm.AccessPadInt(0);
        pi_a.Read(); 
        pi_a.Write(36);

        // Test2: tries to access a non-created PadInt
        // Expected result: exception or error message - PadInt 1 does not exist
        res = PadiDstm.TxBegin();
        pi_b = PadiDstm.CreatePadInt(0);
        pi_b = PadiDstm.AccessPadInt(1);
        res = PadiDstm.TxCommit();

        // Test3: tries to create an already existant PadInt
        // Expected result: exception or error message - PadInt 0 already exists
        res = PadiDstm.TxBegin();
        pi_c = PadiDstm.CreatePadInt(0);
        res = PadiDstm.TxCommit();
        
        /* Round Robin atribution */

        // Test4: Creates two PadInts when one server is down
        // Expected result: both padInts assigned to the same server (2002)
        res = PadiDstm.TxBegin();
        res = PadiDstm.Fail("tcp://localhost:2001/Server");
        pi_d = PadiDstm.CreatePadInt(1);
        pi_e = PadiDstm.CreatePadInt(2);
        res = PadiDstm.TxCommit();

        // Test5: Recovers the failed server and tries to create another pair of PadInts
        // Expected result: one padInt assigned to each server
        res = PadiDstm.TxBegin();
        res = PadiDstm.Recover("tcp://localhost:2001/Server");
        pi_f = PadiDstm.CreatePadInt(3);
        pi_g = PadiDstm.CreatePadInt(4);
        res = PadiDstm.TxCommit();

        // Test6: Fails both servers and tries to create two padInts
        // Expected result: exception or error message
        res = PadiDstm.TxBegin();
        res = PadiDstm.Fail("tcp://localhost:2001/Server");
        res = PadiDstm.Fail("tcp://localhost:2002/Server");
        pi_h = PadiDstm.CreatePadInt(5);
        pi_i = PadiDstm.CreatePadInt(6);
        res = PadiDstm.TxCommit();

        // Test7: Recovers both servers, freezes one and tries to create two padInts
        // Expected result: both padInts assigned to the same server
        res = PadiDstm.TxBegin();
        res = PadiDstm.Recover("tcp://localhost:2001/Server");
        res = PadiDstm.Recover("tcp://localhost:2002/Server");
        res = PadiDstm.Freeze("tcp://localhost:2001/Server");
        pi_j = PadiDstm.CreatePadInt(7);
        pi_k = PadiDstm.CreatePadInt(8);
        res = PadiDstm.TxCommit();

        /* DataServers status */
        
        // Test8: Tries to access a padInt on the frozen server
        // Fails the 2002 server first to grant assignment to 2001 server
        // Expected result: delay, exception or error message?
        res = PadiDstm.TxBegin();
        res = PadiDstm.Recover("tcp://localhost:2001/Server");
        res = PadiDstm.Fail("tcp://localhost:2002/Server");
        pi_l = PadiDstm.CreatePadInt(9); // assigned to 2001
        res = PadiDstm.Recover("tcp://localhost:2002/Server");
        res = PadiDstm.Freeze("tcp://localhost:2001/Server");
        pi_m = PadiDstm.AccessPadInt(9);
        res = PadiDstm.TxCommit();

        // Test9: Tries to access, read and write a padInt on the failed server
        // Fails the 2001 server first to grant assignment to 2002 server
        // Expected result: exception or error message?
        res = PadiDstm.TxBegin();
        res = PadiDstm.Recover("tcp://localhost:2001/Server");
        res = PadiDstm.Fail("tcp://localhost:2001/Server");
        pi_n = PadiDstm.CreatePadInt(10); // assigned to 2002
        res = PadiDstm.Recover("tcp://localhost:2001/Server");
        res = PadiDstm.Fail("tcp://localhost:2002/Server");
        pi_n = PadiDstm.AccessPadInt(10);
        Console.WriteLine(pi_n.Read());
        pi_n.Write(5);
        Console.WriteLine(pi_n.Read());
        res = PadiDstm.TxCommit();

        // Test10: Issues access, read and write requests for a padInt on a frozen server, that will be recovered later
        // Fails the 2001 server first to grant assignment to 2002 server
        // Expected result: delay
        res = PadiDstm.TxBegin();
        res = PadiDstm.Recover("tcp://localhost:2002/Server");
        res = PadiDstm.Fail("tcp://localhost:2001/Server");
        pi_o = PadiDstm.CreatePadInt(11); // assigned to 2002
        res = PadiDstm.Recover("tcp://localhost:2001/Server");
        res = PadiDstm.Freeze("tcp://localhost:2002/Server");
        pi_o = PadiDstm.AccessPadInt(11);
        Console.WriteLine(pi_o.Read());
        pi_o.Write(5);
        Console.WriteLine(pi_o.Read());
        res = PadiDstm.Recover("tcp://localhost:2002/Server");
        Console.WriteLine(pi_o.Read());
        res = PadiDstm.TxCommit();

    
    }
}
