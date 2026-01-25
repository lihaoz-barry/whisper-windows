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

        // 重试功能相关 / Retry functionality
        private string cachedAudioFilePath = null;
        private string lastErrorMessage = null;
        private bool hasFailedTranscription = false;
        private static readonly string CacheDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WhisperWindows", "FailedAudioCache");
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

            // 初始化重试按钮 / Initialize retry button
            InitializeRetryButton();

            // 确保缓存目录存在 / Ensure cache directory exists
            EnsureCacheDirectoryExists();

            // 检查是否有缓存的失败音频 / Check for cached failed audio
            CheckForCachedFailedAudio();

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

            var clearCacheItem = new ToolStripMenuItem("清除失败音频缓存");
            clearCacheItem.Click += (s, e) => ClearFailedAudioCache();

            var exitItem = new ToolStripMenuItem("退出");
            exitItem.Click += (s, e) => {
                trayIcon.Visible = false;
                Application.Exit();
            };

            trayMenu.Items.Add(showItem);
            trayMenu.Items.Add(settingsItem);
            trayMenu.Items.Add(clearCacheItem);
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
                MessageBox.Show($"Failed to process audio: {ex.Message}", "错误",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Error);
            }
        }

        private async Task HandleSegmentedAudio(string filePath)
        {
            List<AudioSegment> segments = null;
            bool hasError = false;
            try
            {
                // Segment the audio
                textBox1.Text = "正在分段音频...\nSegmenting audio...";
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
                    textBox1.Text = $"正在转录第 {segment.SegmentNumber}/{segment.TotalSegments} 段...\nTranscribing part {segment.SegmentNumber}/{segment.TotalSegments}...";
                    this.Refresh();

                    string transcription = await SendSingleAudioSegment(segment.FilePath, segment);
                    if (string.IsNullOrEmpty(transcription))
                    {
                        // 分段转录失败，缓存原始文件 / Segment transcription failed, cache original file
                        hasError = true;
                        CacheAndShowRetryOption(filePath, $"分段 {segment.SegmentNumber} 转录失败，请重试\nSegment {segment.SegmentNumber} transcription failed, please retry");
                        return;
                    }
                    allTranscriptions.Add(transcription);
                }

                // Merge results
                string finalText = string.Join(" ", allTranscriptions);

                // 成功后清除之前的缓存 / Clear previous cache on success
                if (!string.IsNullOrEmpty(cachedAudioFilePath))
                {
                    ClearCachedFile(cachedAudioFilePath);
                    cachedAudioFilePath = null;
                }
                HideRetryOption();

                textBox1.Text = finalText;
                Clipboard.SetText(finalText);
                PlaySound("whisper_windows.Resources.copy.wav");
                FlashWindow(this);
                showBalloonTip(finalText);
            }
            catch (Exception ex)
            {
                hasError = true;
                CacheAndShowRetryOption(filePath, $"处理分段音频失败: {ex.Message}\nFailed to process segmented audio: {ex.Message}");
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
                    // 缓存音频文件以便重试 / Cache audio file for retry
                    CacheAndShowRetryOption(filePath, "API Token 未配置，请在设置中配置\nAPI Token not configured");
                    return null;
                }

                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                client.Timeout = TimeSpan.FromMinutes(5); // 增加超时时间 / Increase timeout

                var content = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(File.ReadAllBytes(filePath));
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");

                content.Add(fileContent, "file", Path.GetFileName(filePath));
                content.Add(new StringContent("whisper-1"), "model");

                try
                {
                    var response = await client.PostAsync("https://api.openai.com/v1/audio/transcriptions", content);
                    var resultText = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        // API 返回错误，缓存并显示重试选项 / API returned error, cache and show retry
                        CacheAndShowRetryOption(filePath, $"API 错误 ({response.StatusCode}): {resultText}\nAPI Error ({response.StatusCode}): {resultText}");
                        return null;
                    }

                    var jsonDocument = System.Text.Json.JsonDocument.Parse(resultText);
                    var text = jsonDocument.RootElement.GetProperty("text").GetString();

                    // For single segment, display immediately
                    if (segment == null)
                    {
                        // 成功后清除之前的缓存 / Clear previous cache on success
                        if (!string.IsNullOrEmpty(cachedAudioFilePath))
                        {
                            ClearCachedFile(cachedAudioFilePath);
                            cachedAudioFilePath = null;
                        }
                        HideRetryOption();

                        textBox1.Text = text;
                        Clipboard.SetText(text);
                        PlaySound("whisper_windows.Resources.copy.wav");
                        FlashWindow(this);
                        showBalloonTip(text);
                    }

                    return text;
                }
                catch (TaskCanceledException)
                {
                    CacheAndShowRetryOption(filePath, "请求超时，请点击重试按钮\nRequest timed out, click Retry");
                    return null;
                }
                catch (HttpRequestException ex)
                {
                    CacheAndShowRetryOption(filePath, $"网络错误: {ex.Message}\nNetwork error: {ex.Message}");
                    return null;
                }
                catch (Exception ex)
                {
                    CacheAndShowRetryOption(filePath, $"转录失败: {ex.Message}\nTranscription failed: {ex.Message}");
                    return null;
                }
            }
        }

        /// <summary>
        /// 缓存失败的音频并显示重试选项 / Cache failed audio and show retry option
        /// </summary>
        private void CacheAndShowRetryOption(string filePath, string errorMessage)
        {
            // 缓存音频文件 / Cache the audio file
            string cached = CacheFailedAudio(filePath);
            if (cached != null)
            {
                cachedAudioFilePath = cached;
            }
            ShowRetryOption(errorMessage);
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

        #region Retry Functionality / 重试功能

        /// <summary>
        /// 初始化重试按钮 / Initialize retry button
        /// </summary>
        private void InitializeRetryButton()
        {
            retryButton.Visible = false;
            retryButton.Click += retryButton_Click;
        }

        /// <summary>
        /// 确保缓存目录存在 / Ensure cache directory exists
        /// </summary>
        private void EnsureCacheDirectoryExists()
        {
            if (!Directory.Exists(CacheDirectory))
            {
                Directory.CreateDirectory(CacheDirectory);
            }
        }

        /// <summary>
        /// 检查是否有缓存的失败音频 / Check for cached failed audio on startup
        /// </summary>
        private void CheckForCachedFailedAudio()
        {
            try
            {
                if (Directory.Exists(CacheDirectory))
                {
                    var cachedFiles = Directory.GetFiles(CacheDirectory, "*.wav");
                    if (cachedFiles.Length > 0)
                    {
                        // 使用最新的缓存文件 / Use the most recent cached file
                        cachedAudioFilePath = cachedFiles.OrderByDescending(f => File.GetCreationTime(f)).First();
                        hasFailedTranscription = true;
                        ShowRetryOption($"发现上次失败的录音，点击重试按钮重新转录\nFound previous failed recording, click Retry to transcribe again");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking cached audio: {ex.Message}");
            }
        }

        /// <summary>
        /// 缓存失败的音频文件 / Cache failed audio file
        /// </summary>
        private string CacheFailedAudio(string sourceFilePath)
        {
            try
            {
                EnsureCacheDirectoryExists();

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string cachedFileName = $"failed_audio_{timestamp}.wav";
                string cachedFilePath = Path.Combine(CacheDirectory, cachedFileName);

                // 复制文件到缓存目录 / Copy file to cache directory
                File.Copy(sourceFilePath, cachedFilePath, true);

                return cachedFilePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to cache audio file: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 显示重试选项 / Show retry option
        /// </summary>
        private void ShowRetryOption(string errorMessage)
        {
            hasFailedTranscription = true;
            lastErrorMessage = errorMessage;
            retryButton.Visible = true;
            textBox1.Text = errorMessage;
            textBox1.ForeColor = System.Drawing.Color.Red;
        }

        /// <summary>
        /// 隐藏重试选项 / Hide retry option
        /// </summary>
        private void HideRetryOption()
        {
            hasFailedTranscription = false;
            lastErrorMessage = null;
            retryButton.Visible = false;
            textBox1.ForeColor = System.Drawing.SystemColors.WindowText;
        }

        /// <summary>
        /// 重试按钮点击事件 / Retry button click handler
        /// </summary>
        private async void retryButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(cachedAudioFilePath) || !File.Exists(cachedAudioFilePath))
            {
                MessageBox.Show("缓存的音频文件不存在\nCached audio file not found", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                HideRetryOption();
                return;
            }

            // 禁用重试按钮防止重复点击 / Disable retry button to prevent double clicks
            retryButton.Enabled = false;
            textBox1.Text = "正在重试转录...\nRetrying transcription...";
            textBox1.ForeColor = System.Drawing.SystemColors.WindowText;

            try
            {
                bool success = await RetryTranscription(cachedAudioFilePath);
                if (success)
                {
                    // 成功后清除缓存 / Clear cache on success
                    ClearCachedFile(cachedAudioFilePath);
                    cachedAudioFilePath = null;
                    HideRetryOption();
                }
            }
            finally
            {
                retryButton.Enabled = true;
            }
        }

        /// <summary>
        /// 重试转录 / Retry transcription
        /// </summary>
        private async Task<bool> RetryTranscription(string filePath)
        {
            try
            {
                // 检查是否需要分段 / Check if segmentation is needed
                if (AudioSegmenter.NeedsSegmentation(filePath))
                {
                    return await RetrySegmentedAudio(filePath);
                }
                else
                {
                    return await RetrySingleAudioSegment(filePath);
                }
            }
            catch (Exception ex)
            {
                ShowRetryOption($"重试失败: {ex.Message}\nRetry failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 重试分段音频转录 / Retry segmented audio transcription
        /// </summary>
        private async Task<bool> RetrySegmentedAudio(string filePath)
        {
            List<AudioSegment> segments = null;
            try
            {
                textBox1.Text = "正在分段音频...\nSegmenting audio...";
                segments = AudioSegmenter.SegmentAudio(filePath);

                if (segments.Count == 0)
                {
                    return await RetrySingleAudioSegment(filePath);
                }

                List<string> allTranscriptions = new List<string>();

                for (int i = 0; i < segments.Count; i++)
                {
                    var segment = segments[i];
                    textBox1.Text = $"正在转录第 {segment.SegmentNumber}/{segment.TotalSegments} 段...\nTranscribing part {segment.SegmentNumber}/{segment.TotalSegments}...";
                    this.Refresh();

                    string transcription = await SendAudioForTranscription(segment.FilePath);
                    if (transcription == null)
                    {
                        ShowRetryOption($"分段 {segment.SegmentNumber} 转录失败\nSegment {segment.SegmentNumber} transcription failed");
                        return false;
                    }
                    allTranscriptions.Add(transcription);
                }

                string finalText = string.Join(" ", allTranscriptions);
                OnTranscriptionSuccess(finalText);
                return true;
            }
            finally
            {
                if (segments != null)
                {
                    AudioSegmenter.CleanupSegments(segments);
                }
            }
        }

        /// <summary>
        /// 重试单个音频转录 / Retry single audio transcription
        /// </summary>
        private async Task<bool> RetrySingleAudioSegment(string filePath)
        {
            string transcription = await SendAudioForTranscription(filePath);
            if (transcription != null)
            {
                OnTranscriptionSuccess(transcription);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 发送音频进行转录 / Send audio for transcription
        /// </summary>
        private async Task<string> SendAudioForTranscription(string filePath)
        {
            using (var client = new HttpClient())
            {
                string apiKey = TokenManager.GetDecryptedToken();

                if (string.IsNullOrEmpty(apiKey))
                {
                    ShowRetryOption("API Token 未配置\nAPI Token not configured");
                    return null;
                }

                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                client.Timeout = TimeSpan.FromMinutes(5); // 增加超时时间 / Increase timeout

                var content = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(File.ReadAllBytes(filePath));
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");

                content.Add(fileContent, "file", Path.GetFileName(filePath));
                content.Add(new StringContent("whisper-1"), "model");

                try
                {
                    var response = await client.PostAsync("https://api.openai.com/v1/audio/transcriptions", content);
                    var resultText = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        string errorMsg = $"API 错误 ({response.StatusCode}): {resultText}\nAPI Error ({response.StatusCode}): {resultText}";
                        ShowRetryOption(errorMsg);
                        return null;
                    }

                    var jsonDocument = System.Text.Json.JsonDocument.Parse(resultText);
                    return jsonDocument.RootElement.GetProperty("text").GetString();
                }
                catch (TaskCanceledException)
                {
                    ShowRetryOption("请求超时，请重试\nRequest timed out, please retry");
                    return null;
                }
                catch (HttpRequestException ex)
                {
                    ShowRetryOption($"网络错误: {ex.Message}\nNetwork error: {ex.Message}");
                    return null;
                }
                catch (Exception ex)
                {
                    ShowRetryOption($"转录失败: {ex.Message}\nTranscription failed: {ex.Message}");
                    return null;
                }
            }
        }

        /// <summary>
        /// 转录成功处理 / Handle transcription success
        /// </summary>
        private void OnTranscriptionSuccess(string text)
        {
            textBox1.Text = text;
            textBox1.ForeColor = System.Drawing.SystemColors.WindowText;
            Clipboard.SetText(text);
            PlaySound("whisper_windows.Resources.copy.wav");
            FlashWindow(this);
            showBalloonTip(text);
        }

        /// <summary>
        /// 清除单个缓存文件 / Clear single cached file
        /// </summary>
        private void ClearCachedFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to delete cached file: {ex.Message}");
            }
        }

        /// <summary>
        /// 清除所有失败音频缓存 / Clear all failed audio cache
        /// </summary>
        private void ClearFailedAudioCache()
        {
            try
            {
                if (Directory.Exists(CacheDirectory))
                {
                    var files = Directory.GetFiles(CacheDirectory, "*.wav");
                    int deletedCount = 0;

                    foreach (var file in files)
                    {
                        try
                        {
                            File.Delete(file);
                            deletedCount++;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to delete {file}: {ex.Message}");
                        }
                    }

                    cachedAudioFilePath = null;
                    HideRetryOption();

                    MessageBox.Show($"已清除 {deletedCount} 个缓存文件\nCleared {deletedCount} cached files",
                        "缓存已清除", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("没有缓存文件需要清除\nNo cached files to clear",
                        "缓存为空", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"清除缓存失败: {ex.Message}\nFailed to clear cache: {ex.Message}",
                    "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

    }
}