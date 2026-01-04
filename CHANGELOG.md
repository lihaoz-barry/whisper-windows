# Changelog - 更新日志

## 版本号说明 | Version Numbering
所有版本遵循 [Semantic Versioning](https://semver.org) 规范。
All versions follow [Semantic Versioning](https://semver.org) specification.

---

## [0.2.0] - 2026-01-04

### 中文 (Chinese)

#### 新增功能 (New Features)
- ✨ **安全的 API Token 管理** - 新增设置窗口，允许用户安全地存储和管理个人 OpenAI API Token
  - 使用 Windows DPAPI 加密存储 Token，确保安全性
  - Token 存储在应用程序设置中，不再硬编码在源代码中
  - 支持 Token 验证和掩码显示，防止意外泄露

- 🔐 **增强安全性** - 移除了源代码中的硬编码 API Key
  - Token 现在通过加密方式存储在本地应用设置中
  - 每个用户的 Token 独立管理，互不影响

- 🎯 **托盘最小化功能** - 应用可以最小化到系统托盘
  - 双击托盘图标显示/隐藏窗口
  - 方便用户在后台持续使用 Ctrl+M 快捷键进行语音转文字

- 📝 **设置窗口增强** - 新增完整的设置界面
  - Token 管理：添加、修改、删除 API Key
  - Token 验证：在保存前测试 Token 是否有效
  - 用户友好的界面提示

#### 改进 (Improvements)
- 🔄 重构了 Token 管理逻辑，提高代码可维护性
- 📱 改进了 UI 响应性，使用托盘减少了窗口占用的屏幕空间
- 🛡️ 增强了应用程序的安全性，符合最佳实践

#### 修复 (Bug Fixes)
- 修复了编译依赖问题，移除了未使用的 NuGet 包引用
- 改进了空值处理，增加了代码的稳定性

#### 技术细节 (Technical Details)
- 新增 `TokenManager.cs` 类，专门处理 Token 的加密和解密
- 新增 `SettingsForm.cs` 设置窗体
- 更新了 `Form1.cs` 以支持托盘功能和 Token 管理集成

---

### English

#### New Features
- ✨ **Secure API Token Management** - New settings window allows users to safely store and manage personal OpenAI API tokens
  - Encrypts tokens using Windows DPAPI for enhanced security
  - Tokens are stored in application settings, no longer hardcoded in source
  - Supports token validation and masked display to prevent accidental leaks

- 🔐 **Enhanced Security** - Removed hardcoded API keys from source code
  - Tokens are now encrypted and stored in local application settings
  - Each user's token is managed independently

- 🎯 **System Tray Minimization** - Application can be minimized to system tray
  - Double-click tray icon to show/hide window
  - Convenient for users to continue using Ctrl+M hotkey in the background for speech-to-text

- 📝 **Settings Window Enhancement** - New complete settings interface
  - Token Management: Add, modify, delete API keys
  - Token Validation: Test token validity before saving
  - User-friendly interface with helpful prompts

#### Improvements
- 🔄 Refactored token management logic for better code maintainability
- 📱 Improved UI responsiveness with tray functionality reducing screen space usage
- 🛡️ Enhanced application security following best practices

#### Bug Fixes
- Fixed compilation dependency issues, removed unused NuGet package references
- Improved null handling, increased code stability

#### Technical Details
- Added `TokenManager.cs` class for handling token encryption and decryption
- Added `SettingsForm.cs` settings form
- Updated `Form1.cs` to support tray functionality and token management integration

---

## [0.1.0] - 2025-11-23

### 中文 (Chinese)
初始版本，包含以下核心功能：
- 全局热键支持 (Ctrl+M)
- 基于 NAudio 的音频录制
- OpenAI Whisper API 集成
- 自动复制到剪贴板
- 系统通知提示
- 音效反馈

### English
Initial release with core features:
- Global hotkey support (Ctrl+M)
- Audio recording via NAudio
- OpenAI Whisper API integration
- Auto-copy to clipboard
- System notifications
- Sound feedback

---

## 版本更新历史 | Version History

| 版本 | Date | 说明 |
|------|------|------|
| 0.2.0 | 2026-01-04 | API Token 管理 & 托盘最小化 |
| 0.1.0 | 2025-11-23 | 初始版本 |

---

## 如何升级 | How to Upgrade

1. 下载最新版本 | Download the latest version
2. 备份现有的应用设置 | Backup existing application settings
3. 关闭当前应用 | Close the current application
4. 替换 exe 文件 | Replace the executable
5. 运行新版本 | Run the new version

**重要**: 您的现有 Token 设置将被保留 | **Important**: Your existing token settings will be preserved

---

## 反馈和问题 | Feedback & Issues

如有问题或建议，欢迎提出 Issue！
For questions or suggestions, please submit an Issue!

GitHub Issues: [whisper-windows/issues](https://github.com/lihaoz-barry/whisper-windows/issues)
