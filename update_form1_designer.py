import sys

# 读取原文件
with open(r'c:\Users\Barry\source\repos\whisper windows\Form1.Designer.cs', 'r', encoding='utf-8') as f:
    lines = f.readlines()

# 1. 在变量声明部分添加 settingsButton
for i, line in enumerate(lines):
    if 'private FileSystemWatcher fileSystemWatcher1;' in line:
        lines.insert(i + 1, '        private Button settingsButton;\n')
        break

# 2. 在 InitializeComponent 中添加设置按钮初始化
#    找到创建控件的地方
for i, line in enumerate(lines):
    if 'this.fileSystemWatcher1 = new System.IO.FileSystemWatcher();' in line:
        lines.insert(i + 1, '            this.settingsButton = new System.Windows.Forms.Button();\n')
        break

# 3. 找到 flowLayoutPanel1 的配置位置，在其后添加 settingsButton 的配置
for i, line in enumerate(lines):
    if '// label1' in line:
        # 在 label1 配置之前添加 settingsButton 配置
        button_config = '''            // 
            // settingsButton
            // 
            this.settingsButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.settingsButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.settingsButton.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.settingsButton.Location = new System.Drawing.Point(346, 8);
            this.settingsButton.Name = "settingsButton";
            this.settingsButton.Size = new System.Drawing.Size(30, 30);
            this.settingsButton.TabIndex = 5;
            this.settingsButton.Text = "⚙";
            this.settingsButton.UseVisualStyleBackColor = true;
            this.settingsButton.Click += new System.EventHandler(this.settingsButton_Click);
            // 
'''
        lines.insert(i, button_config)
        break

# 4. 在 Form1 配置的 Controls.Add 部分添加 settingsButton
for i, line in enumerate(lines):
    if 'this.Controls.Add(this.flowLayoutPanel1);' in line:
        lines.insert(i + 1, '            this.Controls.Add(this.settingsButton);\n')
        break

# 写回文件
with open(r'c:\Users\Barry\source\repos\whisper windows\Form1.Designer.cs', 'w', encoding='utf-8') as f:
    f.writelines(lines)

print("Form1.Designer.cs updated successfully!")
