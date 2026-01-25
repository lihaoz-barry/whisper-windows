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
using System.Net;

namespace whisper_windows
{
    /// <summary>
    /// Represents the current transcription status
    /// </summary>
    public enum TranscriptionStatus
    {
        Idle,
        Recording,
        Transcribing,
        Success,
        Failed
    }

    /// <summary>
    /// Contains information about a failed transcription for retry purposes
    /// </summary>
    public class FailedTranscription
    {
        public string AudioFilePath { get; set; }
        public DateTime FailedAt { get; set; }
        public string ErrorMessage { get; set; }
        public int RetryCount { get; set; }
    }

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

        // Transcription status tracking
        private TranscriptionStatus currentStatus = TranscriptionStatus.Idle;
        private FailedTranscription lastFailedTranscription = null;

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

            // Initialize retry button as hidden
            UpdateRetryButtonVisibility();
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
                currentStatus = TranscriptionStatus.Recording;
                UpdateRetryButtonVisibility();
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
            currentStatus = TranscriptionStatus.Transcribing;
            UpdateRetryButtonVisibility();

            try
            {
                // Check if audio needs segmentation
                if (AudioSegmenter.NeedsSegmentation(filePath))
                {
                    await HandleSegmentedAudio(filePath);
                }
                else
                {
                    await SendSingleAudioSegment(filePath, null);
                }
            }
            catch (Exception ex)
            {
                HandleTranscriptionError(filePath, ex.Message);
            }
        }

        private async Task HandleSegmentedAudio(string filePath)
        {
            List<AudioSegment> segments = null;
            try
            {
                // Segment the audio
                textBox1.Text = "Segmenting audio...";
                segments = AudioSegmenter.SegmentAudio(filePath);

                if (segments.Count == 0)
                {
                    await SendSingleAudioSegment(filePath, null);
                    return;
                }

                List<string> allTranscriptions = new List<string>();

                // Process each segment
                for (int i = 0; i < segments.Count; i++)
                {
                    var segment = segments[i];
                    textBox1.Text = $"Transcribing part {segment.SegmentNumber}/{segment.TotalSegments}...";
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

                // Mark as success
                currentStatus = TranscriptionStatus.Success;
                lastFailedTranscription = null;
                UpdateRetryButtonVisibility();

                PlaySound("whisper_windows.Resources.copy.wav");
                FlashWindow(this);
                showBalloonTip(finalText);
            }
            catch (Exception ex)
            {
                HandleTranscriptionError(filePath, ex.Message);
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
                client.Timeout = TimeSpan.FromSeconds(120); // Set explicit timeout

                // Get stored token
                string apiKey = TokenManager.GetDecryptedToken();

                if (string.IsNullOrEmpty(apiKey))
                {
                    throw new InvalidOperationException("API Token 未配置，请在设置中配置");
                }

                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var content = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(File.ReadAllBytes(filePath));
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");

                content.Add(fileContent, "file", Path.GetFileName(filePath));
                content.Add(new StringContent("whisper-1"), "model");

                var response = await client.PostAsync("https://api.openai.com/v1/audio/transcriptions", content);
                var resultText = await response.Content.ReadAsStringAsync();

                // Check HTTP status code for errors
                if (!response.IsSuccessStatusCode)
                {
                    string errorMessage = GetErrorMessageFromResponse(response.StatusCode, resultText);
                    throw new HttpRequestException(errorMessage);
                }

                var jsonDocument = System.Text.Json.JsonDocument.Parse(resultText);

                // Check if response contains error
                if (jsonDocument.RootElement.TryGetProperty("error", out var errorElement))
                {
                    string errorMsg = errorElement.TryGetProperty("message", out var msgElement)
                        ? msgElement.GetString()
                        : "Unknown API error";
                    throw new HttpRequestException($"API Error: {errorMsg}");
                }

                var text = jsonDocument.RootElement.GetProperty("text").GetString();

                // For single segment, display immediately
                if (segment == null)
                {
                    textBox1.Text = text;
                    textBox1.ForeColor = System.Drawing.SystemColors.WindowText; // Reset to normal color
                    Clipboard.SetText(text);

                    // Mark as success
                    currentStatus = TranscriptionStatus.Success;
                    lastFailedTranscription = null;
                    UpdateRetryButtonVisibility();

                    PlaySound("whisper_windows.Resources.copy.wav");
                    FlashWindow(this);
                    showBalloonTip(text);
                }

                return text;
            }
        }

        /// <summary>
        /// Gets a user-friendly error message based on HTTP status code
        /// </summary>
        private string GetErrorMessageFromResponse(HttpStatusCode statusCode, string responseBody)
        {
            // Try to extract error message from response body
            string apiMessage = "";
            try
            {
                var jsonDocument = System.Text.Json.JsonDocument.Parse(responseBody);
                if (jsonDocument.RootElement.TryGetProperty("error", out var errorElement))
                {
                    if (errorElement.TryGetProperty("message", out var msgElement))
                    {
                        apiMessage = msgElement.GetString();
                    }
                }
            }
            catch { }

            switch (statusCode)
            {
                case HttpStatusCode.Unauthorized:
                    return "认证失败：API Token 无效或已过期。请在设置中检查您的 Token。";
                case HttpStatusCode.TooManyRequests:
                    return "请求过于频繁：已达到 API 速率限制。请稍后重试。";
                case HttpStatusCode.BadRequest:
                    return $"请求错误：{(string.IsNullOrEmpty(apiMessage) ? "音频文件格式可能不支持" : apiMessage)}";
                case HttpStatusCode.InternalServerError:
                case HttpStatusCode.BadGateway:
                case HttpStatusCode.ServiceUnavailable:
                    return "服务器错误：OpenAI 服务暂时不可用。请稍后重试。";
                case HttpStatusCode.RequestTimeout:
                case HttpStatusCode.GatewayTimeout:
                    return "请求超时：服务器响应时间过长。请稍后重试。";
                default:
                    return $"请求失败 ({(int)statusCode}): {(string.IsNullOrEmpty(apiMessage) ? statusCode.ToString() : apiMessage)}";
            }
        }

        /// <summary>
        /// Handles transcription errors by caching the failed audio and showing error UI
        /// </summary>
        private void HandleTranscriptionError(string audioFilePath, string errorMessage)
        {
            currentStatus = TranscriptionStatus.Failed;

            // Cache the failed transcription for retry
            lastFailedTranscription = new FailedTranscription
            {
                AudioFilePath = audioFilePath,
                FailedAt = DateTime.Now,
                ErrorMessage = errorMessage,
                RetryCount = (lastFailedTranscription?.AudioFilePath == audioFilePath)
                    ? lastFailedTranscription.RetryCount + 1
                    : 0
            };

            // Show error in textbox with red color
            textBox1.ForeColor = System.Drawing.Color.Red;
            textBox1.Text = $"转录失败: {errorMessage}\n\n点击\"重试\"按钮使用缓存的音频重新转录。";

            // Update UI to show retry button
            UpdateRetryButtonVisibility();

            // Flash window to get attention
            FlashWindow(this);

            // Show balloon tip
            trayIcon.ShowBalloonTip(3000,
                "转录失败",
                "音频转录失败，点击重试按钮重新尝试。",
                ToolTipIcon.Error);
        }

        /// <summary>
        /// Updates the visibility of the retry and clear cache buttons based on current status
        /// </summary>
        private void UpdateRetryButtonVisibility()
        {
            bool showRetryButtons = (currentStatus == TranscriptionStatus.Failed && lastFailedTranscription != null);

            retryButton.Visible = showRetryButtons;
            clearCacheButton.Visible = showRetryButtons;

            if (showRetryButtons && lastFailedTranscription != null)
            {
                retryButton.Text = lastFailedTranscription.RetryCount > 0
                    ? $"重试 ({lastFailedTranscription.RetryCount})"
                    : "重试";
            }
        }

        /// <summary>
        /// Retry button click handler - resends cached audio to API
        /// </summary>
        private async void retryButton_Click(object sender, EventArgs e)
        {
            if (lastFailedTranscription == null || !File.Exists(lastFailedTranscription.AudioFilePath))
            {
                MessageBox.Show("没有可重试的音频文件。缓存可能已被清除。", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ClearFailedCache();
                return;
            }

            // Disable retry button during retry
            retryButton.Enabled = false;
            retryButton.Text = "重试中...";

            // Reset text color
            textBox1.ForeColor = System.Drawing.SystemColors.WindowText;
            textBox1.Text = "正在重试转录...";

            try
            {
                currentStatus = TranscriptionStatus.Transcribing;

                string filePath = lastFailedTranscription.AudioFilePath;

                // Check if audio needs segmentation
                if (AudioSegmenter.NeedsSegmentation(filePath))
                {
                    await HandleSegmentedAudio(filePath);
                }
                else
                {
                    await SendSingleAudioSegment(filePath, null);
                }
            }
            catch (Exception ex)
            {
                HandleTranscriptionError(lastFailedTranscription.AudioFilePath, ex.Message);
            }
            finally
            {
                retryButton.Enabled = true;
                UpdateRetryButtonVisibility();
            }
        }

        /// <summary>
        /// Clear cache button click handler - removes cached failed audio
        /// </summary>
        private void clearCacheButton_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "确定要清除缓存的失败音频吗？\n\n清除后将无法重试此次录音。",
                "确认清除",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                ClearFailedCache();
            }
        }

        /// <summary>
        /// Clears the failed transcription cache
        /// </summary>
        private void ClearFailedCache()
        {
            lastFailedTranscription = null;
            currentStatus = TranscriptionStatus.Idle;

            // Reset UI
            textBox1.ForeColor = System.Drawing.SystemColors.WindowText;
            textBox1.Text = "";

            UpdateRetryButtonVisibility();
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
                // Don't copy error messages to clipboard
                if (currentStatus == TranscriptionStatus.Failed)
                {
                    return;
                }

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
