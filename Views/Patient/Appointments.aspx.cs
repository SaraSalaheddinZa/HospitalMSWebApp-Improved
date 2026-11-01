using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using HospitalMSWebApp.Infrastructure;
namespace HospitalMSWebApp.Views.Patient
{
    public class Appointments : WorkspacePage
    {
        protected override string Role { get { return "Patient"; } }
        protected override string Heading { get { return "My appointments"; } }
        protected override string SubmissionReturnUrl { get { long id;string action=V("action");return "Appointments.aspx"+(long.TryParse(V("appointmentId"),out id)&&id>0?"?appointment="+id+(Session["WorkspaceError"]!=null&&(action=="request"||action=="cancel")?"&edit="+action:""):""); } }
        protected override void Submit()
        {
            long id=Id("appointmentId");string action=V("action");
            if(action!="cancel"&&action!="request")throw new ArgumentException("Unknown appointment action.");
            var start=HospitalTime.ParseUtc(V("observedStart"));string status=V("observedStatus");
            if(string.IsNullOrEmpty(status))throw new ArgumentException("Reload the appointment before making changes.");
            Workflows.TransitionAppointment(id,action=="cancel"?"Cancelled":"RescheduleRequested",CurrentUser.UserId,null,
                action=="request"?(DateTime?)HospitalTime.ParseLocal(V("start")):null,
                action=="request"?(DateTime?)HospitalTime.ParseLocal(V("end")):null,V("note"),start,status,HospitalTime.ParseUtc(V("observedEnd")));
            Audit(action=="cancel"?"PatientAppointmentCancelled":"PatientAppointmentRequest","Appointment",id);
        }
        protected override string Content()
        {
            var b=new StringBuilder("<p><a href='Workspace.aspx'>My health</a> · <a href='MedicalTeam.aspx'>Find a doctor / book appointment</a></p><p>Hospital time: "+H(HospitalTime.Zone.Id)+". Each appointment shows its UTC offset.</p>");
            long id;
            if(Request.QueryString["appointment"]!=null){
                if(!long.TryParse(Request.QueryString["appointment"],out id)||id<=0)return b.Append(Panel("Appointment unavailable","This appointment is unavailable. <a href='Appointments.aspx'>Return to your appointments</a>.")).ToString();
                var data=Workflows.PatientAppointments(CurrentUser.UserId,id);
                if(data.Rows.Count!=1)return b.Append(Panel("Appointment unavailable","This appointment is unavailable. <a href='Appointments.aspx'>Return to your appointments</a>.")).ToString();
                var r=data.Rows[0];string status=Convert.ToString(r["Status"]);DateTime start=Convert.ToDateTime(r["ScheduledStart"]);
                if(Convert.ToString(Session["PatientBookingNotice"])==id.ToString()){Session.Remove("PatientBookingNotice");b.Append("<div class='alert alert-success' role='status'>Appointment booked. The details below are saved in your appointments.</div>");}
                b.Append(Panel("Appointment details",Summary(r)+"<p>Reference GH-APT-"+id+"</p><p>"+H(Convert.ToString(r["Reason"]))+"</p><p>Visit location and preparation instructions are not recorded here. Contact reception to confirm these details.</p><p><a href='Appointments.aspx'>All appointments</a></p>"));
                bool eligible=start>DateTime.UtcNow&&(status=="Booked"||status=="Confirmed"||status=="RescheduleRequested");
                if(status=="RescheduleRequested")b.Append(Panel("Rescheduling request pending","<p>Your original appointment remains reserved. Reception must confirm a new time. Sending a request does not book the preferred time. You will receive an inbox update when reception acts.</p>"));
                if(eligible){
                    string action=Request.QueryString["edit"];
                    string hidden="<input type='hidden' name='appointmentId' value='"+id+"'><input type='hidden' name='observedStart' value='"+HospitalTime.Token(start)+"'><input type='hidden' name='observedEnd' value='"+HospitalTime.Token(Convert.ToDateTime(r["ScheduledEnd"]))+"'><input type='hidden' name='observedStatus' value='"+H(status)+"'>";
                    if(action=="cancel")b.Append(Panel("Confirm cancellation","<p>You are cancelling the appointment shown above. Its time will become available to other patients.</p>"+Form("cancel",hidden,"Confirm cancellation")+"<a href='Appointments.aspx?appointment="+id+"'>Keep appointment</a>"));
                    else if(action=="request"&&status!="RescheduleRequested")b.Append(Panel("Request a different time","<p>Enter a preferred window in hospital time ("+H(HospitalTime.Zone.Id)+"). Reception will check availability. Your original appointment stays reserved until reception confirms a change.</p>"+Form("request",hidden+Field("Preferred start ("+HospitalTime.Zone.Id+")","start","datetime-local")+Field("Preferred end ("+HospitalTime.Zone.Id+")","end","datetime-local")+Field("Reason for request (optional, up to 700 characters)","note","text",false),"Send rescheduling request")+"<a href='Appointments.aspx?appointment="+id+"'>Keep appointment</a>"));
                    else b.Append(Panel("Manage this appointment",(status!="RescheduleRequested"?"<a class='btn btn-outline-primary' href='Appointments.aspx?appointment="+id+"&amp;edit=request'>Request a different time</a> ":"")+"<a class='btn btn-outline-primary' href='Appointments.aspx?appointment="+id+"&amp;edit=cancel'>Cancel appointment</a>"));
                }else b.Append(Panel("Appointment actions","<p>Online changes are unavailable because this appointment has started or is no longer booked. Contact reception about this appointment.</p><a href='MedicalTeam.aspx'>Book a new appointment</a>"));
                var history=Workflows.PatientAppointmentHistory(id,CurrentUser.UserId);var items=new StringBuilder();
                foreach(DataRow h in history.Rows)items.Append("<li><p>").Append(H(HospitalTime.Format(Convert.ToDateTime(h["OccurredAt"])))).Append(" · ").Append(GuardianUi.Badge(Convert.ToString(h["NewStatus"]))).Append("</p><p>").Append(H(Convert.ToString(h["Detail"]))).Append("</p></li>");
                b.Append(Panel("Appointment updates",items.Length==0?"<p>No status updates have been recorded.</p>":"<p>Request details retain the explicitly labelled time basis recorded by reception.</p><ol>"+items+"</ol>"));
                return b.ToString();
            }
            bool previous=Request.QueryString["view"]=="past";int page;if(!int.TryParse(Request.QueryString["page"],out page)||page<1||page>100000)page=1;
            var rows=Workflows.PatientAppointments(CurrentUser.UserId).Rows.Cast<DataRow>().Where(r=>IsUpcoming(r)!=previous);
            rows=previous?rows.OrderByDescending(r=>Convert.ToDateTime(r["ScheduledStart"])):rows.OrderBy(r=>Convert.ToDateTime(r["ScheduledStart"]));
            var all=rows.ToArray();var list=new StringBuilder("<p><a class='d-inline-block' href='Appointments.aspx'"+(!previous?" aria-current='page'":"")+">Upcoming appointments</a> · <a class='d-inline-block' href='Appointments.aspx?view=past'"+(previous?" aria-current='page'":"")+">Past and cancelled appointments</a></p>");
            foreach(var r in all.Skip((page-1)*20).Take(20))list.Append("<article class='gh-appointment-card'><div class='gh-appointment-detail'>").Append(Summary(r)).Append("<a href='Appointments.aspx?appointment=").Append(r["AppointmentId"]).Append("'>View appointment details</a></div></article>");
            if(all.Length==0)list.Append("<p>").Append(previous?"No past or cancelled appointments are recorded.":"You have no upcoming appointments.").Append("</p><a href='MedicalTeam.aspx'>Find a doctor</a>");
            if(page>1)list.Append("<a href='Appointments.aspx?view=").Append(previous?"past":"upcoming").Append("&amp;page=").Append(page-1).Append("'>Previous page</a> ");
            if(page*20<all.Length)list.Append("<a href='Appointments.aspx?view=").Append(previous?"past":"upcoming").Append("&amp;page=").Append(page+1).Append("'>Next page</a>");
            return b.Append(Panel(previous?"Past and cancelled appointments":"Upcoming appointments",list.ToString())).ToString();
        }
        public static bool IsUpcoming(DataRow r){string s=Convert.ToString(r["Status"]);return Convert.ToDateTime(r["ScheduledStart"])>=DateTime.UtcNow&&s!="Completed"&&s!="Cancelled"&&s!="NoShow";}
        public static string Summary(DataRow r)
        {
            var start=Convert.ToDateTime(r["ScheduledStart"]);var end=Convert.ToDateTime(r["ScheduledEnd"]);
            return "<h3>"+H(Convert.ToString(r["DoctorName"]))+"</h3><p>"+H(Convert.ToString(r["Specialty"]))+" · "+H(r["DepartmentName"]==DBNull.Value?"Department not recorded":Convert.ToString(r["DepartmentName"]))+"</p><p>"+H(HospitalTime.Format(start))+" to "+H(HospitalTime.Format(end))+" · "+(end-start).TotalMinutes.ToString("0",CultureInfo.InvariantCulture)+" minutes</p>"+GuardianUi.Badge(Convert.ToString(r["Status"]));
        }
    }
}
