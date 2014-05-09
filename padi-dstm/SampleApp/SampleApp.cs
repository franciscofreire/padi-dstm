using System;
using PADI_DSTM;

class SampleApp {
    
    static void Main(string[] args) {
        bool res;

        PadiDstm.Init();

       // res = PadiDstm.TxBegin();
        PadInt pi_a = PadiDstm.CreatePadInt(0);
        PadInt pi_b = PadiDstm.CreatePadInt(1);
        //res = PadiDstm.TxCommit();

        
        pi_a = PadiDstm.AccessPadInt(0);
        pi_b = PadiDstm.AccessPadInt(1);
        PadInt pi_c = PadiDstm.AccessPadInt(0);
        PadInt pi_d = PadiDstm.AccessPadInt(1);
        try {
            res = PadiDstm.TxBegin();
            Console.WriteLine("a = " + pi_a.Read());
            Console.WriteLine("b = " + pi_b.Read());
            pi_a.Write(new Random().Next(30));
            pi_b.Write(new Random().Next(30));
            Console.WriteLine("a = " + pi_a.Read());
            Console.WriteLine("b = " + pi_b.Read());
        } catch (TxException te) {
            Console.WriteLine(te.Tid + " " + te.Msg);
            Console.WriteLine("Press any key to Exit");
            Console.ReadKey();
            return;
        }
        Console.WriteLine("Press any key to Commit");
        Console.ReadKey();
        //PadiDstm.Status();
        // The following 3 lines assume we have 2 servers: one at port 2001 and another at port 2002
        //res = PadiDstm.Freeze("tcp://localhost:2001/Server");
        //res = PadiDstm.Recover("tcp://localhost:2001/Server");
        //res = PadiDstm.Fail("tcp://localhost:2002/Server");
        res = PadiDstm.TxCommit();
    }
     
}
