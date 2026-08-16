namespace SharpAI.Security
{
    using System;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// Encrypts and decrypts opaque bearer tokens that reference a server-side session. Tokens carry only
    /// the internal session identifier, encrypted with AES-256-CBC using a fresh random IV per token (the
    /// IV is prepended to the ciphertext). The AES key is derived from the configured key material.
    /// </summary>
    public class SessionTokenService
    {
        #region Private-Members

        private const int _IvLength = 16;
        private readonly byte[] _Key;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate with key material. The 256-bit AES key is derived as SHA-256 of the material.
        /// </summary>
        /// <param name="keyMaterial">Key material. May not be null or empty.</param>
        public SessionTokenService(string keyMaterial)
        {
            if (String.IsNullOrEmpty(keyMaterial)) throw new ArgumentNullException(nameof(keyMaterial));
            _Key = SHA256.HashData(Encoding.UTF8.GetBytes(keyMaterial));
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Encrypt a session identifier into an opaque base64 token (random IV prepended).
        /// </summary>
        /// <param name="sessionId">Session identifier. May not be null or empty.</param>
        /// <returns>Opaque token.</returns>
        public string Encrypt(string sessionId)
        {
            if (String.IsNullOrEmpty(sessionId)) throw new ArgumentNullException(nameof(sessionId));

            using (Aes aes = Aes.Create())
            {
                aes.Key = _Key;
                aes.GenerateIV();
                byte[] iv = aes.IV;

                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                {
                    byte[] plain = Encoding.UTF8.GetBytes(sessionId);
                    byte[] cipher = encryptor.TransformFinalBlock(plain, 0, plain.Length);

                    byte[] result = new byte[iv.Length + cipher.Length];
                    Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
                    Buffer.BlockCopy(cipher, 0, result, iv.Length, cipher.Length);
                    return Convert.ToBase64String(result);
                }
            }
        }

        /// <summary>
        /// Decrypt an opaque token back into its session identifier, or null when the token is invalid.
        /// </summary>
        /// <param name="token">Opaque token.</param>
        /// <returns>Session identifier, or null.</returns>
        public string Decrypt(string token)
        {
            if (String.IsNullOrEmpty(token)) return null;

            try
            {
                byte[] data = Convert.FromBase64String(token);
                if (data.Length <= _IvLength) return null;

                byte[] iv = new byte[_IvLength];
                Buffer.BlockCopy(data, 0, iv, 0, _IvLength);

                using (Aes aes = Aes.Create())
                {
                    aes.Key = _Key;
                    aes.IV = iv;

                    using (ICryptoTransform decryptor = aes.CreateDecryptor())
                    {
                        byte[] plain = decryptor.TransformFinalBlock(data, _IvLength, data.Length - _IvLength);
                        return Encoding.UTF8.GetString(plain);
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}
