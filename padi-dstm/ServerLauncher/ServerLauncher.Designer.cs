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
            this.LaunchMaster = new System.Windows.Forms.Button();
            this.SampleAppButton = new System.Windows.Forms.Button();
            this.ClientGUIbutton = new System.Windows.Forms.Button();
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
            // 
            // PortTextBox
            // 
            this.PortTextBox.Location = new System.Drawing.Point(365, 41);
            this.PortTextBox.Name = "PortTextBox";
            this.PortTextBox.Size = new System.Drawing.Size(81, 20);
            this.PortTextBox.TabIndex = 1;
            // 
            // UrlLabel
            // 
            this.UrlLabel.AutoSize = true;
            this.UrlLabel.Location = new System.Drawing.Point(96, 25);
            this.UrlLabel.Name = "UrlLabel";
            this.UrlLabel.Size = new System.Drawing.Size(94, 13);
            this.UrlLabel.TabIndex = 2;
            this.UrlLabel.Text = "New Server URL :";
            // 
            // PortLabel
            // 
            this.PortLabel.AutoSize = true;
            this.PortLabel.Location = new System.Drawing.Point(394, 25);
            this.PortLabel.Name = "PortLabel";
            this.PortLabel.Size = new System.Drawing.Size(29, 13);
            this.PortLabel.TabIndex = 3;
            this.PortLabel.Text = "Port:";
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
            // LaunchMaster
            // 
            this.LaunchMaster.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.LaunchMaster.Location = new System.Drawing.Point(12, 81);
            this.LaunchMaster.Name = "LaunchMaster";
            this.LaunchMaster.Size = new System.Drawing.Size(94, 23);
            this.LaunchMaster.TabIndex = 5;
            this.LaunchMaster.Text = "Launch Master";
            this.LaunchMaster.UseVisualStyleBackColor = true;
            this.LaunchMaster.Click += new System.EventHandler(this.LaunchMaster_Click);
            // 
            // SampleAppButton
            // 
            this.SampleAppButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.SampleAppButton.Location = new System.Drawing.Point(132, 81);
            this.SampleAppButton.Name = "SampleAppButton";
            this.SampleAppButton.Size = new System.Drawing.Size(120, 23);
            this.SampleAppButton.TabIndex = 6;
            this.SampleAppButton.Text = "Launch SampleApp";
            this.SampleAppButton.UseVisualStyleBackColor = true;
            this.SampleAppButton.Click += new System.EventHandler(this.SampleAppButton_Click);
            // 
            // ClientGUIbutton
            // 
            this.ClientGUIbutton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.ClientGUIbutton.Location = new System.Drawing.Point(276, 81);
            this.ClientGUIbutton.Name = "ClientGUIbutton";
            this.ClientGUIbutton.Size = new System.Drawing.Size(120, 23);
            this.ClientGUIbutton.TabIndex = 7;
            this.ClientGUIbutton.Text = "Launch Client GUI";
            this.ClientGUIbutton.UseVisualStyleBackColor = true;
            this.ClientGUIbutton.Click += new System.EventHandler(this.ClientGUIbutton_Click);
            // 
            // ServerLauncher
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(578, 116);
            this.Controls.Add(this.ClientGUIbutton);
            this.Controls.Add(this.SampleAppButton);
            this.Controls.Add(this.LaunchMaster);
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
        private System.Windows.Forms.Button LaunchMaster;
        private System.Windows.Forms.Button SampleAppButton;
        private System.Windows.Forms.Button ClientGUIbutton;
    }
}

