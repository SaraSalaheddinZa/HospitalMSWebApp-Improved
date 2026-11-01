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
        public int? RoleEntityId { get; set; }
        public int? PatientId { get; set; }
        public int AuthVersion { get; set; }
        public string Role { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        internal string PasswordHash { get; set; }
    }

    public static class AuthenticationService
    {
        private const int MaxFailedAttempts = 5;

        public static AuthenticatedUser Authenticate(string email, string password, string requestedRole)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password) || !IsKnownRole(requestedRole)) return null;
            var database = new Database();
            var user = FindByLogin(database, email, requestedRole);
            if (user != null)
            {
                if (!PasswordSecurity.Verify(password, user.PasswordHash))
                {
                    RegisterFailedAttempt(database, user.UserId);
                    return null;
                }
                return UpdateSuccessfulLogin(database, user, password) ? user : null;
            }
            return AuthenticateLegacy(database, email.Trim(), password, requestedRole);
        }

        public static void SignIn(HttpContext context, AuthenticatedUser user, bool persistent)
        {
            if (context == null || user == null) throw new ArgumentNullException(context == null ? "context" : "user");
            context.Session.Clear();
            var manager = new SessionIDManager();
            var newId = manager.CreateSessionID(context);
            bool redirected;
            bool cookieAdded;
            manager.SaveSessionID(context, newId, out redirected, out cookieAdded);
            SetSession(context, user);

            var userData = string.Join("|", user.Role, user.RoleEntityId.HasValue ? user.RoleEntityId.Value.ToString() : "",
                user.PatientId.HasValue ? user.PatientId.Value.ToString() : "", user.AuthVersion.ToString());
            var issued = DateTime.Now;
            var expiry = issued.AddMinutes(persistent ? 480 : 30);
            var ticket = new FormsAuthenticationTicket(2, user.UserId.ToString(), issued, expiry, persistent, userData, FormsAuthentication.FormsCookiePath);
            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, FormsAuthentication.Encrypt(ticket))
            {
                HttpOnly = true,
                Secure = context.Request.IsSecureConnection,
                SameSite = SameSiteMode.Lax
            };
            if (persistent) cookie.Expires = expiry;
            context.Response.Cookies.Add(cookie);
        }

        public static AuthenticatedUser GetTicketUser(HttpContext context)
        {
            if (context == null || !context.Request.IsAuthenticated || !(context.User.Identity is FormsIdentity)) return null;
            var ticket = ((FormsIdentity)context.User.Identity).Ticket;
            int accountId;
            if (ticket == null || ticket.Expired || !int.TryParse(ticket.Name, out accountId)) return null;
            var values = (ticket.UserData ?? string.Empty).Split('|');
            int version;
            if (values.Length != 4 || !int.TryParse(values[3], out version)) return null;
            try
            {
                var data = new Database().Query(@"SELECT UserId, LegacyUserId, PatientId, RoleName, DisplayName, Email, AuthVersion
                    FROM gh.UserAccounts WHERE UserId=@UserId AND IsActive=1 AND AuthVersion=@AuthVersion",
                    Database.Parameter("@UserId", accountId), Database.Parameter("@AuthVersion", version));
                if (data.Rows.Count != 1) return null;
                var user = ToUser(data.Rows[0]);
                return user.Role == values[0] ? user : null;
            }
            catch (Exception ex) when (ex is SqlException || ex is System.Configuration.ConfigurationErrorsException)
            {
                System.Diagnostics.Trace.TraceError("Authentication ticket validation failed: " + ex.Message);
                return null;
            }
        }

        public static bool RestoreSession(HttpContext context)
        {
            var user = GetTicketUser(context);
            if (user == null) return false;
            if (Convert.ToString(context.Session["accountId"]) != user.UserId.ToString()) SetSession(context, user);
            return true;
        }

        public static void SignOut(HttpContext context)
        {
            FormsAuthentication.SignOut();
            if (context.Session != null) { context.Session.Clear(); context.Session.Abandon(); }
            context.Response.Cookies.Add(new HttpCookie(FormsAuthentication.FormsCookieName, "")
            {
                Expires = DateTime.UtcNow.AddDays(-1), HttpOnly = true, Secure = context.Request.IsSecureConnection, SameSite = SameSiteMode.Lax
            });
        }

        private static void SetSession(HttpContext context, AuthenticatedUser user)
        {
            context.Session["accountId"] = user.UserId;
            // Legacy Web Forms pages still depend on uid being the role-table identity.
            context.Session["uid"] = user.RoleEntityId ?? user.UserId;
            context.Session["role"] = user.Role;
            context.Session["userName"] = user.Name;
            context.Session["userEmail"] = user.Email;
            context.Session["patientId"] = user.PatientId;
            context.Session.Timeout = 30;
        }

        private static AuthenticatedUser FindByLogin(Database database, string email, string role)
        {
            try
            {
                var account = database.Query(@"SELECT TOP (1) UserId, LegacyUserId, PatientId, RoleName, DisplayName, Email, PasswordHash, AuthVersion
                    FROM gh.UserAccounts WHERE NormalizedEmail=@Email AND RoleName=@Role AND IsActive=1
                    AND (LockedUntil IS NULL OR LockedUntil<=SYSUTCDATETIME())",
                    Database.Parameter("@Email", email.Trim().ToUpperInvariant()), Database.Parameter("@Role", role));
                if (account.Rows.Count == 0) return null;
                var user = ToUser(account.Rows[0]);
                user.PasswordHash = Convert.ToString(account.Rows[0]["PasswordHash"]);
                return user;
            }
            catch (SqlException) { return null; }
        }

        private static AuthenticatedUser AuthenticateLegacy(Database database, string email, string password, string role)
        {
            var map = LegacyMap.For(role);
            if (map == null) return null;
            try
            {
                // A unified account is authoritative even when it is disabled or locked.
                // Legacy credentials must never restore it or replace a reset password.
                if(Convert.ToInt32(database.Scalar("SELECT COUNT(*) FROM gh.UserAccounts WHERE NormalizedEmail=@Email AND RoleName=@Role",Database.Parameter("@Email",email.ToUpperInvariant()),Database.Parameter("@Role",role)))>0)return null;
                var table = database.Query(map.Query, Database.Parameter("@Email", email));
                if (table.Rows.Count != 1 || !PasswordSecurity.VerifyLegacy(password, Convert.ToString(table.Rows[0]["PasswordValue"]))) return null;
                var legacyId = Convert.ToInt32(table.Rows[0]["LegacyUserId"]);
                var user = CreateMigratedLegacyAccount(database, role, legacyId, Convert.ToString(table.Rows[0]["DisplayName"]), email, password);
                if(user==null)return null;
                database.Execute(map.StoreMigratedPassword, Database.Parameter("@UserId", legacyId), Database.Parameter("@PasswordHash", PasswordSecurity.Hash(password)));
                return user;
            }
            catch (Exception ex) when (ex is SqlException || ex is System.Configuration.ConfigurationErrorsException)
            {
                System.Diagnostics.Trace.TraceError("Legacy credential migration failed: " + ex.Message);
                return null;
            }
        }

        private static AuthenticatedUser CreateMigratedLegacyAccount(Database database, string role, int legacyId, string name, string email, string password)
        {
            return database.InTransaction((connection, transaction) =>
            {
                var existing = QueryOne(connection, transaction, @"SELECT TOP (1) UserId, LegacyUserId, PatientId, RoleName, DisplayName, Email, AuthVersion
                    FROM gh.UserAccounts WITH (UPDLOCK,HOLDLOCK) WHERE RoleName=@Role AND (NormalizedEmail=@Email OR LegacyUserId=@LegacyId)",
                    Database.Parameter("@Email", email.ToUpperInvariant()), Database.Parameter("@Role", role),Database.Parameter("@LegacyId",legacyId));
                if (existing == null)
                {
                    var userId = Convert.ToInt32(Scalar(connection, transaction, @"INSERT INTO gh.UserAccounts
                        (LegacyUserId,RoleName,DisplayName,Email,NormalizedEmail,PasswordHash,IsActive,FailedLoginCount,AuthVersion)
                        VALUES(@LegacyUserId,@Role,@Name,@Email,@NormalizedEmail,@PasswordHash,1,0,1); SELECT CAST(SCOPE_IDENTITY() AS int);",
                        Database.Parameter("@LegacyUserId", legacyId), Database.Parameter("@Role", role), Database.Parameter("@Name", name), Database.Parameter("@Email", email),
                        Database.Parameter("@NormalizedEmail", email.ToUpperInvariant()), Database.Parameter("@PasswordHash", PasswordSecurity.Hash(password))));
                    return new AuthenticatedUser { UserId = userId, RoleEntityId = legacyId, Role = role, Name = name, Email = email, AuthVersion = 1 };
                }
                // Also protects renamed accounts and a concurrent first-login migration.
                return null;
            });
        }

        private static bool UpdateSuccessfulLogin(Database database, AuthenticatedUser user, string password)
        {
            return database.Execute(@"UPDATE gh.UserAccounts SET FailedLoginCount=0,LockedUntil=NULL,LastLoginAt=SYSUTCDATETIME(),
                PasswordHash=CASE WHEN @Upgrade=1 THEN @PasswordHash ELSE PasswordHash END WHERE UserId=@UserId
                AND IsActive=1 AND AuthVersion=@Version AND PasswordHash=@VerifiedHash AND (LockedUntil IS NULL OR LockedUntil<=SYSUTCDATETIME())",
                Database.Parameter("@Upgrade", PasswordSecurity.NeedsUpgrade(user.PasswordHash) ? 1 : 0),
                Database.Parameter("@PasswordHash", PasswordSecurity.NeedsUpgrade(user.PasswordHash) ? PasswordSecurity.Hash(password) : user.PasswordHash),
                Database.Parameter("@UserId", user.UserId),Database.Parameter("@Version",user.AuthVersion),Database.Parameter("@VerifiedHash",user.PasswordHash))==1;
        }

        private static void RegisterFailedAttempt(Database database, int userId)
        {
            database.Execute(@"UPDATE gh.UserAccounts SET FailedLoginCount=FailedLoginCount+1,
                LockedUntil=CASE WHEN FailedLoginCount+1>=@Limit THEN DATEADD(MINUTE,15,SYSUTCDATETIME()) ELSE LockedUntil END WHERE UserId=@UserId",
                Database.Parameter("@Limit", MaxFailedAttempts), Database.Parameter("@UserId", userId));
        }

        private static AuthenticatedUser ToUser(DataRow row)
        {
            return new AuthenticatedUser
            {
                UserId = Convert.ToInt32(row["UserId"]),
                RoleEntityId = row.Table.Columns.Contains("LegacyUserId") && row["LegacyUserId"] != DBNull.Value ? (int?)Convert.ToInt32(row["LegacyUserId"]) : null,
                PatientId = row.Table.Columns.Contains("PatientId") && row["PatientId"] != DBNull.Value ? (int?)Convert.ToInt32(row["PatientId"]) : null,
                Role = Convert.ToString(row["RoleName"]), Name = Convert.ToString(row["DisplayName"]), Email = Convert.ToString(row["Email"]),
                AuthVersion = row.Table.Columns.Contains("AuthVersion") ? Convert.ToInt32(row["AuthVersion"]) : 1
            };
        }

        private static bool IsKnownRole(string role) { return role == "Admin" || role == "Doctor" || role == "Receptionist" || role == "Laboratorian" || role == "Nurse" || role == "Pharmacist" || role == "Patient"; }
        private static object Scalar(SqlConnection connection, SqlTransaction transaction, string sql, params SqlParameter[] parameters) { using (var command = Command(connection, transaction, sql, parameters)) return command.ExecuteScalar(); }
        private static int Execute(SqlConnection connection, SqlTransaction transaction, string sql, params SqlParameter[] parameters) { using (var command = Command(connection, transaction, sql, parameters)) return command.ExecuteNonQuery(); }
        private static DataRow QueryOne(SqlConnection connection, SqlTransaction transaction, string sql, params SqlParameter[] parameters) { var data = new DataTable(); using (var command = Command(connection, transaction, sql, parameters)) using (var adapter = new SqlDataAdapter(command)) adapter.Fill(data); return data.Rows.Count == 0 ? null : data.Rows[0]; }
        private static SqlCommand Command(SqlConnection connection, SqlTransaction transaction, string sql, SqlParameter[] parameters) { var command = new SqlCommand(sql, connection, transaction); command.Parameters.AddRange(parameters); return command; }

        private sealed class LegacyMap
        {
            public string Query { get; private set; }
            public string StoreMigratedPassword { get; private set; }
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
                    Query = "SELECT " + id + " AS LegacyUserId," + name + " AS DisplayName," + email + " AS Email," + password + " AS PasswordValue FROM dbo." + table + " WHERE " + email + "=@Email AND " + status + "='Active'",
                    StoreMigratedPassword = "UPDATE dbo." + table + " SET " + password + "=@PasswordHash WHERE " + id + "=@UserId"
                };
            }
        }
    }
}
