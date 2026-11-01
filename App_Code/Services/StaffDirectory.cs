using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using HospitalMSWebApp.Infrastructure;
namespace HospitalMSWebApp.Services
{
    public sealed class StaffDirectoryEntry
    {
        public string Role,Name,Email,Phone,Address,Gender,Status,Password,Specialty;
        public int? Id,Experience;
        public DateTime? BirthDate;
        public bool ConfirmEmailChange;
        public bool ConfirmPasswordChange;
    }
    public sealed partial class HospitalWorkflowService
    {
        private sealed class StaffDirectoryMap
        {
            public string Table,Id,Name,Email,Phone,Address,Password,Status,Gender;
        }
        private static StaffDirectoryMap DirectoryMap(string role)
        {
            // SQL identifiers come only from this fixed allowlist, never from submitted input.
            if(role=="Doctor")return new StaffDirectoryMap{Table="dbo.doctorTbl",Id="doctorID",Name="doctorName",Email="doctorEmail",Phone="doctorPhone",Address="doctorAdd",Password="doctorPassword",Status="doctorStatus",Gender="doctorGender"};
            if(role=="Laboratorian")return new StaffDirectoryMap{Table="dbo.laboratorianTbl",Id="labID",Name="labName",Email="labEmail",Phone="labPhone",Address="labAddress",Password="labPassword",Status="labStatus",Gender="labGender"};
            if(role=="Receptionist")return new StaffDirectoryMap{Table="dbo.receptionistTbl",Id="receptionistID",Name="receptionistName",Email="receptionistEmail",Phone="receptionistPhone",Address="receptionistAdd",Password="receptionistPassword",Status="receptionistStatus"};
            throw new ArgumentException("Choose a supported staff directory.");
        }
        public int SaveStaffDirectoryEntry(StaffDirectoryEntry entry,int actor)
        {
            if(entry==null)throw new ArgumentException("Enter staff details.");
            var map=DirectoryMap(entry.Role);
            string name=(entry.Name??"").Trim(),email=(entry.Email??"").Trim().ToLowerInvariant(),phone=(entry.Phone??"").Trim(),address=(entry.Address??"").Trim(),gender=(entry.Gender??"").Trim(),specialty=(entry.Specialty??"").Trim();
            if(name.Length<1||name.Length>50||email.Length>(entry.Role=="Laboratorian"?50:150)||!System.Text.RegularExpressions.Regex.IsMatch(email,@"^[^\s@]+@[^\s@]+\.[^\s@]+$")||phone.Length>50||address.Length>(entry.Role=="Receptionist"?100:150)||Array.IndexOf(new[]{"Active","Inactive"},entry.Status)<0)
                throw new ArgumentException("Enter a name (up to 50 characters), valid email, phone, address and Active or Inactive status within the directory limits.");
            if(map.Gender!=null&&(gender.Length<1||gender.Length>10))throw new ArgumentException("Choose a valid gender.");
            if(entry.Role=="Doctor"&&(!entry.BirthDate.HasValue||entry.BirthDate.Value.Date>DateTime.UtcNow.Date||entry.BirthDate.Value.Year<1753||!entry.Experience.HasValue||entry.Experience<0||entry.Experience>80||specialty.Length<1||specialty.Length>50))throw new ArgumentException("Enter a valid birth date, specialization and experience from 0 to 80 years.");
            bool replace=!string.IsNullOrEmpty(entry.Password);
            if(entry.Id.HasValue&&replace&&!entry.ConfirmPasswordChange)throw new ArgumentException("Leave password blank to preserve the existing credential, or explicitly confirm a password replacement.");
            string error;if((!entry.Id.HasValue||replace)&&!PasswordSecurity.IsAcceptable(entry.Password,out error))throw new ArgumentException(error);
            string hash=replace?PasswordSecurity.Hash(entry.Password):null;
            return database.InTransaction((c,t)=>{
                actor=RequireActor(c,t,actor,"Admin");
                var previous=entry.Id.HasValue?QueryOne(c,t,"SELECT "+map.Name+" AS Name,"+map.Email+" AS Email,"+map.Status+" AS Status,"+map.Password+" AS PasswordHash FROM "+map.Table+" WITH(UPDLOCK,HOLDLOCK) WHERE "+map.Id+"=@I",P("@I",entry.Id.Value)):null;
                if(entry.Id.HasValue&&previous==null)throw new InvalidOperationException("Select an existing staff member.");
                var account=entry.Id.HasValue?QueryOne(c,t,"SELECT UserId,NormalizedEmail,IsActive FROM gh.UserAccounts WITH(UPDLOCK,HOLDLOCK) WHERE RoleName=@R AND LegacyUserId=@I",P("@R",entry.Role),P("@I",entry.Id.Value)):null;
                if(account!=null&&!string.Equals(Convert.ToString(account["NormalizedEmail"]),email.ToUpperInvariant(),StringComparison.Ordinal)&&!entry.ConfirmEmailChange)throw new ArgumentException("Confirm the sign-in email change explicitly, or keep the existing account email.");
                if(account!=null&&!Convert.ToBoolean(account["IsActive"])&&entry.Status=="Active")throw new ArgumentException("This sign-in account is disabled. Profile edits cannot reactivate it; preserve Inactive status.");
                if(Convert.ToInt32(Scalar(c,t,"SELECT COUNT(*) FROM gh.UserAccounts WITH(UPDLOCK,HOLDLOCK) WHERE NormalizedEmail=@E AND UserId<>@U",P("@E",email.ToUpperInvariant()),P("@U",account==null?0:account["UserId"])))>0)throw new InvalidOperationException("This email is already assigned to another account.");
                foreach(var role in new[]{"Doctor","Laboratorian","Receptionist"}){
                    var other=DirectoryMap(role);
                    if(Convert.ToInt32(Scalar(c,t,"SELECT COUNT(*) FROM "+other.Table+" WITH(UPDLOCK,HOLDLOCK) WHERE "+other.Email+"=@E AND "+other.Id+"<>@I",P("@E",email),P("@I",entry.Role==role?entry.Id.GetValueOrDefault():0)))>0)throw new InvalidOperationException("This email is already assigned in a staff directory.");
                }
                var parameters=new List<SqlParameter>{P("@N",name),P("@E",email),P("@Phone",phone),P("@A",address),P("@S",entry.Status)};
                string columns=map.Name+","+map.Email+","+map.Phone+","+map.Address+","+map.Status,values="@N,@E,@Phone,@A,@S",updates=map.Name+"=@N,"+map.Email+"=@E,"+map.Phone+"=@Phone,"+map.Address+"=@A,"+map.Status+"=@S";
                if(map.Gender!=null){columns+=","+map.Gender;values+=",@G";updates+=","+map.Gender+"=@G";parameters.Add(P("@G",gender));}
                if(entry.Role=="Doctor"){columns+=",doctorDOB,doctorExp,doctorSpec";values+=",@DOB,@Exp,@Spec";updates+=",doctorDOB=@DOB,doctorExp=@Exp,doctorSpec=@Spec";parameters.Add(P("@DOB",entry.BirthDate.Value.Date));parameters.Add(P("@Exp",entry.Experience.Value));parameters.Add(P("@Spec",specialty));}
                int id;
                if(entry.Id.HasValue){
                    id=entry.Id.Value;parameters.Add(P("@I",id));
                    if(replace){updates+=","+map.Password+"=@Hash";parameters.Add(P("@Hash",hash));}
                    Execute(c,t,"UPDATE "+map.Table+" SET "+updates+" WHERE "+map.Id+"=@I",parameters.ToArray());
                }else{
                    columns+=","+map.Password;values+=",@Hash";parameters.Add(P("@Hash",hash));
                    string identity="SELECT CAST(SCOPE_IDENTITY() AS int)";
                    if(entry.Role=="Receptionist"){
                        id=Convert.ToInt32(Scalar(c,t,"SELECT ISNULL(MAX(receptionistID),0)+1 FROM dbo.receptionistTbl WITH(UPDLOCK,HOLDLOCK)"));
                        columns+=","+map.Id;values+=",@I";parameters.Add(P("@I",id));identity="SELECT @I";
                    }
                    id=Convert.ToInt32(Scalar(c,t,"INSERT INTO "+map.Table+"("+columns+") VALUES("+values+");"+identity,parameters.ToArray()));
                }
                SynchronizeDirectoryAccount(c,t,entry.Role,id,name,email,hash,entry.Status=="Active",previous==null?null:Convert.ToString(previous["PasswordHash"]));
                Execute(c,t,"INSERT INTO gh.WorkflowHistory(EntityType,EntityId,ActorUserId,PreviousStatus,NewStatus,Detail) VALUES(@R,@I,@U,@Before,@After,@Detail)",P("@R",entry.Role+"Directory"),P("@I",id),P("@U",actor),P("@Before",previous==null?null:previous["Status"]),P("@After",entry.Status),P("@Detail",previous==null?"Staff profile and access created":"Profile updated: "+Convert.ToString(previous["Name"])+" -> "+name+"; email: "+Convert.ToString(previous["Email"])+" -> "+email+"; password replaced: "+(replace?"yes":"no")));
                return id;
            });
        }
        private static void SynchronizeDirectoryAccount(SqlConnection c,SqlTransaction t,string role,int id,string name,string email,string replacementHash,bool active,string previousHash)
        {
            var account=QueryOne(c,t,"SELECT UserId,IsActive FROM gh.UserAccounts WITH(UPDLOCK,HOLDLOCK) WHERE RoleName=@R AND LegacyUserId=@I",P("@R",role),P("@I",id));
            if(account==null){
                string hash=replacementHash??previousHash;
                if(string.IsNullOrEmpty(hash)||!hash.StartsWith("PBKDF2$",StringComparison.Ordinal))throw new InvalidOperationException("Set a new password to create secure sign-in access for this historical staff record.");
                Execute(c,t,"INSERT INTO gh.UserAccounts(RoleName,LegacyUserId,DisplayName,Email,NormalizedEmail,PasswordHash,IsActive) VALUES(@R,@I,@N,@E,@Normalized,@H,@A)",P("@R",role),P("@I",id),P("@N",name),P("@E",email),P("@Normalized",email.ToUpperInvariant()),P("@H",hash),P("@A",active));
            }else{
                if(!Convert.ToBoolean(account["IsActive"])&&active)throw new InvalidOperationException("Profile synchronization cannot reactivate a disabled sign-in account.");
                Execute(c,t,@"UPDATE gh.UserAccounts SET DisplayName=@N,Email=@E,NormalizedEmail=@Normalized,
                    PasswordHash=COALESCE(@Hash,PasswordHash),PasswordChangedAt=CASE WHEN @Hash IS NOT NULL THEN SYSUTCDATETIME() ELSE PasswordChangedAt END,
                    AuthVersion=CASE WHEN IsActive<>@A OR @Hash IS NOT NULL OR NormalizedEmail<>@Normalized THEN AuthVersion+1 ELSE AuthVersion END,
                    IsActive=@A WHERE UserId=@U",P("@N",name),P("@E",email),P("@Normalized",email.ToUpperInvariant()),P("@Hash",replacementHash),P("@A",active),P("@U",account["UserId"]));
            }
        }
        public void SetDirectoryStaffActive(string role,int id,bool active,int actor)
        {
            var map=DirectoryMap(role);
            database.InTransaction((c,t)=>{
                actor=RequireActor(c,t,actor,"Admin");
                var previous=Scalar(c,t,"SELECT "+map.Status+" FROM "+map.Table+" WITH(UPDLOCK,HOLDLOCK) WHERE "+map.Id+"=@I",P("@I",id));
                if(previous==null)throw new InvalidOperationException("Select an existing staff member.");
                if(Convert.ToString(previous)==(active?"Active":"Inactive"))throw new InvalidOperationException("This staff member already has the selected status.");
                Execute(c,t,"UPDATE "+map.Table+" SET "+map.Status+"=@S WHERE "+map.Id+"=@I",P("@S",active?"Active":"Inactive"),P("@I",id));
                Execute(c,t,"UPDATE gh.UserAccounts SET IsActive=@A,AuthVersion=AuthVersion+1 WHERE RoleName=@R AND LegacyUserId=@I",P("@A",active),P("@R",role),P("@I",id));
                Execute(c,t,"INSERT INTO gh.WorkflowHistory(EntityType,EntityId,ActorUserId,PreviousStatus,NewStatus) VALUES(@R,@I,@U,@Old,@S)",P("@R",role+"Directory"),P("@I",id),P("@U",actor),P("@Old",previous),P("@S",active?"Active":"Inactive"));
                return 0;
            });
        }
    }
}
