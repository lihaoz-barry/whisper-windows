using System;
using System.Drawing;
using System.Net.Http;
using System.Windows.Forms;

namespace whisper_windows
{
    public partial class SettingsForm : Form
    {
        private Label titleLabel;
        private Label instructionLabel;
        private Label currentTokenLabel;
        private Label currentTokenValue;
        private Label newTokenLabel;
        private TextBox tokenTextBox;
        private Button testButton;
        private Button saveButton;
        private Button cancelButton;
        private Label statusLabel;

        public SettingsForm()
        {
            InitializeComponent();
            SetupForm();
            LoadCurrentToken();
        }

        private void SetupForm()
        {
            // 窗体基本属性
            this.Width = 500;
            this.Height = 350;
            this.Text = "设置 - Whisper Windows";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            int leftMargin = 20;
            int currentY = 20;

            // 标题标签
            titleLabel = new Label();
            titleLabel.Text = "⚙ OpenAI API Token 配置";
            titleLabel.Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold);
            titleLabel.Location = new Point(leftMargin, currentY);
            titleLabel.Size = new Size(450, 30);
            this.Controls.Add(titleLabel);
            currentY += 40;

            // 说明文字
            instructionLabel = new Label();
            instructionLabel.Text = "在此配置您的 OpenAI API Token 以使用 Whisper 语音转录服务。\nToken 将使用 Windows DPAPI 加密存储。";
            instructionLabel.Font = new Font("Microsoft YaHei UI", 9F);
            instructionLabel.ForeColor = Color.FromArgb(100, 100, 100);
            instructionLabel.Location = new Point(leftMargin, currentY);
            instructionLabel.Size = new Size(450, 35);
            this.Controls.Add(instructionLabel);
            currentY += 45;

            // 当前 Token 显示
            currentTokenLabel = new Label();
            currentTokenLabel.Text = "当前 Token:";
            currentTokenLabel.Font = new Font("Microsoft YaHei UI", 9F);
            currentTokenLabel.Location = new Point(leftMargin, currentY);
            currentTokenLabel.Size = new Size(100, 20);
            this.Controls.Add(currentTokenLabel);

            currentTokenValue = new Label();
            currentTokenValue.Text = "未配置";
            currentTokenValue.Font = new Font("Consolas", 9F);
            currentTokenValue.ForeColor = Color.FromArgb(0, 120, 212);
            currentTokenValue.Location = new Point(leftMargin + 100, currentY);
            currentTokenValue.Size = new Size(350, 20);
            this.Controls.Add(currentTokenValue);
            currentY += 35;

            // 新 Token 输入标签
            newTokenLabel = new Label();
            newTokenLabel.Text = "输入新 Token:";
            newTokenLabel.Font = new Font("Microsoft YaHei UI", 9F);
            newTokenLabel.Location = new Point(leftMargin, currentY);
            newTokenLabel.Size = new Size(450, 20);
            this.Controls.Add(newTokenLabel);
            currentY += 25;

            // Token 输入框
            tokenTextBox = new TextBox();
            tokenTextBox.Location = new Point(leftMargin, currentY);
            tokenTextBox.Size = new Size(450, 25);
            tokenTextBox.Font = new Font("Consolas", 9F);
            tokenTextBox.PasswordChar = '*';
            tokenTextBox.PlaceholderText = "sk-proj-...";
            this.Controls.Add(tokenTextBox);
            currentY += 40;

            // 测试连接按钮
            testButton = new Button();
            testButton.Text = "🔍 测试连接";
            testButton.Location = new Point(leftMargin, currentY);
            testButton.Size = new Size(120, 35);
            testButton.Font = new Font("Microsoft YaHei UI", 9F);
            testButton.FlatStyle = FlatStyle.System;
            testButton.Click += TestButton_Click;
            this.Controls.Add(testButton);

