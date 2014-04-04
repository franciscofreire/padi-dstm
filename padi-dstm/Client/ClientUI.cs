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

            private Library _lib;
            private IPadInt _accessedObj; // falta integrar com transacções

            public Form1()
            {
                InitializeComponent();
            }

            private void initButton_Click(object sender, EventArgs e) {
                String url = "tcp://localhost:" + portTextBox.Text + "/" + nameTextBox.Text;
                _lib = new Library(statusTextBox, url, Convert.ToInt32(portTextBox.Text));
                _lib.Init();

                initButton.Enabled = false;
            }

            private void createButton_Click(object sender, EventArgs e) {
                _accessedObj = _lib.CreatePadInt(Convert.ToInt32(createTextBox.Text)); 
            }

            private void accessButton_Click(object sender, EventArgs e) {
                _accessedObj = _lib.AccessPadInt(Convert.ToInt32(accessTextBox.Text));
            }

            private void txBeginButton_Click(object sender, EventArgs e) {
                _lib.TxBegin();
            }

            private void txCommitButton_Click(object sender, EventArgs e) {
                _lib.TxCommit();
            }

            private void txAbortButton_Click(object sender, EventArgs e)
            {
                _lib.TxAbort();
            }

            private void failButton_Click(object sender, EventArgs e)
            {
                _lib.Fail(failTextBox.Text.ToString());
            }

            private void freezeButton_Click(object sender, EventArgs e)
            {
                _lib.Freeze(freezeTextBox.Text.ToString());
            }

            private void recoverButton_Click(object sender, EventArgs e)
            {
                _lib.Recover(recoverTextBox.Text.ToString());
            }

            private void statusButton_Click(object sender, EventArgs e)
            {
                _lib.Status();
            }

            private void readButton_Click(object sender, EventArgs e) {
                // falta integrar com transacções
                int value = _accessedObj.Read();
                readTextBox.Text = value.ToString();
            }

            private void writeButton_Click(object sender, EventArgs e) {
                // falta integrar com transacções
                _accessedObj.Write(Convert.ToInt32(writeTextBox.Text));
            }










           
            
        }
    }
}