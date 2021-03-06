﻿using System;
using System.Windows.Forms;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Transactions;

namespace PADI_DSTM {
    
    // Delegates for Form's TextBox manipulation
    delegate void ClearTextDel();
    delegate void UpdateTextDel(String msg);

    public class PadInt {

        private int uid;
        private IPadInt remoteObj;

        public PadInt(int uid, IPadInt remoteObj) {
            this.uid = uid;
            this.remoteObj = remoteObj;
        }

        public IPadInt Remote {
            get {
                return remoteObj;
            }
        }


        public int Read() {
            if (PadiDstm.txId == -1) {
                throw new TxException(uid, "Read operation at PadInt " +
                                          uid + " Failed. No active Transaction");
            }
            return remoteObj.Read(PadiDstm.txId);
        }

        public void Write(int value) {
            if (PadiDstm.txId == -1) {
                throw new TxException(uid, "Write operation at PadInt " +
                                          uid + " Failed. No active Transaction");
            }
            remoteObj.Write(PadiDstm.txId, value);
        }
    }

    public class PadiDstm {
        // Current transaction from this client
        public static int txId;

        public const String urlMaster = "tcp://localhost:9999/MasterServer";
        public static String clientUrl;

        // TCP Channel
        public static TcpChannel channel;

        // MasterServer remote object
        public static IMasterServer masterServer;


        public static bool Init() {
            try {
                txId = -1;
                // Port 0 -> To request that an available port be dynamically assigned
                channel = new TcpChannel(0);
                ChannelServices.RegisterChannel(channel, false);
                try {
                    masterServer = (IMasterServer)Activator.GetObject(typeof(IMasterServer), urlMaster);
                } catch (RemotingException re) {
                    Console.WriteLine("[Init]:\n" + re);
                    return false;
                    //throw new TxException(txId, "TxCommit transaction with id " + txId + "failed. canCommit voting failed.");

                }
                ChannelDataStore data = (ChannelDataStore)channel.ChannelData;
                clientUrl = (String)data.ChannelUris[0];
                //try {
                    masterServer.registerClient(clientUrl);
                //} catch (RemotingException re) {

                //}
                return true;
            } catch (Exception e) {
                Console.WriteLine("Init Exception:" + e);
                return false;
            }
        }

        public static bool TxBegin() {
            if (txId != -1) {
                throw new TxException(txId, "Cannot start new transaction." +
                    "Transaction with id" + txId + "is active");
            }
            try {
                txId = masterServer.TxBegin(clientUrl);
                return true;
            } catch (TxException e) {
                Console.WriteLine("Transaction with id " + e.Tid + " cannot begin.");
                return false;
            }
        }

        public static bool TxCommit() {
            if (txId == -1) {
                throw new TxException(txId, "Cannot commit. No active Transaction");
            }
            try {
                masterServer.TxCommit(txId);
                txId = -1;
                return true;
            } catch (TxException e) {
                Console.WriteLine("Transaction with id " + e.Tid + " cannot be commited.");
                return false;
            } catch (OperationException e) {
                Console.WriteLine(e.Msg);
                txId = -1;
                throw new OperationException(e.Msg);
            }
        }

        public static bool TxAbort() {
            if (txId == -1) {
                throw new TxException(txId, "Cannot abort. No active Transaction");
            }
            try {
                masterServer.TxAbort(txId);
                txId = -1;
                return true;
            } catch (TxException e) {
                Console.WriteLine("Transaction with id " + e.Tid + " cannot be aborted.");
                return false;
            } 
        }

        public static PadInt CreatePadInt(int uid) {
            try {
                IPadInt obj = masterServer.CreatePadInt(uid);
                if (obj == null) {
                    return null;
                } else {
                    PadInt localPadInt = new PadInt(uid, obj);
                    return localPadInt;
                }
            } catch (TxException re) {
                //Console.WriteLine("[CreatePadInt]: Cannot createPadInt with uid " + uid + "\n" + re);
                String text = "[CreatePadInt]: Cannot createPadInt with uid " + uid + "\n" + re;
                Console.WriteLine(text);
                //textBox.Invoke(new ClearTextDel(textBox.Clear));
                //textBox.Invoke(new UpdateTextDel(textBox.AppendText), new object[] { text });
                return null;
            }
        }

