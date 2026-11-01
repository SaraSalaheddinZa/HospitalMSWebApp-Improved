using System;
using System.Security.Cryptography;
using System.Text;

namespace HospitalMSWebApp.Infrastructure
{
    public static class PasswordSecurity
    {
        private const int Iterations = 100000;
        private const int SaltLength = 16;
        private const int HashLength = 32;

        public static string Hash(string password)
        {
            if (password == null) throw new ArgumentNullException("password");
            var salt = new byte[SaltLength];
            using (var random = RandomNumberGenerator.Create()) random.GetBytes(salt);
            using (var derive = new Rfc2898DeriveBytes(password, salt, Iterations))
                return "PBKDF2$" + Iterations + "$" + Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(derive.GetBytes(HashLength));
        }

        public static bool Verify(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash)) return false;
            var pieces = storedHash.Split('$');
            if (pieces.Length != 4 || pieces[0] != "PBKDF2") return false;
            int iterations;
            byte[] salt;
            byte[] expected;
            if (!int.TryParse(pieces[1], out iterations) || iterations<1 || iterations>2000000) return false;
            try { salt = Convert.FromBase64String(pieces[2]); expected = Convert.FromBase64String(pieces[3]); }
            catch (FormatException) { return false; }
            if(salt.Length<8||salt.Length>64||expected.Length<16||expected.Length>64)return false;
            using (var derive = new Rfc2898DeriveBytes(password, salt, iterations))
                return FixedTimeEquals(derive.GetBytes(expected.Length), expected);
        }

        /// <summary>
        /// Valid only on the legacy-table migration route. Raw values are compared in constant
        /// time, then replaced with PBKDF2; they are never written by the application.
        /// </summary>
        public static bool VerifyLegacy(string password, string storedValue)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedValue)) return false;
            if (storedValue.StartsWith("PBKDF2$",StringComparison.Ordinal)) return Verify(password, storedValue);
            using (var sha = SHA256.Create())
            {
                var legacyHash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(password))).Replace("-", "").ToLowerInvariant();
                if (FixedTimeEquals(Encoding.UTF8.GetBytes(legacyHash), Encoding.UTF8.GetBytes(storedValue.ToLowerInvariant()))) return true;
            }
            return FixedTimeEquals(Encoding.UTF8.GetBytes(password), Encoding.UTF8.GetBytes(storedValue));
        }

        public static bool NeedsUpgrade(string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash)) return true;
            var parts = storedHash.Split('$');
            int storedIterations;
            return parts.Length != 4 || parts[0] != "PBKDF2" || !int.TryParse(parts[1], out storedIterations) || storedIterations < Iterations;
        }

        public static bool IsAcceptable(string password, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(password) || password.Length < 10) error = "Use at least 10 characters.";
            else if (!Contains(password, char.IsUpper) || !Contains(password, char.IsLower) || !Contains(password, char.IsDigit)) error = "Use upper-case, lower-case, and numeric characters.";
            return error == null;
        }

        private static bool Contains(string value, Func<char, bool> predicate) { foreach (var character in value) if (predicate(character)) return true; return false; }
        private static bool FixedTimeEquals(byte[] left, byte[] right) { if (left == null || right == null || left.Length != right.Length) return false; var difference = 0; for (var i = 0; i < left.Length; i++) difference |= left[i] ^ right[i]; return difference == 0; }
    }
}