            // 保存按钮
            saveButton = new Button();
            saveButton.Text = "💾 保存";
            saveButton.Location = new Point(270, currentY);
            saveButton.Size = new Size(95, 35);
            saveButton.Font = new Font("Microsoft YaHei UI", 9F);
            saveButton.FlatStyle = FlatStyle.System;
            saveButton.Click += SaveButton_Click;
            this.Controls.Add(saveButton);

            // 取消按钮
            cancelButton = new Button();
            cancelButton.Text = "取消";
            cancelButton.Location = new Point(375, currentY);
            cancelButton.Size = new Size(95, 35);
            cancelButton.Font = new Font("Microsoft YaHei UI", 9F);
            cancelButton.FlatStyle = FlatStyle.System;
            cancelButton.Click += CancelButton_Click;
            this.Controls.Add(cancelButton);
            currentY += 50;

            // 状态标签
            statusLabel = new Label();
            statusLabel.Text = "";
            statusLabel.Font = new Font("Microsoft YaHei UI", 9F);
            statusLabel.Location = new Point(leftMargin, currentY);
            statusLabel.Size = new Size(450, 40);
            statusLabel.ForeColor = Color.FromArgb(0, 120, 0);
            this.Controls.Add(statusLabel);
        }

        private void LoadCurrentToken()
        {
            string? maskedToken = TokenManager.GetMaskedToken();
            if (!string.IsNullOrEmpty(maskedToken))
            {
                currentTokenValue.Text = maskedToken;
                currentTokenValue.ForeColor = Color.FromArgb(0, 120, 212);
            }
            else
            {
                currentTokenValue.Text = "未配置";
                currentTokenValue.ForeColor = Color.FromArgb(180, 0, 0);
            }
        }

        private async void TestButton_Click(object sender, EventArgs e)
        {
            string token = tokenTextBox.Text.Trim();
            
            if (string.IsNullOrEmpty(token))
            {
                token = TokenManager.GetDecryptedToken();
                if (string.IsNullOrEmpty(token))
                {
                    ShowStatus("请先输入 Token", false);
                    return;
                }
            }

            if (!TokenManager.IsValidTokenFormat(token))
            {
                ShowStatus("Token 格式不正确，应以 sk- 开头", false);
                return;
            }

            ShowStatus("正在测试连接...", true);
            testButton.Enabled = false;
            saveButton.Enabled = false;

            try
            {
                // 调用 Whisper API 进行测试（使用一个空文件或小文件）
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                    client.Timeout = TimeSpan.FromSeconds(10);

                    // 简单验证：尝试访问 models endpoint
                    var response = await client.GetAsync("https://api.openai.com/v1/models");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        ShowStatus("✓ 连接成功！Token 有效", true);
                    }
                    else
                    {
                        ShowStatus($"✗ 连接失败：{response.StatusCode}", false);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"✗ 连接失败：{ex.Message}", false);
            }
            finally
            {
                testButton.Enabled = true;
                saveButton.Enabled = true;
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            string token = tokenTextBox.Text.Trim();

            if (string.IsNullOrEmpty(token))
            {
                ShowStatus("请输入 Token", false);
                return;
            }

            if (!TokenManager.IsValidTokenFormat(token))
            {
                ShowStatus("Token 格式不正确，应以 sk- 开头", false);
                return;
            }

            try
            {
                TokenManager.SaveEncryptedToken(token);
                ShowStatus("✓ Token 已加密保存", true);
                
                // 更新显示
                LoadCurrentToken();
                
                // 清空输入框
                tokenTextBox.Clear();

                // 2 秒后关闭窗口
                System.Windows.Forms.Timer closeTimer = new System.Windows.Forms.Timer();
                closeTimer.Interval = 2000;
                closeTimer.Tick += (s, args) =>
                {
                    closeTimer.Stop();
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                };
                closeTimer.Start();
            }
            catch (Exception ex)
            {
                ShowStatus($"✗ 保存失败：{ex.Message}", false);
            }
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ShowStatus(string message, bool isSuccess)
        {
            statusLabel.Text = message;
            statusLabel.ForeColor = isSuccess ? Color.FromArgb(0, 120, 0) : Color.FromArgb(180, 0, 0);
        }
    }
}
