using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace HospitalMSWebApp.Infrastructure
{
    /// <summary>
    /// Creates predictable, non-production accounts in the local development database only.
    /// It is disabled by the Release transform and never relies on raw password storage.
    /// </summary>
    public static class DevelopmentDataSeeder
    {
        private static string Password
        {
            get
            {
                string password = Environment.GetEnvironmentVariable("GUARDIAN_DEMO_PASSWORD");
                if (string.IsNullOrWhiteSpace(password))
                {
                    throw new InvalidOperationException("GUARDIAN_DEMO_PASSWORD is not configured.");
                }
                return password;
            }
        }

        public static void EnsureDevelopmentAccounts()
        {
            EnsureDevelopmentAccounts(ConfigurationManager.AppSettings["EnvironmentName"], ConfigurationManager.AppSettings["SeedDevelopmentData"]);
        }

        // Configuration is read only at the public startup boundary. Keep the gate together
        // with the bootstrap operation so tests can exercise it without mutating app settings.
        private static void EnsureDevelopmentAccounts(string environmentName, string enabled)
        {
            if (!string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase)) return;
            try
            {
                var database = new Database();
                database.InTransaction((connection, transaction) =>
                {
                    // Bootstrap only an empty account store, never repair a populated one on startup.
                    // Looking for original seed emails would recreate access after an administrator
                    // intentionally renamed or removed an account. Existing identities belong to
                    // the user, regardless of their current email, activation or lockout state.
                    if(Convert.ToInt32(Scalar(connection,transaction,"SELECT COUNT(*) FROM gh.UserAccounts WITH(UPDLOCK,HOLDLOCK)"))!=0)return 0;
                    var doctorId = EnsureDoctor(connection, transaction);
                    var receptionistId = EnsureReceptionist(connection, transaction);
                    var laboratorianId = EnsureLaboratorian(connection, transaction);
                    var patientId = EnsurePatient(connection, transaction, receptionistId);
                    var nurseStaffId = EnsureStaffMember(connection, transaction, "Nurse", "DEV-NURSE-001", "Morgan Bell", "dev.nurse@guardianhealth.test", "555-0105");
                    var pharmacistStaffId = EnsureStaffMember(connection, transaction, "Pharmacist", "DEV-PHARM-001", "Riley Chen", "dev.pharmacist@guardianhealth.test", "555-0106");
                    EnsureAccount(connection, transaction, "Admin", "dev.admin@guardianhealth.test", "Development Administrator", null, null, null);
                    EnsureAccount(connection, transaction, "Doctor", "dev.doctor@guardianhealth.test", "Dr. Avery Morgan", doctorId, null, null);
                    EnsureAccount(connection, transaction, "Receptionist", "dev.reception@guardianhealth.test", "Jordan Lee", receptionistId, null, null);
                    EnsureAccount(connection, transaction, "Laboratorian", "dev.lab@guardianhealth.test", "Casey Patel", laboratorianId, null, null);
                    EnsureAccount(connection, transaction, "Nurse", "dev.nurse@guardianhealth.test", "Morgan Bell", null, null, nurseStaffId);
                    EnsureAccount(connection, transaction, "Pharmacist", "dev.pharmacist@guardianhealth.test", "Riley Chen", null, null, pharmacistStaffId);
                    EnsureAccount(connection, transaction, "Patient", "dev.patient@guardianhealth.test", "Taylor Reed", null, patientId, null);
                    return 0;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Development account seed failed: " + ex);
                throw new InvalidOperationException("Guardian Health development accounts could not be initialized.", ex);
            }
        }

        private static int EnsureDoctor(SqlConnection connection, SqlTransaction transaction)
        {
            var id = NullableInt(Scalar(connection, transaction, "SELECT doctorID FROM dbo.doctorTbl WHERE doctorEmail=@Email", P("@Email", "dev.doctor@guardianhealth.test")));
            if (!id.HasValue)
                id = Convert.ToInt32(Scalar(connection, transaction, @"INSERT INTO dbo.doctorTbl(doctorName,doctorPhone,doctorExp,doctorSpec,doctorGender,doctorAdd,doctorDOB,doctorPassword,doctorEmail,doctorStatus)
                    VALUES(@Name,@Phone,8,@Spec,'Other','Guardian Health development clinic','1985-01-15',@PasswordHash,@Email,'Active'); SELECT CAST(SCOPE_IDENTITY() AS int);",
                    P("@Name", "Dr. Avery Morgan"), P("@Phone", "555-0101"), P("@Spec", "General Medicine"), P("@PasswordHash", PasswordSecurity.Hash(Password)), P("@Email", "dev.doctor@guardianhealth.test")));
            return id.Value;
        }

        private static int EnsureReceptionist(SqlConnection connection, SqlTransaction transaction)
        {
            var id = NullableInt(Scalar(connection, transaction, "SELECT receptionistID FROM dbo.receptionistTbl WHERE receptionistEmail=@Email", P("@Email", "dev.reception@guardianhealth.test")));
            if (!id.HasValue)
                id = Convert.ToInt32(Scalar(connection, transaction, @"DECLARE @NewId int;
                    SELECT @NewId=ISNULL(MAX(receptionistID),0)+1 FROM dbo.receptionistTbl WITH (UPDLOCK,HOLDLOCK);
                    INSERT INTO dbo.receptionistTbl(receptionistID,receptionistName,receptionistEmail,receptionistAdd,receptionistPhone,receptionistPassword,receptionistStatus)
                    VALUES(@NewId,@Name,@Email,'Guardian Health development clinic','555-0102',@PasswordHash,'Active'); SELECT @NewId;",
                    P("@Name", "Jordan Lee"), P("@Email", "dev.reception@guardianhealth.test"), P("@PasswordHash", PasswordSecurity.Hash(Password))));
            return id.Value;
        }

        private static int EnsureLaboratorian(SqlConnection connection, SqlTransaction transaction)
        {
            var id = NullableInt(Scalar(connection, transaction, "SELECT labID FROM dbo.laboratorianTbl WHERE labEmail=@Email", P("@Email", "dev.lab@guardianhealth.test")));
            if (!id.HasValue)
                id = Convert.ToInt32(Scalar(connection, transaction, @"INSERT INTO dbo.laboratorianTbl(labName,labEmail,labPassword,labPhone,labAddress,labGender,labStatus)
                    VALUES(@Name,@Email,@PasswordHash,'555-0103','Guardian Health development clinic','Other','Active'); SELECT CAST(SCOPE_IDENTITY() AS int);",
                    P("@Name", "Casey Patel"), P("@Email", "dev.lab@guardianhealth.test"), P("@PasswordHash", PasswordSecurity.Hash(Password))));
            return id.Value;
        }

        private static int EnsurePatient(SqlConnection connection, SqlTransaction transaction, int registeredBy)
        {
            var id = NullableInt(Scalar(connection, transaction, "SELECT patientId FROM gh.UserAccounts WHERE NormalizedEmail=@Email AND RoleName='Patient'", P("@Email", "DEV.PATIENT@GUARDIANHEALTH.TEST")));
            if (!id.HasValue)
            {
                id = Convert.ToInt32(Scalar(connection, transaction, @"INSERT INTO dbo.patientTbl(patientName,patientPhone,patientGender,patientDOB,patientAdd,patientAllergies,addBy,MedicalRecordNumber,IsActive)
                    VALUES('Taylor Reed','555-0104','Other','1994-06-20','Guardian Health development clinic','No known allergies',@RegisteredBy,'GH-DEV-000001',1); SELECT CAST(SCOPE_IDENTITY() AS int);", P("@RegisteredBy", registeredBy)));
                Execute(connection, transaction, "INSERT INTO gh.PatientProfiles(PatientId,Email,BloodType,EmergencyContactName,EmergencyContactPhone,ChronicConditions) VALUES(@PatientId,@Email,'O+','Alex Reed','555-0199','')",
                    P("@PatientId", id), P("@Email", "dev.patient@guardianhealth.test"));
            }
            return id.Value;
        }

        private static int EnsureStaffMember(SqlConnection connection, SqlTransaction transaction, string role, string staffNumber, string name, string email, string phone)
        {
            var id = NullableInt(Scalar(connection, transaction, "SELECT StaffMemberId FROM gh.StaffMembers WHERE StaffNumber=@StaffNumber", P("@StaffNumber", staffNumber)));
            if (!id.HasValue)
                id = Convert.ToInt32(Scalar(connection, transaction, @"INSERT INTO gh.StaffMembers(StaffNumber,RoleName,FullName,Email,Phone,IsActive)
                    VALUES(@StaffNumber,@Role,@Name,@Email,@Phone,1); SELECT CAST(SCOPE_IDENTITY() AS int);", P("@StaffNumber", staffNumber), P("@Role", role), P("@Name", name), P("@Email", email), P("@Phone", phone)));
            return id.Value;
        }

        private static void EnsureAccount(SqlConnection connection, SqlTransaction transaction, string role, string email, string name, int? legacyUserId, int? patientId, int? staffMemberId)
        {
            var existing = QueryOne(connection, transaction, "SELECT UserId FROM gh.UserAccounts WITH (UPDLOCK,HOLDLOCK) WHERE NormalizedEmail=@Email AND RoleName=@Role", P("@Email", email.ToUpperInvariant()), P("@Role", role));
            if (existing == null)
            {
                Execute(connection, transaction, @"INSERT INTO gh.UserAccounts(LegacyUserId,StaffMemberId,PatientId,RoleName,DisplayName,Email,NormalizedEmail,PasswordHash,IsActive,FailedLoginCount,LockedUntil,AuthVersion)
                    VALUES(@LegacyUserId,@StaffMemberId,@PatientId,@Role,@Name,@Email,@NormalizedEmail,@PasswordHash,1,0,NULL,1)", P("@LegacyUserId", legacyUserId), P("@StaffMemberId", staffMemberId), P("@PatientId", patientId),
                    P("@Role", role), P("@Name", name), P("@Email", email), P("@NormalizedEmail", email.ToUpperInvariant()), P("@PasswordHash", PasswordSecurity.Hash(Password)));
            }

        }

        private static object Scalar(SqlConnection connection, SqlTransaction transaction, string sql, params SqlParameter[] parameters) { using (var command = Command(connection, transaction, sql, parameters)) return command.ExecuteScalar(); }
        private static int Execute(SqlConnection connection, SqlTransaction transaction, string sql, params SqlParameter[] parameters) { using (var command = Command(connection, transaction, sql, parameters)) return command.ExecuteNonQuery(); }
        private static DataRow QueryOne(SqlConnection connection, SqlTransaction transaction, string sql, params SqlParameter[] parameters) { var table = new DataTable(); using (var command = Command(connection, transaction, sql, parameters)) using (var adapter = new SqlDataAdapter(command)) adapter.Fill(table); return table.Rows.Count == 0 ? null : table.Rows[0]; }
        private static SqlCommand Command(SqlConnection connection, SqlTransaction transaction, string sql, SqlParameter[] parameters) { var command = new SqlCommand(sql, connection, transaction); command.Parameters.AddRange(parameters); return command; }
        private static SqlParameter P(string name, object value) { return Database.Parameter(name, value); }
        private static int? NullableInt(object value) { return value == null || value == DBNull.Value ? (int?)null : Convert.ToInt32(value); }
    }
}
