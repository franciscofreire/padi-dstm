using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication1 {
    public partial class ServerLauncher : Form {
        public ServerLauncher() {
            InitializeComponent();
        }

        private void label1_Click(object sender, EventArgs e) {

        }

        private void label2_Click(object sender, EventArgs e) {

        }

        private void textBox1_TextChanged(object sender, EventArgs e) {
            
        }

        private void PortTextBox_TextChanged(object sender, EventArgs e) {
           
        }

        private void LaunchButton_Click(object sender, EventArgs e) {
         
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = @"C:\Users\fernando\PADI\padi-dstm\padi-dstm\DataServer\bin\Debug\DataServer.exe";
	startInfo.Arguments =  PortTextBox.Text ;
	Process.Start(startInfo);
            

            
        }

        private void button1_Click(object sender, EventArgs e) {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = @"..\..\..\MasterServer\bin\Debug\MasterServer.exe";
            Process.Start(startInfo);

        }
    }
}
