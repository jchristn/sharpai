namespace SharpAI.Security
{
    using System;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// Hashes and verifies passwords and secrets using SHA-256. Verification is constant-time. Plaintext
    /// is never stored; only the hex digest is persisted.
    /// </summary>
    public static class PasswordHasher
    {
        /// <summary>
        /// Compute the lowercase SHA-256 hex digest of the input.
        /// </summary>
        /// <param name="plaintext">Input; null is treated as empty.</param>
        /// <returns>Hex digest.</returns>
        public static string Hash(string plaintext)
        {
            if (plaintext == null) plaintext = String.Empty;
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(plaintext));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        /// <summary>
        /// Verify a plaintext against a stored hex digest in constant time.
        /// </summary>
        /// <param name="plaintext">Candidate plaintext.</param>
        /// <param name="hashHex">Stored hex digest.</param>
        /// <returns>True if the plaintext matches.</returns>
        public static bool Verify(string plaintext, string hashHex)
        {
            return AuthEvaluator.SecureEquals(Hash(plaintext), hashHex);
        }
    }
}
