using System;
using System.Data;
using System.Data.SqlClient;

namespace HospitalMSWebApp.Services
{
    public sealed partial class HospitalWorkflowService
    {
        private static void ValidateStaffContact(string name, string email, string phone)
        {
            if (name.Length == 0 || name.Length > 160 || email.Length > 254 || phone.Length > 30 ||
                !System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^\s@]+@[^\s@]+\.[^\s@]+$"))
                throw new ArgumentException("Enter a name (up to 160 characters), valid email and telephone (up to 30 characters).");
        }
        private static void RequireStaffDepartment(SqlConnection connection, SqlTransaction transaction, int? department)
        {
            if (department.HasValue && Convert.ToInt32(Scalar(connection, transaction,
                "SELECT COUNT(*) FROM gh.Departments WHERE DepartmentId=@D AND IsActive=1", P("@D", department))) != 1)
                throw new ArgumentException("Choose an active department or leave the assignment empty.");
        }
        private static string StaffProfileVersion(SqlConnection c, SqlTransaction t, int id, int authVersion)
        {
            return authVersion.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + Convert.ToString(Scalar(c, t,
                "SELECT ISNULL(MAX(HistoryId),0) FROM gh.WorkflowHistory WHERE EntityType='UserAccount' AND EntityId=@U", P("@U", id)), System.Globalization.CultureInfo.InvariantCulture);
        }
        private static void RequireAvailableStaffEmail(SqlConnection c,SqlTransaction t,string email,int userId=0,int staffId=0)
        {
            if(Convert.ToInt32(Scalar(c,t,"SELECT COUNT(*) FROM gh.UserAccounts WITH(UPDLOCK,HOLDLOCK) WHERE NormalizedEmail=@E AND UserId<>@U",P("@E",email.ToUpperInvariant()),P("@U",userId)))>0 ||
                Convert.ToInt32(Scalar(c,t,"SELECT COUNT(*) FROM gh.StaffMembers WITH(UPDLOCK,HOLDLOCK) WHERE Email=@E AND StaffMemberId<>@S",P("@E",email),P("@S",staffId)))>0)
                throw new ArgumentException("That email belongs to another staff profile or account.");
            foreach(var role in new[]{"Doctor","Laboratorian","Receptionist"}){
                var map=DirectoryMap(role);
                if(Convert.ToInt32(Scalar(c,t,"SELECT COUNT(*) FROM "+map.Table+" WITH(UPDLOCK,HOLDLOCK) WHERE "+map.Email+"=@E",P("@E",email)))>0)
                    throw new ArgumentException("That email belongs to another staff directory.");
            }
        }
        public DataTable GetStaffProfile(int id, int actor)
        {
            return database.InTransaction((c, t) => {
                RequireActor(c, t, actor, "Admin");
                var result = ReadTable(c, t, @"SELECT u.UserId,u.RoleName,s.StaffNumber,s.FullName,u.Email,s.Phone,s.DepartmentId,u.IsActive,u.AuthVersion
                    FROM gh.UserAccounts u JOIN gh.StaffMembers s ON s.StaffMemberId=u.StaffMemberId AND s.RoleName=u.RoleName
                    WHERE u.UserId=@U AND u.RoleName IN('Nurse','Pharmacist')", P("@U", id));
                result.Columns.Add("ProfileVersion", typeof(string));
                if (result.Rows.Count == 1) result.Rows[0]["ProfileVersion"] = StaffProfileVersion(c, t, id, Convert.ToInt32(result.Rows[0]["AuthVersion"]));
                result.Columns.Remove("AuthVersion");
                return result;
            });
        }
        public void UpdateStaffProfile(int id, string name, string email, string phone, int? department,
            bool confirmEmailChange, string expectedVersion, int actor)
        {
            name=(name??"").Trim(); email=(email??"").Trim().ToLowerInvariant(); phone=(phone??"").Trim();
            ValidateStaffContact(name,email,phone);
            database.InTransaction((c,t)=>{
                actor=RequireActor(c,t,actor,"Admin");
                var account=QueryOne(c,t,"SELECT StaffMemberId,RoleName,Email,NormalizedEmail,AuthVersion FROM gh.UserAccounts WITH(UPDLOCK,HOLDLOCK) WHERE UserId=@U",P("@U",id));
                if(account==null || (Convert.ToString(account["RoleName"])!="Nurse" && Convert.ToString(account["RoleName"])!="Pharmacist"))throw new InvalidOperationException("Choose an existing nurse or pharmacist profile.");
                var previous=QueryOne(c,t,"SELECT FullName,Phone,DepartmentId FROM gh.StaffMembers WITH(UPDLOCK,HOLDLOCK) WHERE StaffMemberId=@S AND RoleName=@R",P("@S",account["StaffMemberId"]),P("@R",account["RoleName"]));
                if(previous==null)throw new InvalidOperationException("The staff profile is not linked consistently. No change was saved.");
                if(expectedVersion!=StaffProfileVersion(c,t,id,Convert.ToInt32(account["AuthVersion"])))throw new InvalidOperationException("This profile or its access changed after it was opened. Reopen it before saving.");
                bool emailChanged=Convert.ToString(account["NormalizedEmail"])!=email.ToUpperInvariant();
                if(emailChanged&&!confirmEmailChange)throw new ArgumentException("Explicitly confirm changing the existing sign-in email, or keep the current address.");
                RequireStaffDepartment(c,t,department);
                RequireAvailableStaffEmail(c,t,email,id,Convert.ToInt32(account["StaffMemberId"]));
                string beforeDepartment=Convert.ToString(previous["DepartmentId"]),afterDepartment=department.HasValue?department.Value.ToString():"";
                bool nameChanged=Convert.ToString(previous["FullName"])!=name,phoneChanged=Convert.ToString(previous["Phone"])!=phone,departmentChanged=beforeDepartment!=afterDepartment;
                if(!nameChanged&&!emailChanged&&!phoneChanged&&!departmentChanged)throw new InvalidOperationException("The profile already has these details.");
                Execute(c,t,"UPDATE gh.StaffMembers SET FullName=@N,Email=@E,Phone=@P,DepartmentId=@D,UpdatedAt=SYSUTCDATETIME() WHERE StaffMemberId=@S",P("@N",name),P("@E",email),P("@P",phone),P("@D",department),P("@S",account["StaffMemberId"]));
                // No password, activation, lockout, staff number, identity or role field is changed here.
                Execute(c,t,"UPDATE gh.UserAccounts SET DisplayName=@N,Email=@E,NormalizedEmail=@Normal,AuthVersion=AuthVersion+@Bump WHERE UserId=@U",P("@N",name),P("@E",email),P("@Normal",email.ToUpperInvariant()),P("@Bump",emailChanged?1:0),P("@U",id));
                if(nameChanged)History(c,t,"UserAccount",id,actor,"ProfileUpdated","Name: "+previous["FullName"]+" -> "+name);
                if(emailChanged)History(c,t,"UserAccount",id,actor,"ProfileUpdated","Sign-in email: "+account["Email"]+" -> "+email);
                if(phoneChanged)History(c,t,"UserAccount",id,actor,"ProfileUpdated","Telephone: "+previous["Phone"]+" -> "+phone);
                if(departmentChanged)History(c,t,"UserAccount",id,actor,"ProfileUpdated","Department: "+(beforeDepartment==""?"Unassigned":beforeDepartment)+" -> "+(afterDepartment==""?"Unassigned":afterDepartment));
                Execute(c,t,"INSERT INTO gh.Notifications(UserId,NotificationType,Subject,Message) VALUES(@U,'AccountSecurity','Staff profile updated','An administrator updated your staff profile. Your existing password and access restrictions have been preserved.')",P("@U",id));
                return 0;
            });
        }
    }
}
