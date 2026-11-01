using System;
using System.Data;
using System.Globalization;
using System.Text;
using HospitalMSWebApp.Infrastructure;
namespace HospitalMSWebApp.Views.Patient
{
    public class MedicalTeam : WorkspacePage
    {
        protected override string Role { get { return "Patient"; } }
        protected override string Heading { get { return "Find a doctor"; } }
        private long bookedAppointment;
        protected override string SubmissionReturnUrl { get { return bookedAppointment>0?"Appointments.aspx?appointment="+bookedAppointment:Request.Path+"?doctor="+Number("doctorId",true); } }
        protected override void Submit()
        {
            if(V("action")!="book")throw new ArgumentException("Unknown action.");
            int doctor=Number("doctorId",true),schedule=checked((int)Id("scheduleId"));
            if(Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM gh.DoctorSchedules WHERE DoctorScheduleId=@S AND DoctorId=@D",P("@S",schedule),P("@D",doctor)))!=1)throw new ArgumentException("Select a clinic session for this doctor.");
            bookedAppointment=Workflows.BookMedicalTeamSlot(schedule,HospitalTime.ParseUtc(V("start")),V("reason"),CurrentUser.UserId);
            Session["PatientBookingNotice"]=bookedAppointment;
            Audit("AppointmentBooked","Appointment",bookedAppointment);
        }
        private int Number(string key,bool post=false){int n;return int.TryParse(post?V(key):Request.QueryString[key],out n)&&n>0?n:0;}
        private string Input(string label,string name,string value,string type="text")
        {
            return "<div class='col-md-4'><label class='form-label'>"+H(label)+"<input class='form-control' type='"+type+"' name='"+H(name)+"' value='"+H(value)+"' maxlength='100'></label></div>";
        }
        protected override string Content()
        {
            var b=new StringBuilder("<p><a href='Appointments.aspx'>My appointments</a> · <a href='MedicalTeam.aspx'>Medical team directory</a></p><p>Find a doctor → Choose a time → Review → Book</p><p>Hospital time: "+H(HospitalTime.Zone.Id)+". Times include the UTC offset; your device timezone does not change them.</p>");
            int doctor=Number("doctor");DateTime parsed;DateTime? date=null;
            string dateText=Request.QueryString["date"]??"";
            if(dateText!=""){
                if(!DateTime.TryParseExact(dateText,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out parsed)||parsed.Year==9999||parsed.Date<HospitalTime.Local(DateTime.UtcNow,HospitalTime.Zone).Date)return b.Append(Panel("Choose an appointment date","Use today or a future date. <a href='MedicalTeam.aspx'>Return to medical team</a>.")).ToString();
                date=parsed;
            }
            if(doctor>0){
                var profile=Workflows.FindMedicalTeam(CurrentUser.UserId,doctor:doctor);
                if(profile.Rows.Count!=1)return b.Append(Panel("Doctor unavailable","This profile is not currently available for booking. Choose another doctor or contact reception.")).ToString();
                var p=profile.Rows[0];
                b.Append(Panel(Convert.ToString(p["DoctorName"]),"<p>"+H(Convert.ToString(p["Specialty"]))+" · "+H(p["DepartmentName"]==DBNull.Value?"Department not assigned":Convert.ToString(p["DepartmentName"]))+"</p><p>Availability comes from published clinic schedules. Location and preparation instructions are not recorded here; confirm these with reception.</p>"));
                DateTime selected=date??HospitalTime.Local(p["NextAvailable"]==DBNull.Value?DateTime.UtcNow:Convert.ToDateTime(p["NextAvailable"]),HospitalTime.Zone).Date;
                string back="MedicalTeam.aspx?doctor="+doctor+"&amp;date="+selected.ToString("yyyy-MM-dd");
                var slots=Workflows.GetMedicalTeamSlots(doctor,selected,CurrentUser.UserId,HospitalTime.Zone);
                if(Request.QueryString["slot"]!=null){
                    DateTime chosen;
                    try{chosen=HospitalTime.ParseUtc(Request.QueryString["slot"]);}catch(ArgumentException){return b.Append(Panel("Choose another time","The selected time is invalid. <a href='"+back+"'>Return to availability</a>.")).ToString();}
                    DataRow selection=null;foreach(DataRow candidate in slots.Rows)if(Convert.ToDateTime(candidate["SlotStart"])==chosen&&Convert.ToInt32(candidate["DoctorScheduleId"])==Number("schedule"))selection=candidate;
                    if(selection==null)return b.Append(Panel("Time no longer available","This time is no longer available. No appointment has been booked. <a href='"+back+"'>Choose another time</a>.")).ToString();
                    return b.Append(Panel("Review your appointment","<p>"+H(HospitalTime.Format(chosen))+" to "+H(HospitalTime.Format(Convert.ToDateTime(selection["SlotEnd"])))+" · "+H(Convert.ToString(selection["SlotMinutes"]))+" minutes</p><p>This time is not reserved until you confirm. We check availability again when you book.</p>"+Form("book","<input type='hidden' name='doctorId' value='"+doctor+"'><input type='hidden' name='scheduleId' value='"+selection["DoctorScheduleId"]+"'><input type='hidden' name='start' value='"+HospitalTime.Token(chosen)+"'>"+Field("Reason for visit (optional, maximum 500 characters)","reason","text",false),"Confirm appointment")+"<a href='"+back+"'>Choose a different time</a>")).ToString();
                }
                b.Append(Panel("Choose a date","<form method='get' class='row g-3'><input type='hidden' name='doctor' value='"+doctor+"'>"+Input("Appointment date ("+HospitalTime.Zone.Id+")","date",selected.ToString("yyyy-MM-dd"),"date")+"<div class='col-12'><button class='btn btn-outline-primary'>Show available times</button></div></form><p><a href='MedicalTeam.aspx?doctor="+doctor+"'>Next available date</a> · <a href='MedicalTeam.aspx?doctor="+doctor+"&amp;date="+selected.AddDays(1).ToString("yyyy-MM-dd")+"'>Following day</a></p>"));
                if(slots.Rows.Count==0)return b.Append(Panel("Available times","No available appointments on this date. Choose the next available date, <a href='MedicalTeam.aspx'>try another doctor</a>, or contact reception for scheduling help. <a href='../Support.aspx'>Technical support</a> cannot confirm appointments.")).ToString();
                var list=new StringBuilder();
                foreach(DataRow slot in slots.Rows){
                    DateTime start=Convert.ToDateTime(slot["SlotStart"]),end=Convert.ToDateTime(slot["SlotEnd"]);
                    list.Append("<li><p><a class='btn btn-outline-primary' href='").Append(back).Append("&amp;schedule=").Append(slot["DoctorScheduleId"]).Append("&amp;slot=").Append(HospitalTime.Token(start)).Append("'>Review ").Append(H(HospitalTime.Format(start))).Append("</a> · ").Append(H(Convert.ToString(slot["SlotMinutes"]))).Append(" minutes</p></li>");
                }
                return b.Append(Panel("Available appointments","<ul>"+list+"</ul>")).ToString();
            }
            string name=Request.QueryString["name"]??"",specialty=Request.QueryString["specialty"]??"";int department=Number("department");
            var all=Workflows.FindMedicalTeam(CurrentUser.UserId);
            var options=new StringBuilder("<div class='col-md-4'><label class='form-label'>Specialty<select class='form-select' name='specialty'><option value=''>All specialties</option>");
            var seen=new System.Collections.Generic.HashSet<string>();
            foreach(DataRow row in all.Rows){string s=Convert.ToString(row["Specialty"]);if(seen.Add(s))options.Append("<option value='").Append(H(s)).Append(s==specialty?"' selected>":"'>").Append(H(s)).Append("</option>");}
            options.Append("</select></label></div><div class='col-md-4'><label class='form-label'>Department<select class='form-select' name='department'><option value=''>All departments</option>");
            foreach(DataRow row in Db.Query("SELECT DepartmentId,DepartmentName FROM gh.Departments WHERE IsActive=1 ORDER BY DepartmentName").Rows)options.Append("<option value='").Append(row[0]).Append(Convert.ToInt32(row[0])==department?"' selected>":"'>").Append(H(Convert.ToString(row[1]))).Append("</option>");
            options.Append("</select></label></div>");
            b.Append(Panel("Search medical team","<form method='get' class='row g-3'>"+Input("Doctor name","name",name)+options+Input("Available on (optional, "+HospitalTime.Zone.Id+")","date",dateText,"date")+"<div class='col-12'><button class='btn btn-primary'>Search doctors</button> <a href='MedicalTeam.aspx'>Clear filters</a></div></form>"));
            if(name.Length>100||specialty.Length>50)return b.Append(Panel("Check search","Shorten the name or specialty and search again.")).ToString();
            var results=Workflows.FindMedicalTeam(CurrentUser.UserId,name,specialty,department>0?(int?)department:null,date,displayZone:HospitalTime.Zone);
            if(results.Rows.Count==0)return b.Append(Panel("Medical team","No doctors match these filters. Try another date or contact reception.")).ToString();
            foreach(DataRow r in results.Rows){
                string url="MedicalTeam.aspx?doctor="+r["DoctorId"]+(date.HasValue?"&amp;date="+date.Value.ToString("yyyy-MM-dd"):"");
                b.Append(Panel(Convert.ToString(r["DoctorName"]),"<p>"+H(Convert.ToString(r["Specialty"]))+" · "+H(r["DepartmentName"]==DBNull.Value?"Department not assigned":Convert.ToString(r["DepartmentName"]))+"</p><p>"+(r["NextAvailable"]==DBNull.Value?"No available published appointments":("Next available: "+H(HospitalTime.Format(Convert.ToDateTime(r["NextAvailable"])))))+"</p><a class='btn btn-outline-primary' href='"+url+"'>View profile and times</a>"));
            }
            return b.ToString();
        }
    }
}
