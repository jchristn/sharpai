namespace SharpAI.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// String collection helpers.
    /// </summary>
    public static class StringHelpers
    {
        /// <summary>
        /// Combine two string lists.
        /// </summary>
        /// <param name="list1">String list 1.</param>
        /// <param name="list2">String list 2.</param>
        /// <returns>String list.</returns>
        public static List<string> Combine(List<string> list1, List<string> list2)
        {
            if (list1 == null) return list2;
            if (list2 == null) return list1;
            List<string> ret = new List<string>();
            ret.AddRange(list1);
            ret.AddRange(list2);
            return ret;
        }

        /// <summary>
        /// Redact a string.
        /// </summary>
        /// <param name="str">String.</param>
        /// <param name="replacementChar">Replacement character.</param>
        /// <param name="charsToRetain">Number of characters to retain.</param>
        /// <returns>Redacted string.</returns>
        public static string RedactTail(string str, string replacementChar = "*", int charsToRetain = 4)
        {
            if (String.IsNullOrEmpty(str)) return str;
            if (String.IsNullOrEmpty(replacementChar)) throw new ArgumentNullException(nameof(replacementChar));
            if (charsToRetain < 0) throw new ArgumentOutOfRangeException(nameof(charsToRetain));

            if (str.Length < charsToRetain)
            {
                return new string(replacementChar[0], str.Length);
            }
            else
            {
                return str.Substring(0, charsToRetain) + new string(replacementChar[0], str.Length - charsToRetain);
            }
        }

        /// <summary>
        /// Convert a CSV string list of GUIDs to a List of Guid.
        /// </summary>
        /// <param name="str">Input string.</param>
        /// <returns>List of GUIDs.</returns>
        public static List<Guid> StringToGuidList(string str)
        {
            if (String.IsNullOrEmpty(str)) return null;
            string[] parts = str.Split(',');

            List<Guid> ret = new List<Guid>();

            foreach (string curr in parts)
            {
                if (Guid.TryParse(curr, out Guid guid)) ret.Add(guid);
            }

            return ret;
        }

        /// <summary>
        /// Tests if a string is a valid base64 encoded string.
        /// </summary>
        /// <param name="input">String.</param>
        /// <returns>True if valid.</returns>
        public static bool IsValidBase64(string input)
        {
            if (string.IsNullOrEmpty(input)) return false;
            input = input.Replace(" ", "").Replace("\t", "").Replace("\r", "").Replace("\n", "");
            if (input.Length % 4 != 0) return false;
            foreach (char c in input)
            {
                if (!IsValidBase64Character(c)) return false;
            }

            try
            {
                Convert.FromBase64String(input);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        /// <summary>
        /// Tests if a string is a valid hexadecimal string.
        /// </summary>
        /// <param name="input">String.</param>
        /// <returns>True if valid.</returns>
        public static bool IsValidHex(string input)
        {
            if (string.IsNullOrEmpty(input)) return false;
            input = input.Replace(" ", "").Replace("\t", "").Replace("\r", "").Replace("\n", "");

            if (input.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ||
                input.StartsWith("0X", StringComparison.OrdinalIgnoreCase))
            {
                input = input.Substring(2);
            }
            else if (input.StartsWith("#"))
            {
                input = input.Substring(1);
            }

            if (string.IsNullOrEmpty(input)) return false;

            foreach (char c in input)
            {
                if (!IsValidHexCharacter(c))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Tests if a string is a valid hexadecimal string with strict length requirement.
        /// </summary>
        /// <param name="input">String.</param>
        /// <param name="requireEvenLength">If true, requires even number of hex digits (for byte representation).</param>
        /// <returns>True if valid.</returns>
        public static bool IsValidHex(string input, bool requireEvenLength)
        {
            if (!IsValidHex(input)) return false;

            if (requireEvenLength)
            {
                string cleanInput = input.Replace(" ", "").Replace("\t", "").Replace("\r", "").Replace("\n", "");

                if (cleanInput.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ||
                    cleanInput.StartsWith("0X", StringComparison.OrdinalIgnoreCase))
                {
                    cleanInput = cleanInput.Substring(2);
                }
                else if (cleanInput.StartsWith("#"))
                {
                    cleanInput = cleanInput.Substring(1);
                }

                return cleanInput.Length % 2 == 0;
            }

            return true;
        }

        /// <summary>
        /// Tests if a string is a reasonable email address format
        /// </summary>
        /// <param name="input">The string to test</param>
        /// <returns>True if the string appears to be a valid email format, false otherwise</returns>
        public static bool IsValidEmail(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            input = input.Trim();
            int atCount = input.Count(c => c == '@');
            if (atCount != 1) return false;

            string[] parts = input.Split('@');
            string localPart = parts[0];
            string domainPart = parts[1];

            if (!IsValidLocalPart(localPart)) return false;
            if (!IsValidDomainPart(domainPart)) return false;
            return true;
        }

        /// <summary>
        /// Validates the local part (before @) of an email address
        /// </summary>
        /// <param name="localPart">The local part to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        private static bool IsValidLocalPart(string localPart)
        {
            if (string.IsNullOrEmpty(localPart)) return false;
            if (localPart.Length > 64) return false;
            if (localPart.StartsWith(".") || localPart.EndsWith(".")) return false;
            if (localPart.Contains("..")) return false;
            foreach (char c in localPart)
                if (!IsValidLocalPartCharacter(c)) return false;

            return true;
        }

        /// <summary>
        /// Validates the domain part (after @) of an email address
        /// </summary>
        /// <param name="domainPart">The domain part to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        private static bool IsValidDomainPart(string domainPart)
        {
            if (string.IsNullOrEmpty(domainPart)) return false;
            if (domainPart.Length > 255) return false;
            if (domainPart.StartsWith(".") || domainPart.EndsWith(".") ||
                domainPart.StartsWith("-") || domainPart.EndsWith("-"))
                return false;

            if (domainPart.Contains("..")) return false;
            if (!domainPart.Contains(".")) return IsValidDomainLabel(domainPart);

            string[] labels = domainPart.Split('.');

            foreach (string label in labels)
            {
                if (!IsValidDomainLabel(label)) return false;
            }

            string tld = labels[labels.Length - 1];
            if (tld.Length < 1) return false;

            return true;
        }

        private static bool IsValidDomainLabel(string label)
        {
            if (string.IsNullOrEmpty(label)) return false;
            if (label.Length > 63) return false;
            if (label.StartsWith("-") || label.EndsWith("-")) return false;

            foreach (char c in label)
            {
                if (!IsValidDomainCharacter(c)) return false;
            }

            return true;
        }

        private static bool IsValidLocalPartCharacter(char c)
        {
            return char.IsLetterOrDigit(c) ||
                   c == '.' || c == '-' || c == '_' || c == '+' || c == '=' ||
                   c == '!' || c == '#' || c == '$' || c == '%' || c == '&' ||
                   c == '\'' || c == '*' || c == '/' || c == '?' || c == '^' ||
                   c == '`' || c == '{' || c == '|' || c == '}' || c == '~';
        }

        private static bool IsValidDomainCharacter(char c)
        {
            return char.IsLetterOrDigit(c) || c == '-' || c == '.';
        }

        private static bool IsValidBase64Character(char c)
        {
            return (c >= 'A' && c <= 'Z') ||
                   (c >= 'a' && c <= 'z') ||
                   (c >= '0' && c <= '9') ||
                   c == '+' || c == '/' || c == '=';
        }

        private static bool IsValidHexCharacter(char c)
        {
            return (c >= '0' && c <= '9') ||
                   (c >= 'A' && c <= 'F') ||
                   (c >= 'a' && c <= 'f');
        }
    }
}
