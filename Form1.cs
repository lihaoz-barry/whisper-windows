using NAudio.Wave;
using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Media;
using System.Reflection;

namespace whisper_windows
{
    public partial class Form1 : Form
    {

        [DllImport("user32.dll")]
        private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

        [StructLayout(LayoutKind.Sequential)]
        private struct FLASHWINFO
        {
            public uint cbSize;
            public IntPtr hwnd;
            public uint dwFlags;
            public uint uCount;
            public uint dwTimeout;
        }

        // 控制闪烁的常量  
        private const uint FLASHW_ALL = 3;
        private const uint FLASHW_TIMER = 0x00000004;
        private const uint FLASHW_TIMERNOFG = 0x0000000C;

        public static void FlashWindow(Form form)
        {
            FLASHWINFO fw = new FLASHWINFO();

            fw.cbSize = Convert.ToUInt32(Marshal.SizeOf(fw));
            fw.hwnd = form.Handle;
            fw.dwFlags = FLASHW_ALL ;
            fw.uCount = 5;
            fw.dwTimeout = 0;

            FlashWindowEx(ref fw);
        }


        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);
        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int MYACTION_HOTKEY_ID = 1;

        private DateTime startTime;

        private WaveIn waveSource = null;
        private WaveFileWriter waveFile = null;
        private WaveOut waveOut = null;
        private AudioFileReader audioFileReader = null;
        private string outputFileName = "recorded.wav";
        private Boolean isRecording ;
        
        // 系统托盘
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        public Form1()
        {
            InitializeComponent();
            RegisterHotKey(this.Handle, MYACTION_HOTKEY_ID, 0x2, (int)Keys.M);  // 0x2 代表 Ctrl
            isRecording = false;
            // 初始化计时器
            timer1 = new System.Windows.Forms.Timer();
            timer1.Interval = 1000; // 设置时间间隔为1秒
            timer1.Tick += new EventHandler(timer1_Tick);

            // 初始化按
            button1.Text = "Start";
            textBox1.Click += textBox1_Click;

            this.Controls.Add(textBox1);

            
            // 初始化系统托盘
            InitializeTrayIcon();
            
            // 检查 Token 是否配置（首次运行）
            CheckTokenConfiguration();
        }

        private void InitializeTrayIcon()
        {
            // 创建托盘图标
            trayIcon = new NotifyIcon();
            trayIcon.Icon = this.Icon; // 使用窗体图标
            trayIcon.Text = "Whisper Windows";
            trayIcon.Visible = true;
            
            // 双击托盘图标显示/隐藏窗口
            trayIcon.DoubleClick += (s, e) => {
                if (this.Visible)
                {
                    this.Hide();
                }
                else
                {
                    this.Show();
                    this.WindowState = FormWindowState.Normal;
                    this.Activate();
                }
            };
            
            // 创建托盘菜单
            trayMenu = new ContextMenuStrip();
            
            var showItem = new ToolStripMenuItem("显示主窗口");
            showItem.Click += (s, e) => {
                this.Show();
                this.WindowState = FormWindowState.Normal;
                this.Activate();
            };
            
            var settingsItem = new ToolStripMenuItem("设置...");
            settingsItem.Click += (s, e) => OpenSettings();
            
            var exitItem = new ToolStripMenuItem("退出");
            exitItem.Click += (s, e) => {
                trayIcon.Visible = false;
                Application.Exit();
            };
            
            trayMenu.Items.Add(showItem);
            trayMenu.Items.Add(settingsItem);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add(exitItem);
            
            trayIcon.ContextMenuStrip = trayMenu;
        }
        
        private void CheckTokenConfiguration()
        {
            if (!TokenManager.IsTokenConfigured())
            {
                var result = MessageBox.Show(
                    "欢迎使用 Whisper Windows！\n\n" +
                    "请先配置您的 OpenAI API Token 才能使用语音转录功能。\n\n" +
                    "是否现在配置？",
                    "欢迎",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);
                    
                if (result == DialogResult.Yes)
                {
                    OpenSettings();
                }
            }
        }
        
