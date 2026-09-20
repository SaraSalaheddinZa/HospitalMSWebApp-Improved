using System;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;

namespace HospitalMSWebApp.Infrastructure
{
    public sealed class AuthenticatedUser
    {
        public int UserId { get; set; }
        public int? PatientId { get; set; }
        public string Role { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
    }

    public static class AuthenticationService
    {
        public static AuthenticatedUser Authenticate(string email, string password, string requestedRole)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password) || !IsKnownRole(requestedRole)) return null;
            var database = new Database();
            DataTable account;
            try
            {
                account = database.Query(@"SELECT TOP (1) UserId, PatientId, RoleName, DisplayName, Email, PasswordHash
                                           FROM gh.UserAccounts
                                           WHERE NormalizedEmail = @Email AND RoleName = @Role AND IsActive = 1
                                           AND (LockedUntil IS NULL OR LockedUntil <= SYSUTCDATETIME())",
                    Database.Parameter("@Email", email.Trim().ToUpperInvariant()),
                    Database.Parameter("@Role", requestedRole));
            }
            catch (SqlException)
            {
                // Existing installations continue through the old staff tables until the migration is deployed.
                account = new DataTable();
            }

            if (account.Rows.Count == 1)
            {
                var row = account.Rows[0];
                var stored = Convert.ToString(row["PasswordHash"]);
                if (!PasswordSecurity.Verify(password, stored))
                {
                    RegisterFailedAttempt(database, Convert.ToInt32(row["UserId"]));
                    return null;
                }

                var user = ToUser(row);
                database.Execute(@"UPDATE gh.UserAccounts SET FailedLoginCount = 0, LockedUntil = NULL,
                                  LastLoginAt = SYSUTCDATETIME(), PasswordHash = CASE WHEN @Upgrade = 1 THEN @Hash ELSE PasswordHash END
                                  WHERE UserId = @UserId",
                    Database.Parameter("@Upgrade", PasswordSecurity.NeedsUpgrade(stored) ? 1 : 0),
                    Database.Parameter("@Hash", PasswordSecurity.NeedsUpgrade(stored) ? PasswordSecurity.Hash(password) : stored),
                    Database.Parameter("@UserId", user.UserId));
                return user;
            }

            return AuthenticateLegacy(database, email.Trim(), password, requestedRole);
        }

        public static void SignIn(HttpContext context, AuthenticatedUser user, bool persistent)
        {
            var manager = new SessionIDManager();
            var newId = manager.CreateSessionID(context);
            bool redirected;
            bool cookieAdded;
            manager.SaveSessionID(context, newId, out redirected, out cookieAdded);

            context.Session.Clear();
            context.Session["uid"] = user.UserId;
            context.Session["role"] = user.Role;
            context.Session["userName"] = user.Name;
            context.Session["userEmail"] = user.Email;
            context.Session["patientId"] = user.PatientId;
            context.Session.Timeout = 30;

            var data = user.Role + "|" + user.UserId + "|" + (user.PatientId.HasValue ? user.PatientId.Value.ToString() : "");
            var ticket = new FormsAuthenticationTicket(1, user.UserId.ToString(), DateTime.Now,
                DateTime.Now.AddMinutes(persistent ? 480 : 30), persistent, data, FormsAuthentication.FormsCookiePath);
            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, FormsAuthentication.Encrypt(ticket))
            {
                HttpOnly = true,
                Secure = context.Request.IsSecureConnection,
                SameSite = SameSiteMode.Lax
            };
            if (persistent) cookie.Expires = ticket.Expiration;
            context.Response.Cookies.Add(cookie);
        }

        public static void SignOut(HttpContext context)
        {
            FormsAuthentication.SignOut();
            context.Session.Clear();
            context.Session.Abandon();
            var expired = new HttpCookie(FormsAuthentication.FormsCookieName, "")
            {
                Expires = DateTime.UtcNow.AddDays(-1), HttpOnly = true, Secure = context.Request.IsSecureConnection,
                SameSite = SameSiteMode.Lax
            };
            context.Response.Cookies.Add(expired);
        }

        private static AuthenticatedUser AuthenticateLegacy(Database database, string email, string password, string role)
        {
            var map = LegacyMap.For(role);
            if (map == null) return null;
            var table = database.Query(map.Query, Database.Parameter("@Email", email));
            if (table.Rows.Count != 1) return null;
            var row = table.Rows[0];
            var stored = Convert.ToString(row["PasswordHash"]);
            if (!PasswordSecurity.Verify(password, stored)) return null;

            if (PasswordSecurity.NeedsUpgrade(stored))
            {
                database.Execute(map.UpdatePassword, Database.Parameter("@PasswordHash", PasswordSecurity.Hash(password)),
                    Database.Parameter("@UserId", Convert.ToInt32(row["UserId"])));
            }
            return new AuthenticatedUser
            {
                UserId = Convert.ToInt32(row["UserId"]), Role = role,
                Name = Convert.ToString(row["DisplayName"]), Email = Convert.ToString(row["Email"])
            };
        }

        private static void RegisterFailedAttempt(Database database, int userId)
        {
            database.Execute(@"UPDATE gh.UserAccounts SET FailedLoginCount = FailedLoginCount + 1,
                              LockedUntil = CASE WHEN FailedLoginCount + 1 >= 5 THEN DATEADD(MINUTE, 15, SYSUTCDATETIME()) ELSE LockedUntil END
                              WHERE UserId = @UserId", Database.Parameter("@UserId", userId));
        }

        private static AuthenticatedUser ToUser(DataRow row)
        {
            return new AuthenticatedUser
            {
                UserId = Convert.ToInt32(row["UserId"]),
                PatientId = row["PatientId"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["PatientId"]),
                Role = Convert.ToString(row["RoleName"]), Name = Convert.ToString(row["DisplayName"]), Email = Convert.ToString(row["Email"])
            };
        }

        private static bool IsKnownRole(string role)
        {
            return role == "Admin" || role == "Doctor" || role == "Receptionist" || role == "Laboratorian" ||
                   role == "Nurse" || role == "Pharmacist" || role == "Patient";
        }

        private sealed class LegacyMap
        {
            public string Query { get; private set; }
            public string UpdatePassword { get; private set; }
            public static LegacyMap For(string role)
            {
                switch (role)
                {
                    case "Doctor": return Create("doctorTbl", "doctorID", "doctorName", "doctorEmail", "doctorPassword", "doctorStatus");
                    case "Receptionist": return Create("receptionistTbl", "receptionistID", "receptionistName", "receptionistEmail", "receptionistPassword", "receptionistStatus");
                    case "Laboratorian": return Create("laboratorianTbl", "labID", "labName", "labEmail", "labPassword", "labStatus");
                    default: return null;
                }
            }
            private static LegacyMap Create(string table, string id, string name, string email, string password, string status)
            {
                return new LegacyMap
                {
                    Query = "SELECT " + id + " AS UserId, " + name + " AS DisplayName, " + email + " AS Email, " + password +
                            " AS PasswordHash FROM " + table + " WHERE " + email + " = @Email AND " + status + " = 'Active'",
                    UpdatePassword = "UPDATE " + table + " SET " + password + " = @PasswordHash WHERE " + id + " = @UserId"
                };
            }
        }
    }
}
