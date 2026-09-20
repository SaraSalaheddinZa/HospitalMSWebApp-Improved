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
            {
                return "PBKDF2$" + Iterations + "$" + Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(derive.GetBytes(HashLength));
            }
        }

        public static bool Verify(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash)) return false;
            var pieces = storedHash.Split('$');
            if (pieces.Length == 4 && pieces[0] == "PBKDF2")
            {
                int iterations;
                byte[] salt;
                byte[] expected;
                if (!int.TryParse(pieces[1], out iterations)) return false;
                try { salt = Convert.FromBase64String(pieces[2]); expected = Convert.FromBase64String(pieces[3]); }
                catch (FormatException) { return false; }
                using (var derive = new Rfc2898DeriveBytes(password, salt, iterations))
                    return FixedTimeEquals(derive.GetBytes(expected.Length), expected);
            }

            // Supports the application's historical SHA-256 records during a transparent upgrade.
            using (var sha = SHA256.Create())
            {
                var legacy = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(password))).Replace("-", "").ToLowerInvariant();
                return FixedTimeEquals(Encoding.UTF8.GetBytes(legacy), Encoding.UTF8.GetBytes(storedHash.ToLowerInvariant()));
            }
        }

        public static bool NeedsUpgrade(string storedHash)
        {
            return string.IsNullOrEmpty(storedHash) || !storedHash.StartsWith("PBKDF2$", StringComparison.Ordinal);
        }

        public static bool IsAcceptable(string password, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(password) || password.Length < 10) error = "Use at least 10 characters.";
            else if (!Contains(password, char.IsUpper) || !Contains(password, char.IsLower) || !Contains(password, char.IsDigit))
                error = "Use upper-case, lower-case, and numeric characters.";
            return error == null;
        }

        private static bool Contains(string value, Func<char, bool> predicate)
        {
            foreach (var character in value) if (predicate(character)) return true;
            return false;
        }

        private static bool FixedTimeEquals(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length) return false;
            var difference = 0;
            for (var i = 0; i < left.Length; i++) difference |= left[i] ^ right[i];
            return difference == 0;
        }
    }
}