        private void OpenSettings()
        {
            using (var settingsForm = new SettingsForm())
            {
                settingsForm.ShowDialog(this);
            }
        }
        
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // 关闭窗口时最小化到托盘而不是退出
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                
                // 显示提示
                trayIcon.ShowBalloonTip(3000, 
                    "Whisper Windows", 
                    "应用已最小化到系统托盘，双击托盘图标可重新打开", 
                    ToolTipIcon.Info);
            }
            base.OnFormClosing(e);
        }




        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == 0x0312 && m.WParam.ToInt32() == MYACTION_HOTKEY_ID)
            {
                // 热键被触发
                ToggleRecording();
            }
        }
        private void ToggleRecording()
        {
            ToggleRecordingState();
        }

        private void ToggleRecordingState()
        {
            if (!timer1.Enabled)
            {
                StartRecording();
                timer1.Start();
                startTime = DateTime.Now;
                button1.Text = "Timing...";
                isRecording = true;
                PlaySound("whisper_windows.Resources.start.wav");
            }
            else
            {
                timer1.Stop();
                StopRecording();
                button1.Text = "Start Timing";
                isRecording = false;
                PlaySound("whisper_windows.Resources.stop.wav");
            }
        }

        private void PlaySound(string soundFile)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(soundFile))
            {
                if (stream == null)
                {
                    Console.WriteLine("Cannot find resource: " + soundFile);
                    return;
                }
                using (var player = new SoundPlayer(stream))
                {
                    player.Play();
                }
            }
                

        }




        private void button1_Click(object sender, EventArgs e)
        {
            ToggleRecordingState();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            TimeSpan duration = DateTime.Now - startTime;
            label1.Text = FormatTime(duration);  // 更新Label显示时间
        }

        // 格式化时间为 mm:ss
        private string FormatTime(TimeSpan duration)
        {
            return string.Format("{0:D2}:{1:D2}", duration.Minutes, duration.Seconds);
        }
        private void StartRecording()
        {
            // 检查 Token 是否配置
            if (!TokenManager.IsTokenConfigured())
            {
                MessageBox.Show("请先在设置中配置 API Token", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            waveSource = new WaveIn();
            waveSource.WaveFormat = new WaveFormat(44100, 1); // CD quality audio

            waveSource.DataAvailable += new EventHandler<WaveInEventArgs>(waveSource_DataAvailable);
            waveSource.RecordingStopped += new EventHandler<StoppedEventArgs>(waveSource_RecordingStopped);

            EnsureFileIsNotLocked(outputFileName);
            try
            {
                waveFile = new WaveFileWriter(outputFileName, waveSource.WaveFormat);
            }
            catch (IOException ex)
            {
                MessageBox.Show("Failed to start recording: " + ex.Message);
                return;
            }

            waveSource.StartRecording();
        }

        private void EnsureFileIsNotLocked(string filePath)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    using (FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    {
                        stream.Close();
                    }
                }
                catch (IOException)
                {
                    if (waveFile != null)
                    {
                        waveFile.Dispose();
                    }
                    if (audioFileReader != null)
                    {
                        audioFileReader.Dispose();
                    }
                    File.Delete(filePath);
                }
            }
        }
        private void StopRecording()
        {
            if (waveSource != null)
            {
                waveSource.StopRecording();
            }
            if (waveFile != null)
            {
                waveFile.Close();
                waveFile.Dispose();  // 关闭文件并释放资源
                waveFile = null;     // 将引用设置为null，确保资源可以被垃圾回收
            }
            SendAudioToWhisperAPI(outputFileName);
        }

        private void PlayRecording()
        {
            if (audioFileReader != null)
            {
                audioFileReader.Dispose(); // 释放上一次的资源
            }
            if (waveOut != null)
            {
                waveOut.Stop();
                waveOut.Dispose();
            }

            audioFileReader = new AudioFileReader(outputFileName);
            waveOut = new WaveOut();
            waveOut.Init(audioFileReader);
            waveOut.Play();
        }

        private void waveSource_RecordingStopped(object sender, StoppedEventArgs e)
        {
            if (waveSource != null)
            {
                waveSource.Dispose();
                waveSource = null;
            }
            if (waveFile != null)
            {
                waveFile.Dispose();
                waveFile = null;
            }
        }


        private void waveSource_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (waveFile != null)
            {
                waveFile.Write(e.Buffer, 0, e.BytesRecorded);
                waveFile.Flush();
            }
        }
        private async void SendAudioToWhisperAPI(string filePath)
        {
            using (var client = new HttpClient())
            {
                // 获取存储的 Token
                string apiKey = TokenManager.GetDecryptedToken();

                if (string.IsNullOrEmpty(apiKey))
                {
                    MessageBox.Show("API Token 未配置，请在设置中配置", "错误",
                                  MessageBoxButtons.OK,
                                  MessageBoxIcon.Error);
                    return;
                }

                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var content = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(File.ReadAllBytes(filePath));
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");

                content.Add(fileContent, "file", Path.GetFileName(filePath));
                content.Add(new StringContent("whisper-1"), "model");


                try
                {
                    var response = await client.PostAsync("https://api.openai.com/v1/audio/transcriptions", content);
                    var resultText = await response.Content.ReadAsStringAsync();

                    var jsonDocument = System.Text.Json.JsonDocument.Parse(resultText);
                    var text = jsonDocument.RootElement.GetProperty("text").GetString();
                    textBox1.Text = text;  // Display the transcription
                    Clipboard.SetText(text);
                    PlaySound("whisper_windows.Resources.copy.wav");
                    FlashWindow(this);
                    showBalloonTip(text);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to send audio: " + ex.Message);
                }
            }
        }

        private void showBalloonTip(string translatedText)
        {
            NotifyIcon notifyIcon = new NotifyIcon();
            notifyIcon.Icon = SystemIcons.Information; // 设置图标  
            notifyIcon.Visible = true;

            // 显示气泡提示  
            notifyIcon.ShowBalloonTip(3000, "Whisper Window", $"Result：{translatedText}", ToolTipIcon.Info);

            // 一段时间后隐藏 NotifyIcon  
            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Interval = 4000; // 4 秒后隐藏  
            timer.Tick += (s, e) =>
            {
                notifyIcon.Visible = false;
                notifyIcon.Dispose();
                timer.Stop();
                timer.Dispose();
            };
            timer.Start();
        }


        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            //TextBox textBox1 = sender as TextBox;

            //if (textBox != null)
            //{
            // 选择所有文本
            //textBox.SelectAll();

            // 复制到剪贴板
            //Clipboard.SetText(textBox1.Text);

                // 可选：提供用户反馈
                //MessageBox.Show("文本已复制到剪切板！");
            //}
        }


        private void textBox1_Click(object? sender, EventArgs e)
        {
            TextBox textBox1 = sender as TextBox;
            if (textBox1 != null && !string.IsNullOrEmpty(textBox1.Text)) // 检查 textBox1 和 textBox1.Text 是否为 null 或空
            {
                textBox1.SelectAll();
                try
                {
                    Clipboard.SetText(textBox1.Text); // 尝试将文本设置到剪贴板
                    PlaySound("whisper_windows.Resources.copy.wav");
                }
                catch (ArgumentNullException)
                {
                    Console.WriteLine("Failed to set text to clipboard. Text is null."); // 在控制台输出错误信息
                }
            }
            else
            {
                Console.WriteLine("TextBox is null or text is empty."); // 在控制台输出错误信息
            }
        }

        private void settingsButton_Click(object sender, EventArgs e)
        {
            OpenSettings();
        }

    }
}