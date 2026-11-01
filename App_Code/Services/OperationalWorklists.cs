using System;
using System.Data;
namespace HospitalMSWebApp.Services
{
    public sealed partial class HospitalWorkflowService
    {
        public DataTable GetOperationalWorklist(string queue,int actor)
        {
            var sql=WorklistSql(queue); // Allowlisted SQL only; user input is never concatenated into SQL.
            return database.InTransaction((c,t)=>{
                RequireActor(c,t,actor,"Admin");
                return ReadTable(c,t,"SELECT TOP 200 Reference,Patient,MRN,Status,RecordedAt,ResponsibleTeam,NextAction FROM ("+sql+")w ORDER BY RecordedAt,Reference",P("@AllDates",false));
            });
        }
        public DataTable GetPatientTasks(int patient,int actor)
        {
            return database.InTransaction((c,t)=>{
                RequirePatientRead(c,t,patient,actor);
                var result=new DataTable();
                foreach(var queue in new[]{"Appointments","Clinical","Laboratory","Pharmacy","Beds","Billing","Insurance","Emergency"}){
                    var items=ReadTable(c,t,"SELECT TOP 100 @Queue AS Department,Reference,Status,RecordedAt,ResponsibleTeam,NextAction FROM ("+WorklistSql(queue)+")w WHERE PatientId=@P ORDER BY RecordedAt,Reference",P("@Queue",queue),P("@P",patient),P("@AllDates",true));
                    result.Merge(items);
                }
                return result;
            });
        }
        private static string WorklistSql(string queue)
        {
            switch(queue){
                case "Appointments":return @"SELECT a.PatientId,CONVERT(nvarchar(16),a.ScheduledStart,120)+' · '+d.doctorName AS Reference,p.patientName AS Patient,p.MedicalRecordNumber AS MRN,a.Status,a.CreatedAt AS RecordedAt,'Reception' AS ResponsibleTeam,CASE WHEN a.Status='CheckedIn' THEN 'Clinician to call patient' WHEN a.Status='InProgress' THEN 'Consultation in progress' WHEN a.Status='RescheduleRequested' THEN 'Accept a new time or decline request' WHEN a.ScheduledStart<CAST(SYSUTCDATETIME() AS date) THEN 'Past appointment: reception to reconcile attendance, reschedule or record no-show' ELSE 'Confirm attendance / check in / record no-show as appropriate' END AS NextAction FROM gh.Appointments a JOIN dbo.patientTbl p ON p.patientId=a.PatientId JOIN dbo.doctorTbl d ON d.doctorID=a.DoctorId WHERE a.Status IN('Booked','Confirmed','RescheduleRequested','CheckedIn','InProgress') AND (@AllDates=1 OR a.ScheduledStart<DATEADD(day,1,CAST(SYSUTCDATETIME() AS date)) OR a.Status IN('CheckedIn','InProgress','RescheduleRequested'))";
                case "Clinical":return @"SELECT e.PatientId,'Consultation · '+CONVERT(nvarchar(16),e.StartedAt,120) AS Reference,p.patientName AS Patient,p.MedicalRecordNumber AS MRN,e.Status,e.StartedAt AS RecordedAt,d.doctorName AS ResponsibleTeam,'Complete assessment and treatment plan, then sign' AS NextAction FROM gh.Encounters e JOIN dbo.patientTbl p ON p.patientId=e.PatientId JOIN dbo.doctorTbl d ON d.doctorID=e.DoctorId WHERE e.Status='Open'";
                case "Laboratory":return @"SELECT o.PatientId,c.TestName AS Reference,p.patientName AS Patient,p.MedicalRecordNumber AS MRN,CASE WHEN o.Status='Completed' THEN 'AwaitingReview' ELSE o.Status END AS Status,o.OrderedAt AS RecordedAt,CASE WHEN o.Status='Completed' THEN d.doctorName ELSE 'Laboratory' END AS ResponsibleTeam,CASE o.Status WHEN 'Ordered' THEN 'Collect sample' WHEN 'Collected' THEN 'Start processing' WHEN 'Processing' THEN 'Finalize result' ELSE 'Ordering clinician to acknowledge results' END AS NextAction FROM gh.LabOrders o JOIN dbo.patientTbl p ON p.patientId=o.PatientId JOIN dbo.doctorTbl d ON d.doctorID=o.OrderedByDoctorId JOIN gh.LabOrderItems i ON i.LabOrderId=o.LabOrderId JOIN gh.LabCatalog c ON c.LabTestId=i.LabTestId WHERE o.Status IN('Ordered','Collected','Processing') OR (o.Status='Completed' AND o.ReviewedAt IS NULL)";
                case "Pharmacy":return @"SELECT o.PatientId,m.MedicationName+' · remaining '+CONVERT(nvarchar(30),i.Quantity-ISNULL(x.Dispensed,0)) AS Reference,p.patientName AS Patient,p.MedicalRecordNumber AS MRN,o.Status,o.PrescribedAt AS RecordedAt,'Pharmacy' AS ResponsibleTeam,'Select an unexpired matching batch and dispense remaining quantity' AS NextAction FROM gh.PrescriptionOrders o JOIN dbo.patientTbl p ON p.patientId=o.PatientId JOIN gh.PrescriptionItems i ON i.PrescriptionOrderId=o.PrescriptionOrderId JOIN gh.MedicationCatalog m ON m.MedicationId=i.MedicationId OUTER APPLY(SELECT SUM(Quantity) AS Dispensed FROM gh.Dispensations WHERE PrescriptionItemId=i.PrescriptionItemId)x WHERE o.Status IN('Pending','PartiallyDispensed') AND i.Quantity>ISNULL(x.Dispensed,0)";
                case "Stock":return @"SELECT CAST(NULL AS int) AS PatientId,m.MedicationName+' · '+b.BatchNumber AS Reference,CAST(NULL AS nvarchar(100)) AS Patient,CAST(NULL AS nvarchar(40)) AS MRN,CASE WHEN b.ExpirationDate<CAST(SYSUTCDATETIME() AS date) THEN 'Expired' WHEN b.QuantityOnHand<=b.MinimumStock THEN 'LowStock' ELSE 'NearExpiry' END AS Status,CAST(b.ExpirationDate AS datetime2) AS RecordedAt,'Pharmacy' AS ResponsibleTeam,'Review quantity '+CONVERT(nvarchar(30),b.QuantityOnHand)+' against minimum '+CONVERT(nvarchar(30),b.MinimumStock)+'; expiry shown in date column' AS NextAction FROM gh.MedicationBatches b JOIN gh.MedicationCatalog m ON m.MedicationId=b.MedicationId WHERE b.QuantityOnHand<=b.MinimumStock OR (b.QuantityOnHand>0 AND b.ExpirationDate<=DATEADD(day,30,CAST(SYSUTCDATETIME() AS date)))";
                case "Beds":return @"SELECT a.PatientId,w.WardName+' / '+r.RoomNumber+' / '+b.BedNumber AS Reference,p.patientName AS Patient,p.MedicalRecordNumber AS MRN,b.Status,a.AdmittedAt AS RecordedAt,'Inpatient operations' AS ResponsibleTeam,CASE b.Status WHEN 'Cleaning' THEN 'Confirm cleaning before marking ready' WHEN 'Available' THEN 'Available for admission if room and ward are active' WHEN 'Occupied' THEN 'Manage admission, transfer and discharge' ELSE 'Review bed status' END AS NextAction FROM gh.Beds b JOIN gh.Rooms r ON r.RoomId=b.RoomId JOIN gh.Wards w ON w.WardId=r.WardId LEFT JOIN gh.Admissions a ON a.BedId=b.BedId AND a.DischargedAt IS NULL LEFT JOIN dbo.patientTbl p ON p.patientId=a.PatientId";
                case "Billing":return @"SELECT i.PatientId,i.InvoiceNumber+' · balance '+CONVERT(nvarchar(30),i.TotalAmount-i.PaidAmount) AS Reference,p.patientName AS Patient,p.MedicalRecordNumber AS MRN,i.Status,i.CreatedAt AS RecordedAt,'Billing / reception' AS ResponsibleTeam,'Record patient portion or settle approved insurance claim' AS NextAction FROM gh.Invoices i JOIN dbo.patientTbl p ON p.patientId=i.PatientId WHERE i.Status IN('Open','PartiallyPaid') AND i.TotalAmount>i.PaidAmount";
                case "Insurance":return @"SELECT i.PatientId,i.InvoiceNumber+' · '+c.ClaimNumber AS Reference,p.patientName AS Patient,p.MedicalRecordNumber AS MRN,c.Status,c.SubmittedAt AS RecordedAt,'Claims administration' AS ResponsibleTeam,CASE WHEN c.Status='Approved' THEN 'Record received insurer payment and remittance reference' ELSE 'Record insurer review and decision' END AS NextAction FROM gh.InsuranceClaims c JOIN gh.Invoices i ON i.InvoiceId=c.InvoiceId JOIN dbo.patientTbl p ON p.patientId=i.PatientId WHERE c.Status IN('Submitted','UnderReview','Approved')";
                case "Emergency":return @"SELECT t.PatientId,'Triage · acuity '+CONVERT(nvarchar(4),t.AcuityLevel) AS Reference,p.patientName AS Patient,p.MedicalRecordNumber AS MRN,t.Status,t.AssessedAt AS RecordedAt,'Emergency / nursing' AS ResponsibleTeam,CASE WHEN t.Status='Waiting' THEN 'Assess urgency and start care' ELSE 'Continue care; arrange admission or resolve assessment' END AS NextAction FROM gh.TriageAssessments t JOIN dbo.patientTbl p ON p.patientId=t.PatientId WHERE t.Status IN('Waiting','UnderCare')";
                default:throw new ArgumentException("Choose a supported operational worklist.");
            }
        }
    }
}
