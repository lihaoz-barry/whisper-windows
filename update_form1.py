import sys

# 读取原文件
with open(r'c:\Users\Barry\source\repos\whisper windows\Form1.cs', 'r', encoding='utf-8') as f:
    lines = f.readlines()

# 1. 在第 60 行后添加托盘字段
insert_after_line = -1
for i, line in enumerate(lines):
    if 'private Boolean isRecording ;' in line:
        insert_after_line = i + 1
        break

if insert_after_line > 0:
    lines.insert(insert_after_line, '        \n')
    lines.insert(insert_after_line + 1, '        // 系统托盘\n')
    lines.insert(insert_after_line + 2, '        private NotifyIcon trayIcon;\n')
    lines.insert(insert_after_line + 3, '        private ContextMenuStrip trayMenu;\n')

# 2. 在构造函数末尾添加初始化代码
for i, line in enumerate(lines):
    if 'this.Controls.Add(textBox1);' in line:
        # 找到这行后的下一个空行
        j = i + 1
        while j < len(lines) and lines[j].strip() == '':
            j += 1
        # 在构造函数结束 } 之前插入
        if j < len(lines) and '}' in lines[j]:
            lines.insert(j, '            \n')
            lines.insert(j + 1, '            // 初始化系统托盘\n')
            lines.insert(j + 2, '            InitializeTrayIcon();\n')
            lines.insert(j + 3, '            \n')
            lines.insert(j + 4, '            // 检查 Token 是否配置（首次运行）\n')
            lines.insert(j + 5, '            CheckTokenConfiguration();\n')
        break

# 3. 在构造函数后添加新方法
constructor_end = -1
for i, line in enumerate(lines):
    if 'public Form1()' in line:
        # 找到对应的结束大括号
        brace_count = 0
        for j in range(i, len(lines)):
            if '{' in lines[j]:
                brace_count += 1
            if '}' in lines[j]:
                brace_count -= 1
                if brace_count == 0:
                    constructor_end = j + 1
                    break
        break

if constructor_end > 0:
    new_methods = '''
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
                    "欢迎使用 Whisper Windows！\\n\\n" +
                    "请先配置您的 OpenAI API Token 才能使用语音转录功能。\\n\\n" +
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

'''
    lines.insert(constructor_end, new_methods + '\n')

# 4. 修改 StartRecording 方法
for i, line in enumerate(lines):
    if 'private void StartRecording()' in line:
        # 找到方法开头的 {
        for j in range(i, len(lines)):
            if '{' in lines[j]:
                lines.insert(j + 1, '            // 检查 Token 是否配置\n')
                lines.insert(j + 2, '            if (!TokenManager.IsTokenConfigured())\n')
                lines.insert(j + 3, '            {\n')
                lines.insert(j + 4, '                MessageBox.Show("请先在设置中配置 API Token", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);\n')
                lines.insert(j + 5, '                return;\n')
                lines.insert(j + 6, '            }\n')
                lines.insert(j + 7, '            \n')
                break
        break

# 5. 修改 SendAudioToWhisperAPI - 找到硬编码的 API key
for i, line in enumerate(lines):
    if 'Bearer sk-proj-' in line:
        # 替换这几行
        # 删除包含 Bearer sk-proj- 的行
        del lines[i]
        # 插入新代码
        lines.insert(i, '                // 获取存储的 Token\n')
        lines.insert(i + 1, '                string apiKey = TokenManager.GetDecryptedToken();\n')
        lines.insert(i + 2, '                if (string.IsNullOrEmpty(apiKey))\n')
        lines.insert(i + 3, '                {\n')
        lines.insert(i + 4, '                    MessageBox.Show("API Token 未配置，请在设置中配置", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);\n')
        lines.insert(i + 5, '                    return;\n')
        lines.insert(i + 6, '                }\n')
        lines.insert(i + 7, '                \n')
        lines.insert(i + 8, '                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");\n')
        break

# 6. 写回文件
with open(r'c:\Users\Barry\source\repos\whisper windows\Form1.cs', 'w', encoding='utf-8') as f:
    f.writelines(lines)

print("Form1.cs updated successfully!")
