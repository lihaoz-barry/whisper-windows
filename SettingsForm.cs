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
            // Form properties
            this.Width = 500;
            this.Height = 350;
            this.Text = "Settings - Whisper Windows";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            int leftMargin = 20;
            int currentY = 20;

            // Title label
            titleLabel = new Label();
            titleLabel.Text = "OpenAI API Token Configuration";
            titleLabel.Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold);
            titleLabel.Location = new Point(leftMargin, currentY);
            titleLabel.Size = new Size(450, 30);
            this.Controls.Add(titleLabel);
            currentY += 40;

            // Instruction text
            instructionLabel = new Label();
            instructionLabel.Text = "Configure your OpenAI API Token here to use Whisper transcription.\nToken will be encrypted using Windows DPAPI.";
            instructionLabel.Font = new Font("Microsoft YaHei UI", 9F);
            instructionLabel.ForeColor = Color.FromArgb(100, 100, 100);
            instructionLabel.Location = new Point(leftMargin, currentY);
            instructionLabel.Size = new Size(450, 35);
            this.Controls.Add(instructionLabel);
            currentY += 45;

            // Current Token display
            currentTokenLabel = new Label();
            currentTokenLabel.Text = "Current Token:";
            currentTokenLabel.Font = new Font("Microsoft YaHei UI", 9F);
            currentTokenLabel.Location = new Point(leftMargin, currentY);
            currentTokenLabel.Size = new Size(100, 20);
            this.Controls.Add(currentTokenLabel);

            currentTokenValue = new Label();
            currentTokenValue.Text = "Not configured";
            currentTokenValue.Font = new Font("Consolas", 9F);
            currentTokenValue.ForeColor = Color.FromArgb(0, 120, 212);
            currentTokenValue.Location = new Point(leftMargin + 100, currentY);
            currentTokenValue.Size = new Size(350, 20);
            this.Controls.Add(currentTokenValue);
            currentY += 35;

            // New Token input label
            newTokenLabel = new Label();
            newTokenLabel.Text = "Enter new Token:";
            newTokenLabel.Font = new Font("Microsoft YaHei UI", 9F);
            newTokenLabel.Location = new Point(leftMargin, currentY);
            newTokenLabel.Size = new Size(450, 20);
            this.Controls.Add(newTokenLabel);
            currentY += 25;

            // Token input box
            tokenTextBox = new TextBox();
            tokenTextBox.Location = new Point(leftMargin, currentY);
            tokenTextBox.Size = new Size(450, 25);
            tokenTextBox.Font = new Font("Consolas", 9F);
            tokenTextBox.PasswordChar = '*';
            tokenTextBox.PlaceholderText = "sk-proj-...";
            this.Controls.Add(tokenTextBox);
            currentY += 40;

            // Test connection button
            testButton = new Button();
            testButton.Text = "Test Connection";
            testButton.Location = new Point(leftMargin, currentY);
            testButton.Size = new Size(120, 35);
            testButton.Font = new Font("Microsoft YaHei UI", 9F);
            testButton.FlatStyle = FlatStyle.System;
            testButton.Click += TestButton_Click;
            this.Controls.Add(testButton);

            // Save button
            saveButton = new Button();
            saveButton.Text = "Save";
            saveButton.Location = new Point(270, currentY);
            saveButton.Size = new Size(95, 35);
            saveButton.Font = new Font("Microsoft YaHei UI", 9F);
            saveButton.FlatStyle = FlatStyle.System;
            saveButton.Click += SaveButton_Click;
            this.Controls.Add(saveButton);

            // Cancel button
            cancelButton = new Button();
            cancelButton.Text = "Cancel";
            cancelButton.Location = new Point(375, currentY);
            cancelButton.Size = new Size(95, 35);
            cancelButton.Font = new Font("Microsoft YaHei UI", 9F);
            cancelButton.FlatStyle = FlatStyle.System;
            cancelButton.Click += CancelButton_Click;
            this.Controls.Add(cancelButton);
            currentY += 50;

            // Status label
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
                currentTokenValue.Text = "Not configured";
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
                    ShowStatus("Please enter a Token first", false);
                    return;
                }
            }

            if (!TokenManager.IsValidTokenFormat(token))
            {
                ShowStatus("Invalid Token format, should start with sk-", false);
                return;
            }

            ShowStatus("Testing connection...", true);
            testButton.Enabled = false;
            saveButton.Enabled = false;

            try
            {
                // Call Whisper API for testing
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                    client.Timeout = TimeSpan.FromSeconds(10);

                    // Simple validation: try accessing models endpoint
                    var response = await client.GetAsync("https://api.openai.com/v1/models");

                    if (response.IsSuccessStatusCode)
                    {
                        ShowStatus("Connection successful! Token is valid", true);
                    }
                    else
                    {
                        ShowStatus($"Connection failed: {response.StatusCode}", false);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Connection failed: {ex.Message}", false);
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
                ShowStatus("Please enter a Token", false);
                return;
            }

            if (!TokenManager.IsValidTokenFormat(token))
            {
                ShowStatus("Invalid Token format, should start with sk-", false);
                return;
            }

            try
            {
                TokenManager.SaveEncryptedToken(token);
                ShowStatus("Token encrypted and saved", true);

                // Update display
                LoadCurrentToken();

                // Clear input box
                tokenTextBox.Clear();

                // Close window after 2 seconds
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
                ShowStatus($"Save failed: {ex.Message}", false);
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
