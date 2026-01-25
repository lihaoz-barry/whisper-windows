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
            this.statusPanel = new System.Windows.Forms.Panel();
            this.statusDetailLabel = new System.Windows.Forms.Label();
            this.statusIconLabel = new System.Windows.Forms.Label();
            this.statusTextLabel = new System.Windows.Forms.Label();
            this.statusProgressBar = new System.Windows.Forms.ProgressBar();
            this.statusTimer = new System.Windows.Forms.Timer(this.components);
            this.flowLayoutPanel1.SuspendLayout();
            this.statusPanel.SuspendLayout();
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
            this.textBox1.Location = new System.Drawing.Point(0, 126);
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
            this.flowLayoutPanel1.Location = new System.Drawing.Point(115, 65);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(159, 54);
            this.flowLayoutPanel1.TabIndex = 4;
            //
            // settingsButton
            //
            this.settingsButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.settingsButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.settingsButton.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.settingsButton.Location = new System.Drawing.Point(346, 68);
            this.settingsButton.Name = "settingsButton";
            this.settingsButton.Size = new System.Drawing.Size(30, 30);
            this.settingsButton.TabIndex = 5;
            this.settingsButton.Text = "\u2699";
            this.settingsButton.UseVisualStyleBackColor = true;
            this.settingsButton.Click += new System.EventHandler(this.settingsButton_Click);
            //
            // statusPanel
            //
            this.statusPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.statusPanel.Controls.Add(this.statusIconLabel);
            this.statusPanel.Controls.Add(this.statusTextLabel);
            this.statusPanel.Controls.Add(this.statusDetailLabel);
            this.statusPanel.Controls.Add(this.statusProgressBar);
            this.statusPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.statusPanel.Location = new System.Drawing.Point(0, 0);
            this.statusPanel.Name = "statusPanel";
            this.statusPanel.Size = new System.Drawing.Size(384, 60);
            this.statusPanel.TabIndex = 6;
            //
            // statusIconLabel
            //
            this.statusIconLabel.Font = new System.Drawing.Font("Segoe UI Emoji", 20F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.statusIconLabel.Location = new System.Drawing.Point(12, 8);
            this.statusIconLabel.Name = "statusIconLabel";
            this.statusIconLabel.Size = new System.Drawing.Size(45, 40);
            this.statusIconLabel.TabIndex = 0;
            this.statusIconLabel.Text = "\u23F8";
            this.statusIconLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // statusTextLabel
            //
            this.statusTextLabel.AutoSize = true;
            this.statusTextLabel.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.statusTextLabel.Location = new System.Drawing.Point(60, 8);
            this.statusTextLabel.Name = "statusTextLabel";
            this.statusTextLabel.Size = new System.Drawing.Size(50, 25);
            this.statusTextLabel.TabIndex = 1;
            this.statusTextLabel.Text = "\u5C31\u7EEA";
            //
            // statusDetailLabel
            //
            this.statusDetailLabel.AutoSize = true;
            this.statusDetailLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.statusDetailLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
            this.statusDetailLabel.Location = new System.Drawing.Point(60, 35);
            this.statusDetailLabel.Name = "statusDetailLabel";
            this.statusDetailLabel.Size = new System.Drawing.Size(150, 15);
            this.statusDetailLabel.TabIndex = 2;
            this.statusDetailLabel.Text = "\u6309 Ctrl+M \u5F00\u59CB\u5F55\u5236";
            //
            // statusProgressBar
            //
            this.statusProgressBar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.statusProgressBar.Location = new System.Drawing.Point(280, 25);
            this.statusProgressBar.MarqueeAnimationSpeed = 30;
            this.statusProgressBar.Name = "statusProgressBar";
            this.statusProgressBar.Size = new System.Drawing.Size(90, 10);
            this.statusProgressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.statusProgressBar.TabIndex = 3;
            this.statusProgressBar.Visible = false;
            //
            // statusTimer
            //
            this.statusTimer.Interval = 2000;
            this.statusTimer.Tick += new System.EventHandler(this.statusTimer_Tick);
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
            this.ClientSize = new System.Drawing.Size(384, 220);
            this.Controls.Add(this.statusPanel);
            this.Controls.Add(this.flowLayoutPanel1);
            this.Controls.Add(this.settingsButton);
            this.Controls.Add(this.textBox1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.Text = "Whisper Window";
            this.statusPanel.ResumeLayout(false);
            this.statusPanel.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
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
        private Panel statusPanel;
        private Label statusIconLabel;
        private Label statusTextLabel;
        private Label statusDetailLabel;
        private ProgressBar statusProgressBar;
        private System.Windows.Forms.Timer statusTimer;
    }
}
