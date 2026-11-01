using System;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace HospitalMSWebApp.Infrastructure
{
    public sealed class PasswordRecoveryRequest
    {
        public string Name { get; internal set; }
        public string Email { get; internal set; }
        // Raw token is returned only to the server mail sender, never persisted or logged.
        public string Token { get; internal set; }
    }

    public sealed class PasswordRecoveryService
    {
        private readonly Database database = new Database();
        private const string InvalidLink = "This reset link is invalid or expired. Request a new link.";

        public static void ReportFailure(string stage, Exception error)
        {
            // Exception messages, inner exceptions and Data may contain submitted values or
            // mail-server responses. Recovery diagnostics deliberately record metadata only.
            string operation = stage == "request" ? "request" : stage == "delivery" ? "delivery" : stage == "redemption" ? "redemption" : "operation";
            System.Diagnostics.Trace.TraceError("Password recovery {0} failed ({1}).", operation,
                error == null ? "Unknown" : error.GetType().FullName);
        }

        public PasswordRecoveryRequest RequestReset(string email, string role, string ip)
        {
            email = (email ?? "").Trim();
            if (email.Length == 0 || email.Length > 256 ||
                Array.IndexOf(new[] { "Admin", "Doctor", "Receptionist", "Laboratorian", "Nurse", "Pharmacist", "Patient" }, role) < 0)
                return null;
            return database.InTransaction((connection, transaction) =>
            {
                int userId, version; string name, destination;
                // Both issuance and redemption lock account before tokens: one consistent lock order.
                using (var command = new SqlCommand(@"SELECT UserId,AuthVersion,DisplayName,Email
                    FROM gh.UserAccounts WITH(UPDLOCK,HOLDLOCK)
                    WHERE NormalizedEmail=@Email AND RoleName=@Role AND IsActive=1", connection, transaction))
                {
                    command.Parameters.AddRange(new[] { Database.Parameter("@Email", email.ToUpperInvariant()), Database.Parameter("@Role", role) });
                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.Read()) return null;
                        userId = reader.GetInt32(0); version = reader.GetInt32(1);
                        name = reader.GetString(2); destination = reader.GetString(3);
                    }
                }
                var bytes = new byte[32];
                using (var random = RandomNumberGenerator.Create()) random.GetBytes(bytes);
                string token = Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
                using (var command = new SqlCommand(@"UPDATE gh.PasswordResetTokens SET UsedAt=SYSUTCDATETIME() WHERE UserId=@User AND UsedAt IS NULL;
                    INSERT INTO gh.PasswordResetTokens(UserId,TokenHash,ExpiresAt,RequestedIp,IssuedAuthVersion)
                    VALUES(@User,@Hash,DATEADD(minute,30,SYSUTCDATETIME()),@Ip,@Version);
                    INSERT INTO gh.AuditEvents(ActionName,EntityType,EntityId,Detail,IpAddress)
                    VALUES('PasswordResetRequested','UserAccount',CONVERT(nvarchar(80),@User),'Recovery link issued',@Ip);", connection, transaction))
                {
                    command.Parameters.AddRange(new[] { Database.Parameter("@User", userId), Database.Parameter("@Hash", TokenHash(token)),
                        Database.Parameter("@Ip", SafeIp(ip)), Database.Parameter("@Version", version) });
                    command.ExecuteNonQuery();
                }
                return new PasswordRecoveryRequest { Name = name, Email = destination, Token = token };
            });
        }

        public int CompleteReset(string token, string password, string ip)
        {
            if (token == null || !Regex.IsMatch(token, @"\A[A-Za-z0-9_-]{43}\z")) throw new ArgumentException(InvalidLink);
            string error;
            if (!PasswordSecurity.IsAcceptable(password, out error)) throw new ArgumentException(error);
            byte[] tokenHash = TokenHash(token);
            // Untrusted lookup locates the account only. Authorization is rechecked under locks below.
            var accountId = database.Scalar("SELECT TOP(1) UserId FROM gh.PasswordResetTokens WHERE TokenHash=@Hash", Database.Parameter("@Hash", tokenHash));
            if (accountId == null) throw new ArgumentException(InvalidLink);
            int userId = Convert.ToInt32(accountId);
            string passwordHash = PasswordSecurity.Hash(password);
            return database.InTransaction((connection, transaction) =>
            {
                object version;
                using (var account = new SqlCommand("SELECT AuthVersion FROM gh.UserAccounts WITH(UPDLOCK,HOLDLOCK) WHERE UserId=@User AND IsActive=1", connection, transaction))
                {
                    account.Parameters.AddWithValue("@User", userId); version = account.ExecuteScalar();
                }
                if (version == null) throw new ArgumentException(InvalidLink);
                using (var check = new SqlCommand(@"SELECT PasswordResetTokenId FROM gh.PasswordResetTokens WITH(UPDLOCK,HOLDLOCK)
                    WHERE UserId=@User AND TokenHash=@Hash AND IssuedAuthVersion=@Version
                    AND UsedAt IS NULL AND ExpiresAt>SYSUTCDATETIME()", connection, transaction))
                {
                    check.Parameters.AddRange(new[] { Database.Parameter("@User", userId), Database.Parameter("@Hash", tokenHash), Database.Parameter("@Version", version) });
                    if (check.ExecuteScalar() == null) throw new ArgumentException(InvalidLink);
                }
                using (var command = new SqlCommand(@"UPDATE gh.UserAccounts SET PasswordHash=@Password,
                    PasswordChangedAt=SYSUTCDATETIME(),AuthVersion=AuthVersion+1 WHERE UserId=@User;
                    UPDATE gh.PasswordResetTokens SET UsedAt=SYSUTCDATETIME() WHERE UserId=@User AND UsedAt IS NULL;
                    INSERT INTO gh.AuditEvents(ActionName,EntityType,EntityId,Detail,IpAddress)
                    VALUES('PasswordReset','UserAccount',CONVERT(nvarchar(80),@User),'One-use recovery link redeemed; prior sessions invalidated',@Ip);
                    INSERT INTO gh.Notifications(UserId,NotificationType,Subject,Message)
                    VALUES(@User,'AccountSecurity','Your password was changed','Your password was changed through account recovery. If this was not you, contact your hospital administrator.');", connection, transaction))
                {
                    command.Parameters.AddRange(new[] { Database.Parameter("@User", userId), Database.Parameter("@Password", passwordHash), Database.Parameter("@Ip", SafeIp(ip)) });
                    command.ExecuteNonQuery();
                }
                return userId;
            });
        }

        private static string SafeIp(string ip) { return ip == null ? null : ip.Substring(0, Math.Min(64, ip.Length)); }
        private static byte[] TokenHash(string token)
        {
            using (var hash = SHA256.Create()) return hash.ComputeHash(Encoding.UTF8.GetBytes(token));
        }
    }
}
