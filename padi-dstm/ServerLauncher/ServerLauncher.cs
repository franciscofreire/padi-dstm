﻿using System;
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

        private void LaunchButton_Click(object sender, EventArgs e) {

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = @"..\..\..\DataServer\bin\Debug\DataServer.exe";
            startInfo.Arguments = PortTextBox.Text;
            Process.Start(startInfo);
        }

        private void LaunchMaster_Click(object sender, EventArgs e) {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = @"..\..\..\MasterServer\bin\Debug\MasterServer.exe";
            Process.Start(startInfo);
        }
    }
}
