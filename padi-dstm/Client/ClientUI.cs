using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Collections;


namespace PADI_DSTM {
    namespace Client {
        public partial class Form1 : Form {

            //private PadiDstm _lib;

            //associates uid's with PadInt objects
            private Hashtable myObjects = new Hashtable();
           
            // object created
            private PadInt _createdObj;

            public Form1() {
                InitializeComponent();
            }

            private void initButton_Click(object sender, EventArgs e) {
                //String url = "tcp://localhost:" + portTextBox.Text + "/" + nameTextBox.Text;
                //_lib.Init();
                PadiDstm.Init();
                initButton.Enabled = false;
            }

            private void createButton_Click(object sender, EventArgs e) {
                _createdObj = PadiDstm.CreatePadInt(Convert.ToInt32(createTextBox.Text));
                //_accessedObj = _lib.CreatePadInt(Convert.ToInt32(createTextBox.Text)); 
            
            }

            private void accessButton_Click(object sender, EventArgs e) {
                _createdObj = PadiDstm.AccessPadInt(Convert.ToInt32(accessTextBox.Text));
                myObjects.Add(Convert.ToInt32(accessTextBox.Text), _createdObj);
                listBox.Items.Add("Id:" + Convert.ToInt32(accessTextBox.Text));
            }

            private void txBeginButton_Click(object sender, EventArgs e) {
                PadiDstm.TxBegin();
                txBeginButton.Enabled = false;
            }

            private void txCommitButton_Click(object sender, EventArgs e) {
                PadiDstm.TxCommit();
                txBeginButton.Enabled = true;
            }

            private void txAbortButton_Click(object sender, EventArgs e) {
                PadiDstm.TxAbort();
                txBeginButton.Enabled = true;
            }

            private void failButton_Click(object sender, EventArgs e) {
                PadiDstm.Fail(failTextBox.Text.ToString());
            }

            private void freezeButton_Click(object sender, EventArgs e) {
                PadiDstm.Freeze(freezeTextBox.Text.ToString());
            }

            private void recoverButton_Click(object sender, EventArgs e) {
                PadiDstm.Recover(recoverTextBox.Text.ToString());
            }

            private void statusButton_Click(object sender, EventArgs e) {
                // sabemos que não faz nada
                PadiDstm.Status(statusTextBox);
            }

            private void readButton_Click(object sender, EventArgs e) {
                // falta integrar com transacções
                String selectedItem = listBox.SelectedItem.ToString();
                string[] parser = selectedItem.Split(':');
                int uid = Convert.ToInt32(parser[1]);

                PadInt obj = (PadInt) myObjects[uid];
                
                int value = obj.Read();
                
                //    int value = _accessedObj.Read(listBox.SelectedItem.ToString());
                readTextBox.Text = value.ToString();
                listBox.ClearSelected();
            }

            private void writeButton_Click(object sender, EventArgs e) {
                // falta integrar com transacções
                //_accessedObj.Write(Convert.ToInt32(writeTextBox.Text));
                String selectedItem = listBox.SelectedItem.ToString();
                string[] parser = selectedItem.Split(':');
                int uid = Convert.ToInt32(parser[1]);

                PadInt obj = (PadInt)myObjects[uid];
                obj.Write(Convert.ToInt32(writeTextBox.Text));

                listBox.ClearSelected();
            }

        }
    }
}