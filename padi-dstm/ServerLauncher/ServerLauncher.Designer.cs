namespace WindowsFormsApplication1 {
    partial class ServerLauncher {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ServerLauncher));
            this.ServerUrl = new System.Windows.Forms.TextBox();
            this.PortTextBox = new System.Windows.Forms.TextBox();
            this.UrlLabel = new System.Windows.Forms.Label();
            this.PortLabel = new System.Windows.Forms.Label();
            this.LaunchButton = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // ServerUrl
            // 
            this.ServerUrl.BackColor = System.Drawing.SystemColors.ActiveBorder;
            this.ServerUrl.Location = new System.Drawing.Point(12, 41);
            this.ServerUrl.Name = "ServerUrl";
            this.ServerUrl.ReadOnly = true;
            this.ServerUrl.Size = new System.Drawing.Size(332, 20);
            this.ServerUrl.TabIndex = 0;
            this.ServerUrl.Text = "tcp://localhost: ";
            this.ServerUrl.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
            // 
            // PortTextBox
            // 
            this.PortTextBox.Location = new System.Drawing.Point(365, 41);
            this.PortTextBox.Name = "PortTextBox";
            this.PortTextBox.Size = new System.Drawing.Size(81, 20);
            this.PortTextBox.TabIndex = 1;
            this.PortTextBox.TextChanged += new System.EventHandler(this.PortTextBox_TextChanged);
            // 
            // UrlLabel
            // 
            this.UrlLabel.AutoSize = true;
            this.UrlLabel.Location = new System.Drawing.Point(96, 25);
            this.UrlLabel.Name = "UrlLabel";
            this.UrlLabel.Size = new System.Drawing.Size(94, 13);
            this.UrlLabel.TabIndex = 2;
            this.UrlLabel.Text = "New Server URL :";
            this.UrlLabel.Click += new System.EventHandler(this.label1_Click);
            // 
            // PortLabel
            // 
            this.PortLabel.AutoSize = true;
            this.PortLabel.Location = new System.Drawing.Point(394, 25);
            this.PortLabel.Name = "PortLabel";
            this.PortLabel.Size = new System.Drawing.Size(29, 13);
            this.PortLabel.TabIndex = 3;
            this.PortLabel.Text = "Port:";
            this.PortLabel.Click += new System.EventHandler(this.label2_Click);
            // 
            // LaunchButton
            // 
            this.LaunchButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.LaunchButton.Location = new System.Drawing.Point(472, 39);
            this.LaunchButton.Name = "LaunchButton";
            this.LaunchButton.Size = new System.Drawing.Size(94, 23);
            this.LaunchButton.TabIndex = 4;
            this.LaunchButton.Text = "Launch Server";
            this.LaunchButton.UseVisualStyleBackColor = true;
            this.LaunchButton.Click += new System.EventHandler(this.LaunchButton_Click);
            // 
            // button1
            // 
            this.button1.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.button1.Location = new System.Drawing.Point(472, 81);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(94, 23);
            this.button1.TabIndex = 5;
            this.button1.Text = "Launch Master";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // ServerLauncher
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(578, 116);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.LaunchButton);
            this.Controls.Add(this.PortLabel);
            this.Controls.Add(this.UrlLabel);
            this.Controls.Add(this.PortTextBox);
            this.Controls.Add(this.ServerUrl);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ServerLauncher";
            this.Text = "ServerLauncher";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox ServerUrl;
        private System.Windows.Forms.TextBox PortTextBox;
        private System.Windows.Forms.Label UrlLabel;
        private System.Windows.Forms.Label PortLabel;
        private System.Windows.Forms.Button LaunchButton;
        private System.Windows.Forms.Button button1;
    }
}

