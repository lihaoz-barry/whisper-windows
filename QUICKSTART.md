# 快速开始指南

## 方法一：使用 VSCode 任务（推荐）

### 1. 构建项目
- 按 `Ctrl+Shift+B` 快速构建
- 或按 `Ctrl+Shift+P` → 选择 "Tasks: Run Task" → 选择 "Build"

### 2. 发布单文件可执行程序
- 按 `Ctrl+Shift+P` → 选择 "Tasks: Run Task" → 选择 "Publish"
- 发布完成后，可执行文件位于：
  ```
  bin\Release\net6.0-windows\win-x64\publish\whisper windows.exe
  ```

### 3. 运行应用
- 按 `Ctrl+Shift+P` → 选择 "Tasks: Run Task" → 选择 "Run"

### 4. 清理构建
- 按 `Ctrl+Shift+P` → 选择 "Tasks: Run Task" → 选择 "Clean"

## 方法二：使用工作流命令

### 1. 构建
直接输入命令：`/build`

### 2. 发布
直接输入命令：`/publish`

### 3. 清理
直接输入命令：`/clean`

## 方法三：使用命令行

### 构建
```powershell
cd "c:\Users\Barry\source\repos\whisper windows"
dotnet build
```

### 发布
```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

### 运行
```powershell
dotnet run
```

## 使用应用

1. 启动 `whisper windows.exe`
2. 按 `Ctrl+M` 开始/停止录音
3. 转录结果自动复制到剪贴板

## 注意事项

⚠️ **第一次运行前请修改 API Key**

编辑 `Form1.cs` 第 259 行，替换为您的 OpenAI API Key：

```csharp
client.DefaultRequestHeaders.Add("Authorization", "Bearer YOUR_API_KEY_HERE");
```

## 需要帮助？

查看完整文档：[README.md](file:///c:/Users/Barry/source/repos/whisper%20windows/README.md)
