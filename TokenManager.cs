using System;
using System.Security.Cryptography;
using System.Text;

namespace whisper_windows
{
    /// <summary>
    /// Manages encrypted storage and retrieval of API Token
    /// Uses Windows DPAPI (Data Protection API) for encryption
    /// </summary>
    public static class TokenManager
    {
        /// <summary>
        /// Checks if Token is configured
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
        /// Saves and encrypts Token
        /// </summary>
        /// <param name="token">API Token to save</param>
        public static void SaveEncryptedToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException("Token cannot be empty", nameof(token));
            }

            try
            {
                // Use DPAPI to encrypt Token
                byte[] tokenBytes = Encoding.UTF8.GetBytes(token);
                byte[] encryptedBytes = ProtectedData.Protect(
                    tokenBytes,
                    null,
                    DataProtectionScope.CurrentUser);

                // Convert to Base64 string for storage
                string encryptedToken = Convert.ToBase64String(encryptedBytes);
                Properties.Settings.Default.EncryptedToken = encryptedToken;
                Properties.Settings.Default.Save();
            }
            catch (CryptographicException ex)
            {
                throw new InvalidOperationException("Failed to encrypt Token", ex);
            }
        }

        /// <summary>
        /// Gets the decrypted Token
        /// </summary>
        /// <returns>Decrypted API Token, or null if not configured</returns>
        public static string? GetDecryptedToken()
        {
            try
            {
                string encryptedToken = Properties.Settings.Default.EncryptedToken;
                if (string.IsNullOrEmpty(encryptedToken))
                {
                    return null;
                }

                // Decode from Base64
                byte[] encryptedBytes = Convert.FromBase64String(encryptedToken);

                // Use DPAPI to decrypt
                byte[] decryptedBytes = ProtectedData.Unprotect(
                    encryptedBytes,
                    null,
                    DataProtectionScope.CurrentUser);

                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch (CryptographicException)
            {
                // Decryption failed, possibly corrupted data or accessed from different user account
                return null;
            }
            catch (FormatException)
            {
                // Base64 format error
                return null;
            }
        }

        /// <summary>
        /// Gets a partially masked Token for display
        /// </summary>
        /// <returns>Partially masked Token (sk-proj-***...***)</returns>
        public static string? GetMaskedToken()
        {
            string? token = GetDecryptedToken();
            if (string.IsNullOrEmpty(token))
            {
                return null;
            }

            // OpenAI API Key format: sk-proj-xxxxxxxxx...
            if (token.Length <= 20)
            {
                return "***...***";
            }

            // Show prefix and suffix
            string prefix = token.Substring(0, Math.Min(12, token.Length));
            string suffix = token.Substring(Math.Max(0, token.Length - 4));
            return $"{prefix}***...***{suffix}";
        }

        /// <summary>
        /// Deletes the saved Token
        /// </summary>
        public static void ClearToken()
        {
            Properties.Settings.Default.EncryptedToken = string.Empty;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// Validates Token format
        /// </summary>
        /// <param name="token">Token to validate</param>
        /// <returns>Whether format is valid</returns>
        public static bool IsValidTokenFormat(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            // OpenAI API Key usually starts with sk-
            return token.StartsWith("sk-") && token.Length > 20;
        }
    }
}
