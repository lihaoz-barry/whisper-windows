import sys

# 读取原文件
with open(r'c:\Users\Barry\source\repos\whisper windows\Form1.cs', 'r', encoding='utf-8') as f:
    lines = f.readlines()

# 找到并替换 SendAudioToWhisperAPI 方法中的环境变量代码
in_send_audio_method = False
method_start_line = -1
brace_count = 0

for i, line in enumerate(lines):
    if 'private async void SendAudioToWhisperAPI' in line:
        in_send_audio_method = True
        method_start_line = i
        continue
    
    if in_send_audio_method:
        # 找到包含环境变量的那一行
        if 'Environment.GetEnvironmentVariable("OPENAI_API_KEY")' in line:
            # 从这一行开始查找需要替换的代码块
            # 查找到 client.DefaultRequestHeaders.Add 这一行结束
            
            # 找到这个代码块的起始（注释行）
            start_idx = i - 2  # "// SECURITY..." 注释
            
            # 找到结束行（Add Authorization header）
            end_idx = i
            for j in range(i, min(i + 20, len(lines))):
                if 'client.DefaultRequestHeaders.Add("Authorization"' in lines[j]:
                    end_idx = j
                    break
            
            # 删除旧代码
            del lines[start_idx:end_idx + 1]
            
            # 插入新代码
            new_code = [
                '                // 获取存储的 Token\n',
                '                string apiKey = TokenManager.GetDecryptedToken();\n',
                '\n',
                '                if (string.IsNullOrEmpty(apiKey))\n',
                '                {\n',
                '                    MessageBox.Show("API Token 未配置，请在设置中配置", "错误",\n',
                '                                  MessageBoxButtons.OK,\n',
                '                                  MessageBoxIcon.Error);\n',
                '                    return;\n',
                '                }\n',
                '\n',
                '                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");\n'
            ]
            
            for idx, code_line in enumerate(new_code):
                lines.insert(start_idx + idx, code_line)
            
            break

# 写回文件
with open(r'c:\Users\Barry\source\repos\whisper windows\Form1.cs', 'w', encoding='utf-8') as f:
    f.writelines(lines)

print("Fixed API token retrieval - now using TokenManager!")
