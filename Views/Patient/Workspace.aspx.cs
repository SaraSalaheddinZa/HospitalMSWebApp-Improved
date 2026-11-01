using System;
using HospitalMSWebApp.Infrastructure;
namespace HospitalMSWebApp.Views.Patient
{
    public class Workspace : WorkspacePage
    {
        protected override string Role { get { return "Patient"; } }
        protected override string Heading { get { return "My health"; } }
        private string HealthSection(string title,string body){return "<details class='gh-panel gh-disclosure' id='"+GuardianUi.Slug(title)+"'><summary><h2>"+H(title)+"</h2><span>View details</span></summary><div class='card-body'>"+body+"</div></details>";}
        private int PatientId { get { if(!CurrentUser.PatientId.HasValue)throw new InvalidOperationException("Your patient record is not linked. Contact reception.");return CurrentUser.PatientId.Value; } }
        protected override void Submit()
        {
            throw new ArgumentException("Open the appointment details or inbox to make changes. This old form is no longer available.");
        }
        protected override string Content()
        {
            if(!CurrentUser.PatientId.HasValue)return Panel("Account needs attention","Contact reception to link your patient record.");
            var reception=Convert.ToString(Db.Scalar("SELECT SettingValue FROM gh.SystemSettings WHERE SettingKey='ReceptionContact'"));
            return "<section class='gh-dashboard-welcome'><div><h2>Welcome, "+H(CurrentUser.Name)+".</h2><p>Stay connected with your care. Your latest appointments, records and messages are below.</p></div><span class='gh-welcome-symbol' aria-hidden='true'>+</span></section><div class='row'>"+Metric("Upcoming appointments","SELECT COUNT(*) FROM gh.Appointments WHERE PatientId=@P AND ScheduledStart>=SYSUTCDATETIME() AND Status NOT IN('Cancelled','NoShow','Completed')",P("@P",PatientId))
                +Metric("Unread notifications","SELECT COUNT(*) FROM gh.Notifications WHERE UserId=@U AND ReadAt IS NULL",P("@U",CurrentUser.UserId))
                +Metric("Outstanding balance","SELECT ISNULL(SUM(TotalAmount-PaidAmount),0) FROM gh.Invoices WHERE PatientId=@P AND Status NOT IN('Draft','Voided')",P("@P",PatientId))+"</div>"
                +HealthSection("My profile","<p>Contact reception to update your details. "+H(reception)+"</p>"+Table(Db.Query("SELECT p.MedicalRecordNumber AS MRN,p.patientName AS Name,p.patientPhone AS Phone,pp.Email,pp.BloodType AS [Blood type],p.patientAllergies AS Allergies FROM dbo.patientTbl p LEFT JOIN gh.PatientProfiles pp ON pp.PatientId=p.patientId WHERE p.patientId=@P",P("@P",PatientId))))
                +Dashboard()
                +HealthSection("My medical timeline",Table(Db.Query("SELECT TOP 100 EventAt AS Date,EventType AS Event,Detail FROM gh.vwPatientTimeline WHERE PatientId=@P ORDER BY EventAt DESC",P("@P",PatientId))))
                +HealthSection("Prescriptions",Table(Db.Query("SELECT m.MedicationName AS Medication,i.Dose,i.Frequency,i.Quantity,o.Status,o.PrescribedAt FROM gh.PrescriptionItems i JOIN gh.PrescriptionOrders o ON o.PrescriptionOrderId=i.PrescriptionOrderId JOIN gh.MedicationCatalog m ON m.MedicationId=i.MedicationId WHERE o.PatientId=@P ORDER BY o.PrescribedAt DESC",P("@P",PatientId))))
                +HealthSection("Laboratory results",Table(Db.Query("SELECT c.TestName AS Test,i.ResultValue AS Result,i.ResultUnit AS Unit,i.ReferenceRange,i.ResultFlag AS Flag,i.ResultedAt AS OriginallyFinalized,i.Revision AS Corrections,i.AmendedAt AS LastCorrection FROM gh.vwLabResults i JOIN gh.LabOrders o ON o.LabOrderId=i.LabOrderId JOIN gh.LabCatalog c ON c.LabTestId=i.LabTestId WHERE o.PatientId=@P AND i.ResultStatus='Final' ORDER BY i.ResultedAt DESC",P("@P",PatientId))))
                +HealthSection("Invoices",Table(Db.Query("SELECT InvoiceNumber,TotalAmount,PaidAmount,TotalAmount-PaidAmount AS Balance,Status,DueDate FROM gh.Invoices WHERE PatientId=@P ORDER BY CreatedAt DESC",P("@P",PatientId))))
                +HealthSection("Payments",Table(Db.Query("SELECT i.InvoiceNumber,p.Amount,p.Method,p.ReferenceNumber,p.ReceivedAt FROM gh.Payments p JOIN gh.Invoices i ON i.InvoiceId=p.InvoiceId WHERE i.PatientId=@P ORDER BY p.ReceivedAt DESC",P("@P",PatientId))))
                +HealthSection("Insurance coverage and claims","<p>Recorded coverage is subject to your insurer's terms. Claims shown here are managed by the hospital billing team.</p>"+Table(Db.Query("SELECT v.ProviderName,p.PolicyNumber,p.CoveragePercent,p.ValidFrom,p.ValidTo,CASE WHEN p.IsActive=1 AND v.IsActive=1 AND (p.ValidFrom IS NULL OR p.ValidFrom<=CAST(SYSUTCDATETIME() AS date)) AND (p.ValidTo IS NULL OR p.ValidTo>=CAST(SYSUTCDATETIME() AS date)) THEN 'Eligible' ELSE 'Ineligible' END AS Status FROM gh.InsurancePolicies p JOIN gh.InsuranceProviders v ON v.InsuranceProviderId=p.InsuranceProviderId WHERE p.PatientId=@P",P("@P",PatientId)))+Table(Db.Query("SELECT i.InvoiceNumber,c.ClaimNumber,c.ClaimedAmount,c.ApprovedAmount,c.Status,c.DecisionNote,c.SubmittedAt,c.SettledAt FROM gh.InsuranceClaims c JOIN gh.Invoices i ON i.InvoiceId=c.InvoiceId WHERE i.PatientId=@P ORDER BY c.SubmittedAt DESC",P("@P",PatientId))))
                +HealthSection("Notifications","<p><a href='../Notifications/Index.aspx'>Open your inbox to read and manage messages</a>.</p>");
        }
        private string Dashboard()
        {
            string next="<p>You have no upcoming appointments.</p>";
            foreach(System.Data.DataRow row in Workflows.PatientAppointments(CurrentUser.UserId).Rows){
                if(!Appointments.IsUpcoming(row))continue;
                next=Appointments.Summary(row)+"<p><a href='Appointments.aspx?appointment="+row["AppointmentId"]+"'>View details or change this appointment</a></p>";break;
            }
            return Panel("Next appointment",next+"<p>Hospital time: "+H(HospitalTime.Zone.Id)+".</p><p><a href='Appointments.aspx'>All appointments</a> · <a href='MedicalTeam.aspx'>Find a doctor</a></p>")
                +Panel("Unread messages",Table(Db.Query("SELECT TOP 3 Subject,Message,CreatedAt AS [Received (UTC)] FROM gh.Notifications WHERE UserId=@U AND ReadAt IS NULL ORDER BY CreatedAt DESC,NotificationId DESC",P("@U",CurrentUser.UserId)))+"<a href='../Notifications/Index.aspx'>Open inbox</a>")
                +Panel("Recent test results",Table(Db.Query("SELECT TOP 3 c.TestName AS Test,i.ResultValue AS Result,i.ResultUnit AS Unit,i.ResultFlag AS Flag,i.ResultedAt AS [Finalized (UTC)] FROM gh.vwLabResults i JOIN gh.LabOrders o ON o.LabOrderId=i.LabOrderId JOIN gh.LabCatalog c ON c.LabTestId=i.LabTestId WHERE o.PatientId=@P AND i.ResultStatus='Final' ORDER BY COALESCE(i.AmendedAt,i.ResultedAt) DESC",P("@P",PatientId)))+"<p>Final results may not yet have been discussed with your doctor. Ask your care team if you need help understanding them.</p><a href='#laboratory-results'>All test results and corrections</a>")
                +Panel("Recent prescriptions",Table(Db.Query("SELECT TOP 3 m.MedicationName AS Medication,i.Dose,i.Frequency,o.Status FROM gh.PrescriptionItems i JOIN gh.PrescriptionOrders o ON o.PrescriptionOrderId=i.PrescriptionOrderId JOIN gh.MedicationCatalog m ON m.MedicationId=i.MedicationId WHERE o.PatientId=@P ORDER BY o.PrescribedAt DESC,i.PrescriptionItemId DESC",P("@P",PatientId)))+"<p>These are recorded prescriptions, not a confirmed list of medicines you currently take.</p><a href='#prescriptions'>Prescription history</a>")
                +Panel("Your health information","<p><a href='../Records/Index.aspx'>Medical record and care journey</a> · <a href='../Documents/Index.aspx'>Documents</a> · <a href='#invoices'>Invoices and payments</a> · <a href='#my-profile'>My profile</a></p>");
        }
    }
}
