using System;
using System.Data.SqlClient;
using System.Transactions;
using HospitalMSWebApp.Infrastructure;
using HospitalMSWebApp.Services;
class AppointmentLifecycle
{
    static readonly Database Db=new Database();
    static readonly HospitalWorkflowService Work=new HospitalWorkflowService();
    static int patient,doctor,reception,clinician,portal,nurse,admin;
    static int Actor(string role){return Convert.ToInt32(Db.Scalar("SELECT UserId FROM gh.UserAccounts WHERE RoleName=@R AND Email LIKE 'dev.%@guardianhealth.test'",Database.Parameter("@R",role)));}
    static void Assert(bool ok,string message){if(!ok)throw new Exception(message);Console.WriteLine("PASS: "+message);}
    static long Seed(string state,DateTime start,bool checkIn=true)
    {
        return Convert.ToInt64(Db.Scalar(@"INSERT INTO gh.Appointments(PatientId,DoctorId,ScheduledStart,ScheduledEnd,Status,CheckInAt)
            VALUES(@P,@D,@S,@E,@State,@In);SELECT CAST(SCOPE_IDENTITY() AS bigint)",Database.Parameter("@P",patient),Database.Parameter("@D",doctor),Database.Parameter("@S",start),Database.Parameter("@E",start.AddMinutes(20)),Database.Parameter("@State",state),Database.Parameter("@In",checkIn&&(state=="CheckedIn"||state=="InProgress")?(object)DateTime.UtcNow:null)));
    }
    static long Consult(long id,int who){return Work.RecordEncounter(patient,doctor,id,"QA attendance","Assessment","Plan",null,null,null,null,null,null,null,null,null,null,null,who);}
    static void Main()
    {
        Environment.SetEnvironmentVariable("HOSPITALMS_CONNECTIONSTRING",@"Data Source=.\SQLEXP2017;Initial Catalog=HospitalMS-Sara7593-Wasan7639;Integrated Security=True;MultipleActiveResultSets=True");
        patient=Convert.ToInt32(Db.Scalar("SELECT PatientId FROM gh.UserAccounts WHERE Email='dev.patient@guardianhealth.test'"));
        doctor=Convert.ToInt32(Db.Scalar("SELECT LegacyUserId FROM gh.UserAccounts WHERE Email='dev.doctor@guardianhealth.test'"));
        reception=Actor("Receptionist");clinician=Actor("Doctor");portal=Actor("Patient");nurse=Actor("Nurse");admin=Actor("Admin");
        foreach(string before in new[]{"Booked","Confirmed","RescheduleRequested","CheckedIn","InProgress","Cancelled","NoShow","Completed"})
        foreach(string after in new[]{"Confirmed","CheckedIn","InProgress","Cancelled","NoShow","RescheduleRequested","Completed"})
        using(var fixture=new TransactionScope())
        {
            // Same-day attendance, after midnight UTC, so NoShow has a genuinely elapsed start.
            long id=Seed(before,DateTime.UtcNow.Date);
            bool booked=before=="Booked"||before=="Confirmed"||before=="RescheduleRequested";
            bool expected=(after=="Confirmed"&&before=="Booked")||(after=="CheckedIn"&&booked)||(after=="InProgress"&&before=="CheckedIn")||((after=="Cancelled"||after=="NoShow")&&booked)||(after=="RescheduleRequested"&&(before=="Booked"||before=="Confirmed"));
            bool rejected=false;
            try{Work.TransitionAppointment(id,after,reception,null,DateTime.UtcNow.AddDays(2).Date.AddHours(10),DateTime.UtcNow.AddDays(2).Date.AddHours(11),"QA request");}
            catch(InvalidOperationException){rejected=true;}
            Assert(rejected!=expected,"Appointment transition "+before+" -> "+after+" is "+(expected?"allowed":"rejected"));
            if(expected){
                Assert(Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM gh.WorkflowHistory WHERE EntityType='Appointment' AND EntityId=@A AND PreviousStatus=@Old AND NewStatus=@New AND ActorUserId=@U",Database.Parameter("@A",id),Database.Parameter("@Old",before),Database.Parameter("@New",after),Database.Parameter("@U",reception)))==1,"Successful transition has one attributed history event");
                if(after=="CheckedIn")Assert(Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM gh.Notifications WHERE PatientId=@P AND UserId=@U AND Subject='Patient checked in'",Database.Parameter("@P",patient),Database.Parameter("@U",clinician)))==1,"Reception check-in notifies the assigned clinician");
            }
        }
        foreach(string state in new[]{"Booked","Confirmed","RescheduleRequested","Cancelled","NoShow","Completed","CheckedIn","InProgress"})
        using(var fixture=new TransactionScope())
        {
            long id=Seed(state,DateTime.UtcNow.Date);bool expected=state=="CheckedIn"||state=="InProgress",rejected=false;
            try{Consult(id,clinician);}catch(InvalidOperationException){rejected=true;}
            Assert(rejected!=expected,"Linked consultation requires recorded attendance: "+state);
            if(expected){
                Assert(Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM gh.Encounters WHERE AppointmentId=@A",Database.Parameter("@A",id)))==1,"Exactly one consultation is associated with attended appointment");
                Assert(Convert.ToString(Db.Scalar("SELECT PreviousStatus FROM gh.WorkflowHistory WHERE EntityType='Appointment' AND EntityId=@A AND NewStatus='Completed'",Database.Parameter("@A",id)))==state,"Consultation completion records its actual prior queue status");
            }
        }
        foreach(string scenario in new[]{"future-checkin","past-checkin","self-checkin","wrong-role-checkin","future-noshow","missing-attendance","duplicate-consultation","cancelled-reschedule","completed-reschedule","unpublished-reschedule","misaligned-slot","invalid-window","inactive-doctor-schedule","inactive-doctor-reschedule"})
        using(var fixture=new TransactionScope())
        {
            var today=DateTime.UtcNow.Date;long id=Seed("Booked",today);Action action;
            if(scenario=="future-checkin"||scenario=="past-checkin"||scenario=="future-noshow"){
                Db.Execute("UPDATE gh.Appointments SET ScheduledStart=@S,ScheduledEnd=@E WHERE AppointmentId=@A",Database.Parameter("@S",today.AddDays(scenario=="past-checkin"?-1:1)),Database.Parameter("@E",today.AddDays(scenario=="past-checkin"?-1:1).AddMinutes(20)),Database.Parameter("@A",id));
                action=()=>Work.TransitionAppointment(id,scenario=="future-noshow"?"NoShow":"CheckedIn",reception);
            }else if(scenario=="self-checkin"||scenario=="wrong-role-checkin")action=()=>Work.TransitionAppointment(id,"CheckedIn",scenario=="self-checkin"?portal:nurse);
            else if(scenario=="missing-attendance"){
                Db.Execute("UPDATE gh.Appointments SET Status='CheckedIn',CheckInAt=NULL WHERE AppointmentId=@A",Database.Parameter("@A",id));action=()=>Consult(id,clinician);
            }else if(scenario=="duplicate-consultation"){
                Work.TransitionAppointment(id,"CheckedIn",reception);Consult(id,clinician);action=()=>Consult(id,clinician);
            }else if(scenario=="cancelled-reschedule"||scenario=="completed-reschedule"){
                Db.Execute("UPDATE gh.Appointments SET Status=@S WHERE AppointmentId=@A",Database.Parameter("@S",scenario=="cancelled-reschedule"?"Cancelled":"Completed"),Database.Parameter("@A",id));action=()=>Work.RescheduleAppointment(id,new DateTime(2097,4,1,10,0,0),new DateTime(2097,4,1,10,20,0),reception);
            }else if(scenario=="inactive-doctor-schedule"||scenario=="inactive-doctor-reschedule"){
                Db.Execute("UPDATE dbo.doctorTbl SET doctorStatus='Inactive' WHERE doctorID=@D",Database.Parameter("@D",doctor));
                action=scenario=="inactive-doctor-schedule"?(Action)(()=>Work.AddDoctorSchedule(doctor,null,new DateTime(2097,4,1),TimeSpan.FromHours(10),TimeSpan.FromHours(11),20,admin)):(()=>Work.RescheduleAppointment(id,new DateTime(2097,4,1,10,0,0),new DateTime(2097,4,1,10,20,0),reception));
            }else if(scenario=="invalid-window")action=()=>Work.AddDoctorSchedule(doctor,null,new DateTime(2097,4,1),TimeSpan.FromHours(-1),TimeSpan.FromHours(1),20,admin);
            else if(scenario=="misaligned-slot"){
                Work.AddDoctorSchedule(doctor,null,new DateTime(2097,4,1),TimeSpan.FromHours(10),TimeSpan.FromHours(11),20,admin);action=()=>Work.BookAppointment(patient,doctor,new DateTime(2097,4,1,10,1,0),new DateTime(2097,4,1,10,21,0),"Test",reception);
            }else action=()=>Work.RescheduleAppointment(id,new DateTime(2097,4,1,10,0,0),new DateTime(2097,4,1,10,20,0),reception);
            bool rejected=false;try{action();}catch(InvalidOperationException){rejected=true;}catch(ArgumentException){rejected=true;}
            Assert(rejected,"Appointment guard: "+scenario);
        }
        using(var fixture=new TransactionScope()){
            long id=Seed("CheckedIn",DateTime.UtcNow.Date);Consult(id,clinician);
            bool rejected=false;try{Db.Execute("INSERT INTO gh.Encounters(AppointmentId,PatientId,DoctorId) VALUES(@A,@P,@D)",Database.Parameter("@A",id),Database.Parameter("@P",patient),Database.Parameter("@D",doctor));}catch(SqlException ex){rejected=ex.Number==2601||ex.Number==2627;}
            Assert(rejected,"Database rejects a second consultation even outside the service");
        }
        Console.WriteLine("All appointment lifecycle fixtures roll back.");
    }
}
