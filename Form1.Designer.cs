namespace whisper_windows
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
                UnregisterHotKey(this.Handle, MYACTION_HOTKEY_ID);

            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.label1 = new System.Windows.Forms.Label();
            this.fileSystemWatcher1 = new System.IO.FileSystemWatcher();
            this.settingsButton = new System.Windows.Forms.Button();
            this.retryButton = new System.Windows.Forms.Button();
            this.clearCacheButton = new System.Windows.Forms.Button();
            this.retryPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.flowLayoutPanel1.SuspendLayout();
            this.retryPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.fileSystemWatcher1)).BeginInit();
            this.SuspendLayout();
            //
            // timer1
            //
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            //
            // textBox1
            //
            this.textBox1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.textBox1.Location = new System.Drawing.Point(0, 67);
            this.textBox1.Margin = new System.Windows.Forms.Padding(20, 3, 3, 3);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            this.textBox1.Size = new System.Drawing.Size(384, 94);
            this.textBox1.TabIndex = 2;
            this.textBox1.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
            //
            // button1
            //
            this.button1.Location = new System.Drawing.Point(40, 18);
            this.button1.Margin = new System.Windows.Forms.Padding(40, 3, 3, 3);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 3;
            this.button1.Text = "button1";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            //
            // flowLayoutPanel1
            //
            this.flowLayoutPanel1.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.flowLayoutPanel1.Controls.Add(this.label1);
            this.flowLayoutPanel1.Controls.Add(this.button1);
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(115, 7);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(159, 54);
            this.flowLayoutPanel1.TabIndex = 4;
            //
            // settingsButton
            //
            this.settingsButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.settingsButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.settingsButton.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.settingsButton.Location = new System.Drawing.Point(346, 8);
            this.settingsButton.Name = "settingsButton";
            this.settingsButton.Size = new System.Drawing.Size(30, 30);
            this.settingsButton.TabIndex = 5;
            this.settingsButton.Text = "\u2699";
            this.settingsButton.UseVisualStyleBackColor = true;
            this.settingsButton.Click += new System.EventHandler(this.settingsButton_Click);
            //
            // retryButton
            //
            this.retryButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            this.retryButton.FlatAppearance.BorderSize = 0;
            this.retryButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.retryButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.retryButton.ForeColor = System.Drawing.Color.White;
            this.retryButton.Location = new System.Drawing.Point(3, 3);
            this.retryButton.Name = "retryButton";
            this.retryButton.Size = new System.Drawing.Size(75, 28);
            this.retryButton.TabIndex = 6;
            this.retryButton.Text = "重试";
            this.retryButton.UseVisualStyleBackColor = false;
            this.retryButton.Visible = false;
            this.retryButton.Click += new System.EventHandler(this.retryButton_Click);
            //
            // clearCacheButton
            //
            this.clearCacheButton.FlatAppearance.BorderSize = 0;
            this.clearCacheButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.clearCacheButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.clearCacheButton.ForeColor = System.Drawing.Color.Gray;
            this.clearCacheButton.Location = new System.Drawing.Point(84, 3);
            this.clearCacheButton.Name = "clearCacheButton";
            this.clearCacheButton.Size = new System.Drawing.Size(75, 28);
            this.clearCacheButton.TabIndex = 7;
            this.clearCacheButton.Text = "清除缓存";
            this.clearCacheButton.UseVisualStyleBackColor = true;
            this.clearCacheButton.Visible = false;
            this.clearCacheButton.Click += new System.EventHandler(this.clearCacheButton_Click);
            //
            // retryPanel
            //
            this.retryPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.retryPanel.Controls.Add(this.retryButton);
            this.retryPanel.Controls.Add(this.clearCacheButton);
            this.retryPanel.Location = new System.Drawing.Point(8, 163);
            this.retryPanel.Name = "retryPanel";
            this.retryPanel.Size = new System.Drawing.Size(170, 34);
            this.retryPanel.TabIndex = 8;
            //
            // label1
            //
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(60, 0);
            this.label1.Margin = new System.Windows.Forms.Padding(60, 0, 3, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(34, 15);
            this.label1.TabIndex = 4;
            this.label1.Text = "00:00";
            //
            // fileSystemWatcher1
            //
            this.fileSystemWatcher1.EnableRaisingEvents = true;
            this.fileSystemWatcher1.SynchronizingObject = this;
            //
            // Form1
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(384, 200);
            this.Controls.Add(this.retryPanel);
            this.Controls.Add(this.flowLayoutPanel1);
            this.Controls.Add(this.settingsButton);
            this.Controls.Add(this.textBox1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.Text = "Whisper Window";
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.retryPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.fileSystemWatcher1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Timer timer1;
        private TextBox textBox1;
        private Button button1;
        private FlowLayoutPanel flowLayoutPanel1;
        private Label label1;
        private FileSystemWatcher fileSystemWatcher1;
        private Button settingsButton;
        private Button retryButton;
        private Button clearCacheButton;
        private FlowLayoutPanel retryPanel;
    }
}
