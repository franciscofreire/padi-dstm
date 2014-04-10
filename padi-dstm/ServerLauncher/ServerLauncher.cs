using System;
using System.Collections;
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

        public ArrayList processes = new ArrayList();

        public ServerLauncher() {
            InitializeComponent();
        }

        private void LaunchButton_Click(object sender, EventArgs e) {

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = @"..\..\..\DataServer\bin\Debug\DataServer.exe";
            startInfo.Arguments = PortTextBox.Text;
            Process p = Process.Start(startInfo);
            processes.Add(p);
        }

        private void LaunchMaster_Click(object sender, EventArgs e) {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = @"..\..\..\MasterServer\bin\Debug\MasterServer.exe";
            Process p = Process.Start(startInfo);
            processes.Add(p);
        }

        private void SampleAppButton_Click(object sender, EventArgs e) {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = @"..\..\..\SampleApp\bin\Debug\SampleApp.exe";
            Process p = Process.Start(startInfo);
            processes.Add(p);
        }

        private void ClientGUIbutton_Click(object sender, EventArgs e) {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = @"..\..\..\Client\bin\Debug\Client.exe";
            Process p = Process.Start(startInfo);
            processes.Add(p);
        }

        private void KillAll_Click(object sender, EventArgs e) {
            foreach (Process p in processes) {
                p.Kill();
            }
            processes.Clear();
        }
    }
}
