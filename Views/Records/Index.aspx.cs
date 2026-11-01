using System;
using HospitalMSWebApp.Infrastructure;
namespace HospitalMSWebApp.Views.Records
{
    public class Index:WorkspacePage
    {
        protected override string Role {get{return CurrentUser==null?"":CurrentUser.Role;}}
        protected override string Heading {get{return "Medical record";}}
        protected override void OnLoad(EventArgs e){if(!RequireRoles("Doctor","Nurse","Patient"))return;base.OnLoad(e);}
        protected override void Submit(){throw new ArgumentException("This is a read-only record.");}
        protected override string Content()
        {
            int id;int.TryParse(Request.QueryString["patientId"],out id);
            if(CurrentUser.Role=="Patient")id=CurrentUser.PatientId.GetValueOrDefault();
            string selector=CurrentUser.Role=="Patient"?"":"<form method='get' class='row g-3 mb-4'>"+PatientOptions()+"<div class='col-md-3'><button class='btn btn-primary'>Open record</button></div></form>";
            if(id<1)return selector+Panel("Select patient","Choose a patient to view their medical record.");
            var person=Db.Query("SELECT p.MedicalRecordNumber AS MRN,p.patientName AS Patient,p.patientDOB AS BirthDate,p.patientAllergies AS Allergies,pp.BloodType,pp.ChronicConditions FROM dbo.patientTbl p LEFT JOIN gh.PatientProfiles pp ON pp.PatientId=p.patientId WHERE p.patientId=@P",P("@P",id));
            if(person.Rows.Count==0)return selector+Panel("Patient not found","Check the patient selection.");
            Audit("MedicalRecordViewed","Patient",id);
            string summary=GuardianUi.PatientSummary(person);
            return selector+summary
                +Panel("Care journey — outstanding actions","<p>Current recorded work across your care departments. A zero means no outstanding item is recorded for that stage; it does not replace your care team's advice.</p>"+Table(Workflows.GetPatientJourney(id,CurrentUser.UserId)))
                +Panel("Current care tasks","<p>Only existing, outstanding records are listed. Completed actions are available in the timeline; no completion is assumed when a stage has no record.</p>"+Table(Workflows.GetPatientTasks(id,CurrentUser.UserId)))
                +Panel("Appointments",Table(Db.Query("SELECT a.ScheduledStart,d.doctorName AS Clinician,a.Status,a.Reason FROM gh.Appointments a JOIN dbo.doctorTbl d ON d.doctorID=a.DoctorId WHERE a.PatientId=@P ORDER BY a.ScheduledStart DESC",P("@P",id))))
                +Panel("Consultations and care plans",Table(Db.Query("SELECT e.StartedAt,d.doctorName AS Clinician,e.ChiefComplaint,e.Assessment,e.TreatmentPlan,e.FollowUpDate,e.Status FROM gh.Encounters e JOIN dbo.doctorTbl d ON d.doctorID=e.DoctorId WHERE e.PatientId=@P ORDER BY e.StartedAt DESC",P("@P",id))))
                +Panel("Diagnoses",Table(Db.Query("SELECT d.DiagnosisName,d.Icd10Code,d.DiagnosedAt FROM gh.Diagnoses d JOIN gh.Encounters e ON e.EncounterId=d.EncounterId WHERE e.PatientId=@P ORDER BY d.DiagnosedAt DESC",P("@P",id))))
                +Panel("Observations",Table(Db.Query("SELECT v.RecordedAt,v.TemperatureC,v.Pulse,v.SystolicBp,v.DiastolicBp,v.OxygenSaturation,v.WeightKg FROM gh.VitalSigns v LEFT JOIN gh.Encounters e ON e.EncounterId=v.EncounterId WHERE v.PatientId=@P OR e.PatientId=@P ORDER BY v.RecordedAt DESC",P("@P",id))))
                +Panel("Medicines and prescriptions",Table(Db.Query("SELECT o.PrescribedAt,m.MedicationName AS Medicine,i.Dose,i.Frequency,i.Quantity,o.Status FROM gh.PrescriptionOrders o JOIN gh.PrescriptionItems i ON i.PrescriptionOrderId=o.PrescriptionOrderId JOIN gh.MedicationCatalog m ON m.MedicationId=i.MedicationId WHERE o.PatientId=@P ORDER BY o.PrescribedAt DESC",P("@P",id))))
                +Panel("Historical prescriptions","<p class='text-muted'>Original records retained from the previous prescribing workflow. Dates and dispensing details were not recorded.</p>"+Table(Db.Query("SELECT d.doctorName AS Clinician,p.medicines AS Medicines,t.testName AS Test FROM dbo.prescriptionTbl p LEFT JOIN dbo.doctorTbl d ON d.doctorID=p.doctorId LEFT JOIN dbo.labTestTbl t ON t.testId=p.testId WHERE p.patientId=@P ORDER BY p.PrescId DESC",P("@P",id))))
                +Panel("Final laboratory results",Table(Db.Query("SELECT c.TestName AS Test,i.ResultValue AS Result,i.ResultUnit AS Unit,i.ReferenceRange,i.ResultFlag AS Flag,i.ResultedAt AS OriginallyFinalized,i.Revision AS Corrections,i.AmendedAt AS LastCorrection FROM gh.vwLabResults i JOIN gh.LabOrders o ON o.LabOrderId=i.LabOrderId JOIN gh.LabCatalog c ON c.LabTestId=i.LabTestId WHERE o.PatientId=@P AND i.ResultStatus='Final' ORDER BY i.ResultedAt DESC",P("@P",id))))
                +Panel("Signed note addenda",Table(Db.Query("SELECT a.RecordedAt,u.DisplayName AS Author,a.Note FROM gh.EncounterAddenda a JOIN gh.Encounters e ON e.EncounterId=a.EncounterId JOIN gh.UserAccounts u ON u.UserId=a.AuthorUserId WHERE e.PatientId=@P ORDER BY a.RecordedAt DESC",P("@P",id))))
                +Panel("Admissions and discharge summaries",Table(Db.Query("SELECT AdmittedAt,DischargedAt,AdmissionReason,DischargeSummary,Status FROM gh.Admissions WHERE PatientId=@P ORDER BY AdmittedAt DESC",P("@P",id))))
                +Panel("Nursing care",Table(Db.Query("SELECT n.RecordedAt,n.CareStatus,n.Note,u.DisplayName AS RecordedBy FROM gh.NursingNotes n JOIN gh.UserAccounts u ON u.UserId=n.RecordedByUserId WHERE n.PatientId=@P ORDER BY n.RecordedAt DESC",P("@P",id))))
                +Panel("Emergency assessments",Table(Db.Query("SELECT t.AssessedAt,t.AcuityLevel,t.ChiefComplaint,t.Status,u.DisplayName AS AssessedBy FROM gh.TriageAssessments t LEFT JOIN gh.UserAccounts u ON u.UserId=t.AssessedByUserId WHERE t.PatientId=@P ORDER BY t.AssessedAt DESC",P("@P",id))))
                +Panel("Billing summary",Table(Db.Query("SELECT InvoiceNumber,TotalAmount,PaidAmount,TotalAmount-PaidAmount AS Outstanding,Status,DueDate FROM gh.Invoices WHERE PatientId=@P AND Status NOT IN('Draft','Voided') ORDER BY CreatedAt DESC",P("@P",id))))
                +Panel("Insurance claims",Table(Db.Query("SELECT v.ProviderName,i.InvoiceNumber,c.ClaimNumber,c.Status,c.ClaimedAmount,c.ApprovedAmount,c.DecisionNote,c.SubmittedAt,c.SettledAt FROM gh.InsuranceClaims c JOIN gh.Invoices i ON i.InvoiceId=c.InvoiceId JOIN gh.InsurancePolicies p ON p.InsurancePolicyId=c.InsurancePolicyId JOIN gh.InsuranceProviders v ON v.InsuranceProviderId=p.InsuranceProviderId WHERE i.PatientId=@P ORDER BY c.SubmittedAt DESC",P("@P",id))))
                +Panel("Laboratory correction history",Table(Workflows.GetLabAmendmentHistory(id,CurrentUser.UserId)))
                +Timeline(id);
        }
        private string Timeline(int patient)
        {
            DateTime parsed;DateTime? from=null,through=null;
            string fromText=Request.QueryString["from"]??"",toText=Request.QueryString["through"]??"",kind=Request.QueryString["eventType"]??"";
            if(fromText!=""){if(!DateTime.TryParseExact(fromText,"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out parsed))return Panel("Timeline","Enter a valid From date.");from=parsed;}
            if(toText!=""){if(!DateTime.TryParseExact(toText,"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out parsed))return Panel("Timeline","Enter a valid Through date.");through=parsed;}
            if(from<System.Data.SqlTypes.SqlDateTime.MinValue.Value||through<System.Data.SqlTypes.SqlDateTime.MinValue.Value||through<from||through>=DateTime.MaxValue.Date)return Panel("Timeline","Use a valid date range from 1753 onward.");
            string filter="<form method='get' action='#timeline' class='row g-3'><input type='hidden' name='patientId' value='"+patient+"'><div class='col-md-4'><label class='form-label'>Event type<select class='form-select' name='eventType'><option value=''>All recorded events</option>";
            foreach(System.Data.DataRow r in Db.Query("SELECT DISTINCT EventType FROM gh.vwPatientEvents WHERE PatientId=@P ORDER BY EventType",P("@P",patient)).Rows){string type=Convert.ToString(r[0]);filter+="<option value='"+H(type)+"' "+(kind==type?"selected":"")+">"+H(type)+"</option>";}
            filter+="</select></label></div><div class='col-md-3'><label class='form-label'>From (UTC)<input class='form-control' type='date' name='from' value='"+H(fromText)+"'></label></div><div class='col-md-3'><label class='form-label'>Through (UTC)<input class='form-control' type='date' name='through' value='"+H(toText)+"'></label></div><div class='col-12'><button class='btn btn-primary'>Filter timeline</button></div></form>";
            if(kind.Length>40)return Panel("Timeline",filter+"<p>Choose a recorded event type.</p>");
            return Panel("Timeline",filter+"<p>Recorded events, newest first (up to 200). Unknown historical authors remain Not recorded.</p>"+Table(Workflows.GetPatientEvents(patient,CurrentUser.UserId,kind,from,through)));
        }
    }
}
