using System;
using System.Security.Cryptography;
using System.Text;

namespace whisper_windows
{
    /// <summary>
    /// 管理 API Token 的加密存储和检索
    /// 使用 Windows DPAPI (Data Protection API) 进行加密
    /// </summary>
    public static class TokenManager
    {
        /// <summary>
        /// 检查 Token 是否已配置
        /// </summary>
        public static bool IsTokenConfigured()
        {
            try
            {
                string encryptedToken = Properties.Settings.Default.EncryptedToken;
                return !string.IsNullOrEmpty(encryptedToken);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 保存并加密 Token
        /// </summary>
        /// <param name="token">要保存的 API Token</param>
        public static void SaveEncryptedToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException("Token 不能为空", nameof(token));
            }

            try
            {
                // 使用 DPAPI 加密 Token
                byte[] tokenBytes = Encoding.UTF8.GetBytes(token);
                byte[] encryptedBytes = ProtectedData.Protect(
                    tokenBytes,
                    null,
                    DataProtectionScope.CurrentUser);

                // 转换为 Base64 字符串保存
                string encryptedToken = Convert.ToBase64String(encryptedBytes);
                Properties.Settings.Default.EncryptedToken = encryptedToken;
                Properties.Settings.Default.Save();
            }
            catch (CryptographicException ex)
            {
                throw new InvalidOperationException("加密 Token 失败", ex);
            }
        }

        /// <summary>
        /// 获取解密后的 Token
        /// </summary>
        /// <returns>解密后的 API Token，如果未配置则返回 null</returns>
        public static string? GetDecryptedToken()
        {
            try
            {
                string encryptedToken = Properties.Settings.Default.EncryptedToken;
                if (string.IsNullOrEmpty(encryptedToken))
                {
                    return null;
                }

                // 从 Base64 解码
                byte[] encryptedBytes = Convert.FromBase64String(encryptedToken);

                // 使用 DPAPI 解密
                byte[] decryptedBytes = ProtectedData.Unprotect(
                    encryptedBytes,
                    null,
                    DataProtectionScope.CurrentUser);

                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch (CryptographicException)
            {
                // 解密失败，可能是数据损坏或在不同用户账户下访问
                return null;
            }
            catch (FormatException)
            {
                // Base64 格式错误
                return null;
            }
        }

        /// <summary>
        /// 获取部分隐藏的 Token 用于显示
        /// </summary>
        /// <returns>部分隐藏的 Token (sk-proj-***...***)</returns>
        public static string? GetMaskedToken()
        {
            string? token = GetDecryptedToken();
            if (string.IsNullOrEmpty(token))
            {
                return null;
            }

            // OpenAI API Key 格式: sk-proj-xxxxxxxxx...
            if (token.Length <= 20)
            {
                return "***...***";
            }

            // 显示前缀和后缀
            string prefix = token.Substring(0, Math.Min(12, token.Length));
            string suffix = token.Substring(Math.Max(0, token.Length - 4));
            return $"{prefix}***...***{suffix}";
        }

        /// <summary>
        /// 删除已保存的 Token
        /// </summary>
        public static void ClearToken()
        {
            Properties.Settings.Default.EncryptedToken = string.Empty;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// 验证 Token 格式是否正确
        /// </summary>
        /// <param name="token">要验证的 Token</param>
        /// <returns>格式是否有效</returns>
        public static bool IsValidTokenFormat(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            // OpenAI API Key 通常以 sk- 开头
            return token.StartsWith("sk-") && token.Length > 20;
        }
    }
}
