using NAudio.Wave;
using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Media;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http.Headers;
using System.Drawing;

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

        // Flash window constants
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

        // System tray
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;

        // Status management
        private StatusManager statusManager;

        public Form1()
        {
            InitializeComponent();
            RegisterHotKey(this.Handle, MYACTION_HOTKEY_ID, 0x2, (int)Keys.M);  // 0x2 = Ctrl
            isRecording = false;
            // Initialize timer
            timer1 = new System.Windows.Forms.Timer();
            timer1.Interval = 1000;
            timer1.Tick += new EventHandler(timer1_Tick);

            // Initialize button
            button1.Text = "Start";
            textBox1.Click += textBox1_Click;

            this.Controls.Add(textBox1);

            // Initialize status manager
            InitializeStatusManager();

            // Initialize system tray
            InitializeTrayIcon();

            // Check token configuration (first run)
            CheckTokenConfiguration();
        }

        private void InitializeStatusManager()
        {
            statusManager = new StatusManager();
            statusManager.StatusChanged += OnStatusChanged;

            // Set initial status display
            UpdateStatusDisplay(StatusManager.GetStatusInfo(AppStatus.Idle));
            statusDetailLabel.Text = "按 Ctrl+M 开始录制";
        }

        private void OnStatusChanged(object sender, StatusChangedEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => OnStatusChanged(sender, e)));
                return;
            }

            var info = StatusManager.GetStatusInfo(e.NewStatus);
            info.DetailMessage = e.DetailMessage;
            UpdateStatusDisplay(info);

            // Show/hide progress bar based on status
            bool showProgress = e.NewStatus == AppStatus.Processing ||
                               e.NewStatus == AppStatus.Sending ||
                               e.NewStatus == AppStatus.Transcribing;
            statusProgressBar.Visible = showProgress;

            // Auto-reset to idle after success
            if (e.NewStatus == AppStatus.Success)
            {
                statusTimer.Start();
            }
        }

        private void UpdateStatusDisplay(StatusInfo info)
        {
            statusIconLabel.Text = info.Icon;
            statusTextLabel.Text = info.Text;
            statusTextLabel.ForeColor = info.Color;
            statusDetailLabel.Text = info.DetailMessage;

            // Update panel border color based on status
            statusPanel.BackColor = Color.FromArgb(245, 245, 245);
        }

        private void statusTimer_Tick(object sender, EventArgs e)
        {
            statusTimer.Stop();
            statusManager.SetIdle();
            statusDetailLabel.Text = "按 Ctrl+M 开始录制";
        }

        private void InitializeTrayIcon()
        {
            // Create tray icon
            trayIcon = new NotifyIcon();
            trayIcon.Icon = this.Icon;
            trayIcon.Text = "Whisper Windows";
            trayIcon.Visible = true;

            // Double-click tray icon to show/hide window
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

            // Create tray menu
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
            // Minimize to tray instead of closing
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();

                // Show notification
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
                // Hotkey triggered
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
                button1.Text = "Stop";
                isRecording = true;
                statusManager.SetRecording();
                PlaySound("whisper_windows.Resources.start.wav");
            }
            else
            {
                timer1.Stop();
                statusManager.SetProcessing("正在停止录制...");
                StopRecording();
                button1.Text = "Start";
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
            label1.Text = FormatTime(duration);

            // Update status detail with recording duration
            if (isRecording)
            {
                statusDetailLabel.Text = $"录制中 {FormatTime(duration)} - 按 Ctrl+M 停止";
            }
        }

        // Format time as mm:ss
        private string FormatTime(TimeSpan duration)
        {
            return string.Format("{0:D2}:{1:D2}", duration.Minutes, duration.Seconds);
        }
        private void StartRecording()
        {
            // Check token configuration
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
                waveFile.Dispose();
                waveFile = null;
            }
            SendAudioToWhisperAPI(outputFileName);
        }

        private void PlayRecording()
        {
            if (audioFileReader != null)
            {
                audioFileReader.Dispose();
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
            try
            {
                statusManager.SetProcessing("正在检查音频文件...");

                // Check if audio needs segmentation
                if (AudioSegmenter.NeedsSegmentation(filePath))
                {
                    await HandleSegmentedAudio(filePath);
                }
                else
                {
                    // Get file size for status display
                    var fileInfo = new FileInfo(filePath);
                    statusManager.SetSending(fileInfo.Length);

                    await SendSingleAudioSegment(filePath, null);
                }
            }
            catch (Exception ex)
            {
                statusManager.SetError($"处理失败: {ex.Message}");
                MessageBox.Show($"Failed to process audio: {ex.Message}", "错误",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Error);
            }
        }

        private async Task HandleSegmentedAudio(string filePath)
        {
            List<AudioSegment> segments = null;
            try
            {
                // Segment the audio
                statusManager.SetProcessing("正在分段音频...");
                textBox1.Text = "正在分段音频...";
                segments = AudioSegmenter.SegmentAudio(filePath);

                if (segments.Count == 0)
                {
                    var fileInfo = new FileInfo(filePath);
                    statusManager.SetSending(fileInfo.Length);
                    await SendSingleAudioSegment(filePath, null);
                    return;
                }

                List<string> allTranscriptions = new List<string>();

                // Process each segment
                for (int i = 0; i < segments.Count; i++)
                {
                    var segment = segments[i];
                    statusManager.SetTranscribingSegment(segment.SegmentNumber, segment.TotalSegments);
                    textBox1.Text = $"正在转录第 {segment.SegmentNumber}/{segment.TotalSegments} 段...";
                    this.Refresh();

                    string transcription = await SendSingleAudioSegment(segment.FilePath, segment);
                    if (!string.IsNullOrEmpty(transcription))
                    {
                        allTranscriptions.Add(transcription);
                    }
                }

                // Merge results
                string finalText = string.Join(" ", allTranscriptions);
                textBox1.Text = finalText;
                Clipboard.SetText(finalText);
                statusManager.SetSuccess("转录完成，已复制到剪贴板");
                PlaySound("whisper_windows.Resources.copy.wav");
                FlashWindow(this);
                showBalloonTip(finalText);
            }
            catch (Exception ex)
            {
                statusManager.SetError($"分段处理失败: {ex.Message}");
                MessageBox.Show($"Failed to process segmented audio: {ex.Message}", "错误",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Error);
            }
            finally
            {
                // Cleanup segment files
                if (segments != null)
                {
                    AudioSegmenter.CleanupSegments(segments);
                }
            }
        }

        private async Task<string> SendSingleAudioSegment(string filePath, AudioSegment segment = null)
        {
            using (var client = new HttpClient())
            {
                // Get stored token
                string apiKey = TokenManager.GetDecryptedToken();

                if (string.IsNullOrEmpty(apiKey))
                {
                    statusManager.SetError("API Token 未配置");
                    MessageBox.Show("API Token 未配置，请在设置中配置", "错误",
                                  MessageBoxButtons.OK,
                                  MessageBoxIcon.Error);
                    return null;
                }

                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var content = new MultipartFormDataContent();
                var fileBytes = File.ReadAllBytes(filePath);
                var fileContent = new ByteArrayContent(fileBytes);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");

                content.Add(fileContent, "file", Path.GetFileName(filePath));
                content.Add(new StringContent("whisper-1"), "model");

                try
                {
                    // Update status to sending if single segment
                    if (segment == null)
                    {
                        statusManager.SetSending(fileBytes.Length);
                    }

                    // Update status to transcribing
                    statusManager.SetTranscribing();

                    var response = await client.PostAsync("https://api.openai.com/v1/audio/transcriptions", content);
                    var resultText = await response.Content.ReadAsStringAsync();

                    // Check for API error response
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorMsg = $"API 返回错误: {response.StatusCode}";
                        try
                        {
                            var errorDoc = System.Text.Json.JsonDocument.Parse(resultText);
                            if (errorDoc.RootElement.TryGetProperty("error", out var errorElement) &&
                                errorElement.TryGetProperty("message", out var msgElement))
                            {
                                errorMsg = msgElement.GetString();
                            }
                        }
                        catch { }

                        statusManager.SetError(errorMsg);
                        throw new Exception(errorMsg);
                    }

                    var jsonDocument = System.Text.Json.JsonDocument.Parse(resultText);
                    var text = jsonDocument.RootElement.GetProperty("text").GetString();

                    // For single segment, display immediately
                    if (segment == null)
                    {
                        textBox1.Text = text;
                        Clipboard.SetText(text);
                        statusManager.SetSuccess("转录完成，已复制到剪贴板");
                        PlaySound("whisper_windows.Resources.copy.wav");
                        FlashWindow(this);
                        showBalloonTip(text);
                    }

                    return text;
                }
                catch (Exception ex)
                {
                    if (segment == null)
                    {
                        statusManager.SetError($"转录失败: {ex.Message}");
                    }
                    MessageBox.Show($"Failed to transcribe: {ex.Message}", "错误",
                                  MessageBoxButtons.OK,
                                  MessageBoxIcon.Error);
                    return null;
                }
            }
        }

        private void showBalloonTip(string translatedText)
        {
            NotifyIcon notifyIcon = new NotifyIcon();
            notifyIcon.Icon = SystemIcons.Information;
            notifyIcon.Visible = true;

            // Show balloon tip
            notifyIcon.ShowBalloonTip(3000, "Whisper Window", $"Result：{translatedText}", ToolTipIcon.Info);

            // Hide NotifyIcon after a while
            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Interval = 4000;
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
        }


        private void textBox1_Click(object? sender, EventArgs e)
        {
            TextBox textBox1 = sender as TextBox;
            if (textBox1 != null && !string.IsNullOrEmpty(textBox1.Text))
            {
                textBox1.SelectAll();
                try
                {
                    Clipboard.SetText(textBox1.Text);
                    PlaySound("whisper_windows.Resources.copy.wav");
                }
                catch (ArgumentNullException)
                {
                    Console.WriteLine("Failed to set text to clipboard. Text is null.");
                }
            }
            else
            {
                Console.WriteLine("TextBox is null or text is empty.");
            }
        }

        private void settingsButton_Click(object sender, EventArgs e)
        {
            OpenSettings();
        }

    }
}
