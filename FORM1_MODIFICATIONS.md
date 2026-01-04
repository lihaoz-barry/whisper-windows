# Form1.cs 手动修改指南

由于 Form1.cs 文件较大（352行），自动化修改容易出错。以下是需要手动添加的代码段：

## 1. 添加系统托盘字段（第60行后）

在 `private Boolean isRecording ;` 后添加：

```csharp
// 系统托盘
private NotifyIcon trayIcon;
private ContextMenuStrip trayMenu;
```

## 2. 在构造函数末尾添加初始化代码（第77行 `}` 之前）

在 `this.Controls.Add(textBox1);` 后、构造函数结束 `}` 之前添加：

```csharp
// 初始化系统托盘
InitializeTrayIcon();

// 检查 Token 是否配置（首次运行）
CheckTokenConfiguration();
```

## 3. 在构造函数后添加新方法

在 `public Form1() {...}` 构造函数的结束 `}` 后添加以下新方法：

```csharp
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
        
        // 显示提示（仅第一次）
        trayIcon.ShowBalloonTip(3000, 
            "Whisper Windows", 
            "应用已最小化到系统托盘，双击托盘图标可重新打开", 
            ToolTipIcon.Info);
    }
    base.OnFormClosing(e);
}
```

## 4. 修改 StartRecording 方法（第152行）

在 `StartRecording()` 方法开头（在 `waveSource = new WaveIn();` 之前）添加：

```csharp
// 检查 Token 是否配置
if (!TokenManager.IsTokenConfigured())
{
    MessageBox.Show("请先在设置中配置 API Token", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    return;
}
```

## 5. 修改 SendAudioToWhisperAPI 方法（第255-259行）

**原代码（第259行）：**
```csharp
client.DefaultRequestHeaders.Add("Authorization", "Bearer YOUR_API_KEY_HERE");
```

**替换为：**
```csharp
// 获取存储的 Token
string apiKey = TokenManager.GetDecryptedToken();
if (string.IsNullOrEmpty(apiKey))
{
    MessageBox.Show("API Token 未配置，请在设置中配置", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
    return;
}

client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
```

## 完成！

完成以上修改后，Form1.cs 将具备：
- ✅ 系统托盘图标
- ✅ 右键托盘菜单（显示/设置/退出）
- ✅ 首次运行欢迎提示
- ✅ 关闭窗口最小化到托盘
- ✅ 使用加密存储的 Token

接下来还需要：
- 在 Form1.Designer.cs 添加设置按钮（齿轮图标）
