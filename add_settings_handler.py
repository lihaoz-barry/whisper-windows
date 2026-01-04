import sys

# 读取原文件
with open(r'c:\Users\Barry\source\repos\whisper windows\Form1.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# 在文件末尾（class结束之前）添加设置按钮事件处理器
# 找到最后一个 }（结束 class）
last_brace_pos = content.rfind('    }\n}')

if last_brace_pos > 0:
    # 在最后一个方法后添加设置按钮事件处理器
    event_handler = '''
        private void settingsButton_Click(object sender, EventArgs e)
        {
            OpenSettings();
        }
'''
    content = content[:last_brace_pos] + event_handler + '\n' + content[last_brace_pos:]

# 写回文件
with open(r'c:\Users\Barry\source\repos\whisper windows\Form1.cs', 'w', encoding='utf-8') as f:
    f.write(content)

print("Settings button click handler added!")
