using System;
using HospitalMSWebApp.Infrastructure;
namespace HospitalMSWebApp.Views.Admin
{
    public class Operations : WorkspacePage
    {
        protected override string Role { get{return "Admin";} }
        protected override string Heading { get{return "Clinical operations";} }
        protected override void Submit()
        {
            switch(V("action")){
                case "doctorDepartment": Workflows.SetDoctorDepartment((int)Id("doctorId"),(int)Id("departmentId"),CurrentUser.UserId);Audit("DoctorDepartmentUpdated","Doctor",V("doctorId"));break;
                case "admit": var admission=Workflows.AdmitPatient((int)Id("patientId"),(int)Id("bedId"),(int)Id("doctorId"),Required("reason"),CurrentUser.UserId,V("triageId")==""?(long?)null:Id("triageId"));Audit("PatientAdmitted","Admission",admission);break;
                case "discharge": Workflows.DischargePatient(Id("admissionId"),Required("summary"));Audit("PatientDischarged","Admission",V("admissionId"));break;
                case "schedule":
                    TimeSpan start,end;
                    if(!TimeSpan.TryParse(V("startTime"),out start)||!TimeSpan.TryParse(V("endTime"),out end))throw new ArgumentException("Enter start and end times.");
                    var schedule=Workflows.AddDoctorSchedule((int)Id("doctorId"),null,Date("date"),start,end,(int)Id("minutes"));Audit("SchedulePublished","DoctorSchedule",schedule);break;
                case "catalog":
                    Db.Execute("INSERT INTO gh.LabCatalog(TestCode,TestName,SpecimenType,ReferenceRange,Unit) VALUES(@C,@N,@S,@R,@U)",P("@C",Required("code")),P("@N",Required("name")),P("@S",Required("specimen")),P("@R",V("reference")),P("@U",V("unit")));Audit("LabTestCreated","LabCatalog",V("code"));break;
                case "transfer": Workflows.TransferAdmission(Id("admissionId"),(int)Id("bedId"),CurrentUser.UserId);Audit("AdmissionTransferred","Admission",V("admissionId"));break;
                case "ready": Workflows.ReadyBed((int)Id("bedId"),CurrentUser.UserId);Audit("BedReady","Bed",V("bedId"));break;
                case "claim":
                    decimal amount;if(!decimal.TryParse(V("approved"),out amount))throw new ArgumentException("Enter an approved amount, or zero for rejection/settlement.");
                    Workflows.DecideClaim(Id("claimId"),Required("status"),amount,CurrentUser.UserId,V("decisionNote"),V("settlementReference"));Audit("ClaimDecision","InsuranceClaim",V("claimId"));break;
                default: throw new ArgumentException("Unknown action.");
            }
        }
        protected override string Content()
        {
            return "<p><a href='Workspace.aspx'>← Administration</a></p>"
                +Panel("Medical team departments",Table(Db.Query("SELECT d.doctorName AS Doctor,d.doctorSpec AS Specialty,dep.DepartmentName AS Department,d.doctorStatus AS Status FROM dbo.doctorTbl d LEFT JOIN gh.DoctorPublicProfiles p ON p.DoctorId=d.doctorId LEFT JOIN gh.Departments dep ON dep.DepartmentId=p.DepartmentId ORDER BY d.doctorName"))+"<p>Names, specialty and active status are managed in <a href='Doctors.aspx'>Doctor directory</a>. Assign a department here for patient discovery. This does not change sign-in credentials or existing appointments.</p>"+Form("doctorDepartment",Options("Doctor","doctorId","SELECT doctorId,doctorName FROM dbo.doctorTbl ORDER BY doctorName")+Options("Department","departmentId","SELECT DepartmentId,DepartmentName FROM gh.Departments WHERE IsActive=1 ORDER BY DepartmentName"),"Save department"))
                +Panel("Publish clinician availability",Form("schedule",Options("Clinician","doctorId","SELECT doctorID,doctorName FROM dbo.doctorTbl WHERE doctorStatus='Active'")+Field("Date (UTC)","date","date")+Field("Start","startTime","time")+Field("End","endTime","time")+Field("Slot minutes","minutes","number"),"Publish"))
                +Panel("Laboratory catalog",Table(Db.Query("SELECT TestCode,TestName,SpecimenType,ReferenceRange,Unit FROM gh.LabCatalog WHERE IsActive=1"))+Form("catalog",Field("Code","code")+Field("Test name","name")+Field("Specimen","specimen")+Field("Reference range","reference","text",false)+Field("Unit","unit","text",false),"Create test"))
                +Panel("Active admissions",Table(Db.Query("SELECT p.patientName AS Patient,p.MedicalRecordNumber AS MRN,w.WardName,r.RoomNumber,b.BedNumber,a.Status,a.AdmittedAt,t.AssessedAt AS LinkedEmergencyAssessment,t.Status AS EmergencyCareStatus FROM gh.Admissions a JOIN dbo.patientTbl p ON p.patientId=a.PatientId LEFT JOIN gh.Beds b ON b.BedId=a.BedId LEFT JOIN gh.Rooms r ON r.RoomId=b.RoomId LEFT JOIN gh.Wards w ON w.WardId=r.WardId LEFT JOIN gh.TriageAssessments t ON t.TriageAssessmentId=a.TriageAssessmentId WHERE a.DischargedAt IS NULL")))
                +Panel("Admit patient","<p>For an emergency admission, link the matching assessment currently under care. Nursing/clinical staff retain responsibility for resolving emergency care after handoff.</p>"+Form("admit",PatientOptions()+OptionalOptions("Emergency assessment (optional)","triageId","SELECT t.TriageAssessmentId,p.patientName+' · '+p.MedicalRecordNumber+' · acuity '+CONVERT(varchar(2),t.AcuityLevel)+' · '+CONVERT(varchar(16),t.AssessedAt,120) FROM gh.TriageAssessments t JOIN dbo.patientTbl p ON p.patientId=t.PatientId WHERE t.Status='UnderCare' AND NOT EXISTS(SELECT 1 FROM gh.Admissions a WHERE a.TriageAssessmentId=t.TriageAssessmentId)","No emergency assessment linked")+Options("Attending clinician","doctorId","SELECT doctorID,doctorName FROM dbo.doctorTbl WHERE doctorStatus='Active'")+Options("Bed","bedId","SELECT b.BedId,w.WardName+' / '+r.RoomNumber+' / '+b.BedNumber FROM gh.Beds b JOIN gh.Rooms r ON r.RoomId=b.RoomId JOIN gh.Wards w ON w.WardId=r.WardId WHERE b.Status='Available' AND r.IsActive=1 AND w.IsActive=1")+Field("Admission reason","reason"),"Admit"))
                +Panel("Discharge",Form("discharge",Options("Admission","admissionId","SELECT a.AdmissionId,p.patientName FROM gh.Admissions a JOIN dbo.patientTbl p ON p.patientId=a.PatientId WHERE a.DischargedAt IS NULL")+Field("Discharge summary","summary"),"Discharge"))
                +Panel("Transfer to an available bed",Form("transfer",Options("Admission","admissionId","SELECT a.AdmissionId,p.patientName FROM gh.Admissions a JOIN dbo.patientTbl p ON p.patientId=a.PatientId WHERE a.DischargedAt IS NULL")+Options("Destination","bedId","SELECT b.BedId,w.WardName+' / '+r.RoomNumber+' / '+b.BedNumber FROM gh.Beds b JOIN gh.Rooms r ON r.RoomId=b.RoomId JOIN gh.Wards w ON w.WardId=r.WardId WHERE b.Status='Available' AND r.IsActive=1 AND w.IsActive=1"),"Transfer"))
                +Panel("Confirm bed cleaned and ready",Form("ready",Options("Bed","bedId","SELECT b.BedId,w.WardName+' / '+r.RoomNumber+' / '+b.BedNumber FROM gh.Beds b JOIN gh.Rooms r ON r.RoomId=b.RoomId JOIN gh.Wards w ON w.WardId=r.WardId WHERE b.Status='Cleaning'"),"Mark ready"))
                +Panel("Insurance decisions and settlement","<p>Submitted → Under review → Approved or Rejected → Paid. Rejections require a reason. Mark Paid only after funds have been received and enter the payment reference.</p>"+Table(Db.Query("SELECT c.ClaimNumber,i.InvoiceNumber,p.patientName AS Patient,c.ClaimedAmount,c.ApprovedAmount,c.Status,c.DecisionNote,c.SettlementReference,c.SettledAt FROM gh.InsuranceClaims c JOIN gh.Invoices i ON i.InvoiceId=c.InvoiceId JOIN dbo.patientTbl p ON p.patientId=i.PatientId ORDER BY c.SubmittedAt DESC"))+Form("claim",Options("Claim","claimId","SELECT c.InsuranceClaimId,p.patientName+' · '+i.InvoiceNumber+' · '+c.Status FROM gh.InsuranceClaims c JOIN gh.Invoices i ON i.InvoiceId=c.InvoiceId JOIN dbo.patientTbl p ON p.patientId=i.PatientId WHERE c.Status IN('Submitted','UnderReview','Approved')")+Choice("Decision","status","UnderReview","Approved","Rejected","Paid")+Field("Approved amount (zero for review, rejection or settlement)","approved","number")+Field("Decision note (required for rejection)","decisionNote","text",false)+Field("Payment reference (required for settlement)","settlementReference","text",false),"Record decision"))
                +Panel("Operational history",Table(Db.Query("SELECT TOP 100 EntityType,EntityId,PreviousStatus,NewStatus,Detail,OccurredAt FROM gh.WorkflowHistory WHERE EntityType IN('Admission','Bed','Claim') ORDER BY HistoryId DESC")))
                +Panel("Monthly collections",Table(Db.Query("SELECT CONVERT(char(7),ReceivedAt,126) AS Month,Method,SUM(Amount) AS Collected,COUNT(*) AS Payments FROM gh.Payments GROUP BY CONVERT(char(7),ReceivedAt,126),Method ORDER BY Month DESC")))
                +Panel("Appointment outcomes",Table(Db.Query("SELECT Status,COUNT(*) AS Appointments FROM gh.Appointments WHERE ScheduledStart>=DATEADD(day,-30,SYSUTCDATETIME()) GROUP BY Status")))
                +Panel("Bed utilization",Table(Db.Query("SELECT w.WardName,b.Status,COUNT(*) AS Beds FROM gh.Beds b JOIN gh.Rooms r ON r.RoomId=b.RoomId JOIN gh.Wards w ON w.WardId=r.WardId GROUP BY w.WardName,b.Status")));
        }
    }
}