        public static PadInt AccessPadInt( int uid) {
            try {
                IPadInt padIntObj;
                PadIntInfo obj = masterServer.AccessPadInt(uid);

                if (obj == null) {
                    return null;
                }

                if (!obj.hasPadInt()) { // Catch remoting exception
                    IDataServer dataServer = (IDataServer)Activator.GetObject(typeof(IDataServer), obj.ServerUrl);
                    padIntObj = dataServer.load(uid);
                } else {
                    padIntObj = obj.PadInt;
                }
                PadInt localPadInt = new PadInt(uid, padIntObj);
                return localPadInt;
            } catch (TxException re) {
                //Console.WriteLine("[AccessPadInt]:  Cannot accessPadInt with uid " + uid + "\n" + re);
                String text = "[AccessPadInt]:  Cannot accessPadInt with uid " + uid + "\n" + re;
                Console.WriteLine(text);
                //textBox.Invoke(new ClearTextDel(textBox.Clear));
                //textBox.Invoke(new UpdateTextDel(textBox.AppendText), new object[] { text });
                
                return null;
            }
        }

        public static bool Status() {
            try {
                masterServer.Status();
                return true;
            } catch (OperationException e) {
                //Console.WriteLine("Status error: " + e);
                String text = "[Status] " + e;
                Console.WriteLine(text);
                //textBox.Invoke(new ClearTextDel(textBox.Clear));
                //textBox.Invoke(new UpdateTextDel(textBox.AppendText), new object[] { text });
                
                return false;
            }
        }

        public static bool Status(TextBox textBox) {
            try {
                String text = masterServer.Status();
                textBox.Invoke(new ClearTextDel(textBox.Clear));
                textBox.Invoke(new UpdateTextDel(textBox.AppendText), new object[] { text });
                return true;
            } catch (OperationException e) {
                //Console.WriteLine("Status error: " + e);
                String text = "[Status] " + e;
                Console.WriteLine(text);
                //textBox.Invoke(new ClearTextDel(textBox.Clear));
                //textBox.Invoke(new UpdateTextDel(textBox.AppendText), new object[] { text });
                return false;
            }
        }

        public static bool Fail( string URL) {
            try {
                IDataServer dataServer = (IDataServer)Activator.GetObject(typeof(IDataServer), URL);
                dataServer.Fail();
                return true;
            } catch (RemotingException e) { //TODO: Improve the catch
                String text = "[Fail] " + e;
                Console.WriteLine(text);
                //textBox.Invoke(new ClearTextDel(textBox.Clear));
                //textBox.Invoke(new UpdateTextDel(textBox.AppendText), new object[] { text });
                return false;
            }
        }

        public static bool Freeze( string URL) {
            try {
                IDataServer dataServer = (IDataServer)Activator.GetObject(typeof(IDataServer), URL);
                dataServer.Freeze();
                return true;
            } catch (RemotingException e) { //TODO: Improve the catch
                String text = "[Freeze] " + e;
                Console.WriteLine(text);
                //textBox.Invoke(new ClearTextDel(textBox.Clear));
                //textBox.Invoke(new UpdateTextDel(textBox.AppendText), new object[] { text });
                return false;
            }
        }

        public static bool Recover( string URL) {
            try {
                IDataServer dataServer = (IDataServer)Activator.GetObject(typeof(IDataServer), URL);
                dataServer.Freeze();
                return true;
            } catch (RemotingException e) { //TODO: Improve the catch
                String text = "[Recover] " + e;
                Console.WriteLine(text);
                //textBox.Invoke(new ClearTextDel(textBox.Clear));
                //textBox.Invoke(new UpdateTextDel(textBox.AppendText), new object[] { text });
                return false;
            }
        }
    }
}
