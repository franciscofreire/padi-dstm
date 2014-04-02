using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PADI_DSTM
{
    namespace Client
    {
        public partial class Form1 : Form
        {
            public Form1()
            {
                InitializeComponent();
            }

            private void createButton_Click(object sender, EventArgs e) {
                String urlMaster = "tcp://localhost:9999/MasterServer";
                IMasterServer masterServer = (IMasterServer)Activator.GetObject(typeof(IMasterServer), urlMaster);
               
                IPadInt obj = masterServer.CreatePadInt(Convert.ToInt32(createTextBox.Text));

            }

            private void accessButton_Click(object sender, EventArgs e) {
                String urlMaster = "tcp://localhost:9999/MasterServer";
                IMasterServer masterServer = (IMasterServer)Activator.GetObject(typeof(IMasterServer), urlMaster);
                
                PadIntInfo obj = masterServer.AccessPadInt("client1", Convert.ToInt32(accessTextBox.Text));

            }




           
            
        }
    }
}