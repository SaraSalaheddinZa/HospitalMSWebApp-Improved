using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using HospitalMSWebApp.Infrastructure;
using HospitalMSWebApp.Services;
class MedicalTeamTests
{
    static Database db=new Database();static HospitalWorkflowService work=new HospitalWorkflowService();
    static int admin,reception,patient,doctor,portal,pharmacy;
    static SqlParameter P(string n,object v){return Database.Parameter(n,v);}
    static int I(string sql,params SqlParameter[] p){return Convert.ToInt32(db.Scalar(sql,p));}
    static void Assert(bool ok,string text){if(!ok)throw new Exception(text);Console.WriteLine("PASS: "+text);}
    static void Reject(Action a,string text){bool rejected=false;try{a();}catch(ArgumentException){rejected=true;}catch(InvalidOperationException){rejected=true;}catch(SqlException){rejected=true;}Assert(rejected,text);}
    static void Main()
    {
        Environment.SetEnvironmentVariable("HOSPITALMS_CONNECTIONSTRING",@"Data Source=.\SQLEXP2017;Initial Catalog=HospitalMS-Sara7593-Wasan7639;Integrated Security=True;MultipleActiveResultSets=True");
        admin=I("SELECT UserId FROM gh.UserAccounts WHERE Email='dev.admin@guardianhealth.test'");reception=I("SELECT UserId FROM gh.UserAccounts WHERE Email='dev.reception@guardianhealth.test'");
        portal=I("SELECT UserId FROM gh.UserAccounts WHERE Email='dev.patient@guardianhealth.test'");patient=I("SELECT PatientId FROM gh.UserAccounts WHERE UserId=@U",P("@U",portal));doctor=I("SELECT LegacyUserId FROM gh.UserAccounts WHERE Email='dev.doctor@guardianhealth.test'");pharmacy=I("SELECT UserId FROM gh.UserAccounts WHERE Email='dev.pharmacist@guardianhealth.test'");
        DateTime date=new DateTime(2097,5,12);
        using(var fixture=new TransactionScope()){
            int dep=work.AddDepartment("QA-TEAM","QA medical team","","",admin);work.SetDoctorDepartment(doctor,dep,admin);
            int schedule=work.AddDoctorSchedule(doctor,dep,date,TimeSpan.FromHours(9),TimeSpan.FromHours(10),20,admin);
            var team=work.FindMedicalTeam(portal,department:dep,date:date);Assert(team.Rows.Count==1,"Directory filters published availability by department and date");
            Assert(!team.Columns.Contains("doctorEmail")&&!team.Columns.Contains("PasswordHash")&&!team.Columns.Contains("doctorPhone"),"Directory exposes only professional public fields");
            Assert(work.FindMedicalTeam(portal,name:"not-a-doctor-xyz").Rows.Count==0,"Name search has a truthful empty result");
            Assert(work.FindMedicalTeam(portal,specialty:Convert.ToString(team.Rows[0]["Specialty"]),doctor:doctor).Rows.Count==1,"Specialty and exact profile filters use stored data");
            Assert(work.GetMedicalTeamSlots(doctor,date,portal).Rows.Count==3,"Published slot duration yields exactly three valid appointments");
            long booked=work.BookMedicalTeamSlot(schedule,date.AddHours(9),"QA booking",portal);
            Assert(I("SELECT PatientId FROM gh.Appointments WHERE AppointmentId=@A",P("@A",booked))==patient,"Doctor-first booking resolves patient identity from the authenticated account");
            Assert(I("SELECT DepartmentId FROM gh.Appointments WHERE AppointmentId=@A",P("@A",booked))==dep,"Booking records the actual schedule department");
            Assert(work.GetMedicalTeamSlots(doctor,date,portal).Rows.Count==2,"Booked slot disappears from discovery immediately");
            Assert(work.GetPatientBookingConfirmation(booked,portal).Rows.Count==1,"Booking confirmation is available to its owner");
            Assert(I("SELECT COUNT(*) FROM gh.Notifications WHERE PatientId=@P AND NotificationType='Appointment' AND CreatedAt>=DATEADD(minute,-1,SYSUTCDATETIME())",P("@P",patient))>0,"Booking generates the patient inbox notification");
            Assert(I("SELECT COUNT(*) FROM gh.WorkflowHistory WHERE EntityType='Appointment' AND EntityId=@A AND NewStatus='Booked'",P("@A",booked))==1,"Booking has attributed transactional history");
            Assert(I("SELECT COUNT(*) FROM gh.Appointments WHERE AppointmentId=@A AND DoctorId=@D AND Status='Booked'",P("@A",booked),P("@D",doctor))==1,"Reception and clinician schedules share the same persisted appointment");
            work.TransitionAppointment(booked,"Cancelled",portal);Assert(work.GetMedicalTeamSlots(doctor,date,portal).Rows.Count==3,"Valid cancellation releases the published slot");
        }
        foreach(string scenario in new[]{"directory-role","profile-role","slots-role","department-role","invalid-department","past-filter","invalid-slot","wrong-doctor-session","booking-role","duplicate","db-no-schedule","db-owner","db-duration","disabled-doctor","confirmation-owner"})
        using(var fixture=new TransactionScope()){
            int schedule=work.AddDoctorSchedule(doctor,null,date,TimeSpan.FromHours(9),TimeSpan.FromHours(10),20,admin);
            Action action;
            if(scenario=="directory-role"||scenario=="profile-role")action=()=>work.FindMedicalTeam(pharmacy,doctor:scenario=="profile-role"?(int?)doctor:null);
            else if(scenario=="slots-role")action=()=>work.GetMedicalTeamSlots(doctor,date,pharmacy);
            else if(scenario=="department-role")action=()=>work.SetDoctorDepartment(doctor,null,portal);
            else if(scenario=="invalid-department")action=()=>work.SetDoctorDepartment(doctor,int.MaxValue,admin);
            else if(scenario=="past-filter")action=()=>work.FindMedicalTeam(portal,date:DateTime.UtcNow.Date.AddDays(-1));
            else if(scenario=="invalid-slot")action=()=>work.BookMedicalTeamSlot(schedule,date.AddHours(9).AddMinutes(1),"QA",portal);
            else if(scenario=="wrong-doctor-session")action=()=>work.BookMedicalTeamSlot(int.MaxValue,date.AddHours(9),"QA",portal);
            else if(scenario=="booking-role")action=()=>work.BookMedicalTeamSlot(schedule,date.AddHours(9),"QA",reception);
            else if(scenario=="duplicate"){work.BookMedicalTeamSlot(schedule,date.AddHours(9),"QA",portal);action=()=>work.BookMedicalTeamSlot(schedule,date.AddHours(9),"QA",portal);}
            else if(scenario=="disabled-doctor"){db.Execute("UPDATE gh.UserAccounts SET IsActive=0 WHERE RoleName='Doctor' AND LegacyUserId=@D",P("@D",doctor));Assert(work.FindMedicalTeam(portal,doctor:doctor).Rows.Count==0,"Disabled doctor is hidden from discovery");action=()=>work.BookAppointment(patient,doctor,date.AddHours(9),date.AddHours(9).AddMinutes(20),"QA",portal);}
            else if(scenario=="confirmation-owner"){
                long id=work.BookMedicalTeamSlot(schedule,date.AddHours(9),"QA",portal);int other=I("SELECT TOP 1 patientId FROM dbo.patientTbl WHERE patientId<>@P",P("@P",patient));db.Execute("UPDATE gh.Appointments SET PatientId=@P WHERE AppointmentId=@A",P("@P",other),P("@A",id));Assert(work.GetPatientBookingConfirmation(id,portal).Rows.Count==0,"Another patient's confirmation cannot be disclosed");continue;
            }
            else {
                DateTime start=scenario=="db-no-schedule"?date.AddDays(1).AddHours(9):date.AddHours(9);
                int owner=scenario=="db-owner"?I("SELECT TOP 1 patientId FROM dbo.patientTbl WHERE patientId<>@P",P("@P",patient)):patient;
                DateTime end=start.AddMinutes(scenario=="db-duration"?15:20);
                action=()=>db.Scalar("EXEC gh.BookAppointment @P,@D,NULL,@S,@E,'QA',@U",P("@P",owner),P("@D",doctor),P("@S",start),P("@E",end),P("@U",portal));
            }
            Reject(action,"Medical-team service/database guard: "+scenario);
        }
        ConcurrentBooking();
    }
    static void ConcurrentBooking()
    {
        string code="QA-MT-"+Guid.NewGuid().ToString("N").Substring(0,12);int clinician=0,schedule=0,p1=0,p2=0,u1=0,u2=0;DateTime date=new DateTime(2097,5,13,9,0,0);
        try{
            using(var setup=new TransactionScope()){
                clinician=work.SaveStaffDirectoryEntry(new StaffDirectoryEntry{Role="Doctor",Name=code,Email=code+"@example.test",Phone="",Address="QA",Gender="Other",Status="Active",Password="FixtureBooking123!",Specialty="QA",Experience=0,BirthDate=new DateTime(1980,1,1)},admin);
                for(int n=0;n<2;n++){
                    int p=work.RegisterPatient(code+n,"","Other",new DateTime(1990,1,1),"QA","",code+n+"@example.test",null,null,null,null,null,reception);
                    int u=work.CreatePatientAccount(p,code+n+"@example.test","FixtureBooking123!",reception);if(n==0){p1=p;u1=u;}else{p2=p;u2=u;}
                }
                schedule=work.AddDoctorSchedule(clinician,null,date.Date,TimeSpan.FromHours(9),TimeSpan.FromHours(10),20,admin);setup.Complete();
            }
            var start=new ManualResetEventSlim(false);int succeeded=0,rejected=0;
            Func<int,Task> attempt=who=>Task.Run(()=>{start.Wait();try{work.BookMedicalTeamSlot(schedule,date,"QA simultaneous booking",who);Interlocked.Increment(ref succeeded);}catch(InvalidOperationException){Interlocked.Increment(ref rejected);}catch(SqlException e){if(e.Number==1205||e.Number==51002)Interlocked.Increment(ref rejected);else throw;}});
            var first=attempt(u1);var second=attempt(u2);start.Set();Task.WaitAll(first,second);
            Assert(succeeded==1&&rejected==1,"Independent simultaneous patient bookings admit exactly one winner");
            Assert(I("SELECT COUNT(*) FROM gh.Appointments WHERE DoctorId=@D",P("@D",clinician))==1,"Concurrent booking persists exactly one appointment");
        }finally{
            if(clinician>0)db.InTransaction((c,t)=>{
                using(var cmd=new SqlCommand(@"IF NOT EXISTS(SELECT 1 FROM dbo.doctorTbl WHERE doctorId=@D AND doctorName=@Code) THROW 51241,'Fixture identity mismatch',1;
                IF EXISTS(SELECT 1 FROM gh.Appointments WHERE DoctorId=@D AND PatientId NOT IN(@P1,@P2)) THROW 51242,'Unrelated appointment protects fixture from cleanup',1;
                DELETE FROM gh.WorkflowHistory WHERE (EntityType='Appointment' AND EntityId IN(SELECT AppointmentId FROM gh.Appointments WHERE DoctorId=@D))
                OR (EntityType='DoctorSchedule' AND EntityId IN(SELECT DoctorScheduleId FROM gh.DoctorSchedules WHERE DoctorId=@D))
                OR (EntityType='Patient' AND EntityId IN(@P1,@P2)) OR (EntityType='DoctorDirectory' AND EntityId=@D)
                OR (EntityType='UserAccount' AND EntityId IN(SELECT UserId FROM gh.UserAccounts WHERE PatientId IN(@P1,@P2) OR (RoleName='Doctor' AND LegacyUserId=@D)));
                DELETE FROM gh.Notifications WHERE PatientId IN(@P1,@P2);
                DELETE FROM gh.Appointments WHERE DoctorId=@D;
                DELETE FROM gh.DoctorSchedules WHERE DoctorId=@D;
                DELETE FROM gh.UserAccounts WHERE PatientId IN(@P1,@P2) OR (RoleName='Doctor' AND LegacyUserId=@D);
                DELETE FROM gh.PatientProfiles WHERE PatientId IN(@P1,@P2);
                DELETE FROM dbo.patientTbl WHERE patientId IN(@P1,@P2) AND patientName LIKE @Code+'%';
                DELETE FROM dbo.doctorTbl WHERE doctorId=@D AND doctorName=@Code;",c,t)){
                    cmd.Parameters.AddRange(new[]{P("@D",clinician),P("@P1",p1),P("@P2",p2),P("@Code",code)});cmd.ExecuteNonQuery();
                }return 0;
            });
        }
        Assert(I("SELECT COUNT(*) FROM gh.UserAccounts WHERE Email LIKE @C+'%'",P("@C",code))==0,"Concurrent booking removes only its isolated fixture accounts");
    }
}
