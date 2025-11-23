using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace whisper_windows
{
    public partial class SettingsForm : Form
    {
        private Label label;
        private TextBox apiKeyTextBox;
        private Button saveButton;

        public SettingsForm()
        {
            InitializeComponent();
            SetupForm();
        }

        private void SetupForm()
        {
            // 窗体基本属性
            this.Width = 400;
            this.Height = 200;
            this.Text = "API Key 设置";

            // Label
            label = new Label();
            label.Text = "请输入您的 API Key：";
            label.Location = new Point(10, 20);
            label.Size = new Size(180, 20);
            this.Controls.Add(label);

            // TextBox
            apiKeyTextBox = new TextBox();
            apiKeyTextBox.Location = new Point(10, 50);
            apiKeyTextBox.Size = new Size(360, 20);
            this.Controls.Add(apiKeyTextBox);

            // Save Button
            saveButton = new Button();
            saveButton.Text = "保存";
            saveButton.Location = new Point(280, 130);
            saveButton.Size = new Size(100, 30);
            saveButton.Click += SaveButton_Click;
            this.Controls.Add(saveButton);
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            string apiKey = apiKeyTextBox.Text;
            // 这里添加保存 API Key 的逻辑
            MessageBox.Show("API Key 已保存！");
            this.Close();
        }
    }

}
