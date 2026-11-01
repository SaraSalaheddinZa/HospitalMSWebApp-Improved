using System;
using HospitalMSWebApp.Infrastructure;
namespace HospitalMSWebApp.Views.Nurse
{
    public class Emergency:WorkspacePage
    {
        protected override string Role { get{return "Nurse";} }
        protected override string Heading { get{return "Emergency & triage";} }
        protected override void Submit()
        {
            if(V("action")=="assess"){
                var id=Workflows.RecordTriage((int)Id("patientId"),(int)Id("acuity"),Required("complaint"),CurrentUser.UserId);
                Audit("TriageRecorded","TriageAssessment",id);
            }else if(V("action")=="progress"){
                var id=Id("triageId");Workflows.ProgressTriage(id,Required("status"),CurrentUser.UserId);Audit("TriageProgressed","TriageAssessment",id);
            }else throw new ArgumentException("Unknown action.");
        }
        protected override string Content()
        {
            return "<div class='row'>"+Metric("Waiting","SELECT COUNT(*) FROM gh.TriageAssessments WHERE Status='Waiting'")+Metric("Under care","SELECT COUNT(*) FROM gh.TriageAssessments WHERE Status='UnderCare'")+Metric("Acuity 1–2 active","SELECT COUNT(*) FROM gh.TriageAssessments WHERE AcuityLevel<=2 AND Status<>'Resolved'")+"</div>"
                +Panel("Emergency queue",Table(Db.Query("SELECT p.MedicalRecordNumber AS MRN,p.patientName AS Patient,t.AcuityLevel AS Acuity,t.ChiefComplaint AS Complaint,t.AssessedAt,t.Status,a.AdmittedAt AS InpatientHandoff,w.WardName AS Ward,r.RoomNumber AS Room,b.BedNumber AS Bed FROM gh.TriageAssessments t JOIN dbo.patientTbl p ON p.patientId=t.PatientId LEFT JOIN gh.Admissions a ON a.TriageAssessmentId=t.TriageAssessmentId AND a.DischargedAt IS NULL LEFT JOIN gh.Beds b ON b.BedId=a.BedId LEFT JOIN gh.Rooms r ON r.RoomId=b.RoomId LEFT JOIN gh.Wards w ON w.WardId=r.WardId WHERE t.Status<>'Resolved' ORDER BY t.AcuityLevel,t.AssessedAt")))
                +Panel("Record assessment",Form("assess",PatientOptions()+Choice("Clinician-assigned acuity","acuity","1","2","3","4","5")+Field("Chief complaint","complaint"),"Record assessment"))
                +Panel("Progress care",Form("progress",Options("Assessment","triageId","SELECT t.TriageAssessmentId,p.patientName+' · acuity '+CONVERT(varchar(2),t.AcuityLevel)+' · '+t.Status FROM gh.TriageAssessments t JOIN dbo.patientTbl p ON p.patientId=t.PatientId WHERE t.Status<>'Resolved' ORDER BY t.AcuityLevel,t.AssessedAt")+Choice("Next state","status","UnderCare","Resolved"),"Update"))
                +Panel("Recent resolved assessments",Table(Db.Query("SELECT TOP 50 t.TriageAssessmentId AS ID,p.patientName AS Patient,t.AcuityLevel,t.ChiefComplaint,t.AssessedAt FROM gh.TriageAssessments t JOIN dbo.patientTbl p ON p.patientId=t.PatientId WHERE t.Status='Resolved' ORDER BY t.AssessedAt DESC")));
        }
    }
}
