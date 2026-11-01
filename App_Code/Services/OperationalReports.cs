using System;
using System.Data;
namespace HospitalMSWebApp.Services
{
    public sealed partial class HospitalWorkflowService
    {
        public static string[] OperationalReportKeys {get{return new[]{"collections","invoices","appointments","laboratory","admissions","claims","emergency","stock","stock-movements","claim-decisions","clinical-signoff","receivables","patient-flow"};}}
        public static string OperationalReportTitle(string key){return ReportDefinition(key).Item1;}
        public DataTable GetOperationalReport(string key,int actor,DateTime from,DateTime through)
        {
            if(from.Date<System.Data.SqlTypes.SqlDateTime.MinValue.Value||through.Date<from.Date||through>=DateTime.MaxValue.Date)throw new ArgumentException("Use inclusive UTC dates from 1753 onward, with the end on or after the start and before 9999-12-31.");
            string sql=ReportDefinition(key).Item2;
            return database.InTransaction((c,t)=>{
                RequireActor(c,t,actor,"Admin");
                return ReadTable(c,t,sql,P("@From",from.Date),P("@To",through.Date.AddDays(1)));
            });
        }
        private static Tuple<string,string> ReportDefinition(string key)
        {
            switch(key){
                case "collections":return Tuple.Create("Collections by day and method","SELECT CAST(ReceivedAt AS date) AS Day,Method,COUNT(*) AS Payments,SUM(Amount) AS Collected FROM gh.Payments WHERE ReceivedAt>=@From AND ReceivedAt<@To GROUP BY CAST(ReceivedAt AS date),Method ORDER BY Day DESC");
                case "invoices":return Tuple.Create("Invoices created in period — current balances","SELECT Status,COUNT(*) AS Invoices,SUM(TotalAmount) AS Invoiced,SUM(PaidAmount) AS Paid,SUM(TotalAmount-PaidAmount) AS Outstanding FROM gh.Invoices WHERE CreatedAt>=@From AND CreatedAt<@To GROUP BY Status");
                case "appointments":return Tuple.Create("Appointments scheduled in period","SELECT d.doctorName AS Clinician,a.Status,COUNT(*) AS Appointments FROM gh.Appointments a JOIN dbo.doctorTbl d ON d.doctorID=a.DoctorId WHERE a.ScheduledStart>=@From AND a.ScheduledStart<@To GROUP BY d.doctorName,a.Status ORDER BY d.doctorName,a.Status");
                case "laboratory":return Tuple.Create("Laboratory orders placed in period","SELECT o.Status,COUNT(*) AS Orders,AVG(CASE WHEN o.Status='Completed' THEN CAST(DATEDIFF(minute,o.OrderedAt,x.FinishedAt) AS decimal(18,2)) END) AS AverageMinutesToFinalResult FROM gh.LabOrders o OUTER APPLY(SELECT MAX(ResultedAt) AS FinishedAt FROM gh.LabOrderItems WHERE LabOrderId=o.LabOrderId)x WHERE o.OrderedAt>=@From AND o.OrderedAt<@To GROUP BY o.Status");
                case "admissions":return Tuple.Create("Admissions started in period","SELECT Status,COUNT(*) AS Admissions,SUM(CASE WHEN TriageAssessmentId IS NOT NULL THEN 1 ELSE 0 END) AS LinkedEmergencyAdmissions,AVG(CASE WHEN DischargedAt IS NOT NULL THEN CAST(DATEDIFF(hour,AdmittedAt,DischargedAt) AS decimal(18,2)) END) AS AverageCompletedStayHours FROM gh.Admissions WHERE AdmittedAt>=@From AND AdmittedAt<@To GROUP BY Status");
                case "claims":return Tuple.Create("Insurance claims submitted in period","SELECT Status,COUNT(*) AS Claims,SUM(ClaimedAmount) AS Claimed,SUM(ISNULL(ApprovedAmount,0)) AS Approved FROM gh.InsuranceClaims WHERE SubmittedAt>=@From AND SubmittedAt<@To GROUP BY Status");
                case "emergency":return Tuple.Create("Emergency assessments in period","SELECT AcuityLevel AS Acuity,Status,COUNT(*) AS Assessments FROM gh.TriageAssessments WHERE AssessedAt>=@From AND AssessedAt<@To GROUP BY AcuityLevel,Status ORDER BY AcuityLevel");
                case "stock":return Tuple.Create("Current medication stock exceptions","SELECT m.MedicationName,b.BatchNumber,b.ExpirationDate,b.QuantityOnHand,b.MinimumStock,CASE WHEN b.ExpirationDate<CAST(SYSUTCDATETIME() AS date) THEN 'Expired' WHEN b.QuantityOnHand<=b.MinimumStock THEN 'Low stock' ELSE 'Expires within 30 days' END AS Status FROM gh.MedicationBatches b JOIN gh.MedicationCatalog m ON m.MedicationId=b.MedicationId WHERE b.QuantityOnHand<=b.MinimumStock OR (b.QuantityOnHand>0 AND b.ExpirationDate<=DATEADD(day,30,CAST(SYSUTCDATETIME() AS date))) ORDER BY b.ExpirationDate");
                case "stock-movements":return Tuple.Create("Medication movements in period","SELECT m.MedicationName,x.MovementType,COUNT(*) AS Movements,SUM(x.Quantity) AS Units FROM gh.StockMovements x JOIN gh.MedicationBatches b ON b.MedicationBatchId=x.MedicationBatchId JOIN gh.MedicationCatalog m ON m.MedicationId=b.MedicationId WHERE x.RecordedAt>=@From AND x.RecordedAt<@To GROUP BY m.MedicationName,x.MovementType ORDER BY m.MedicationName,x.MovementType");
                case "claim-decisions":return Tuple.Create("Insurance decision turnaround","SELECT p.ProviderName,c.Status,COUNT(*) AS Decisions,AVG(CAST(DATEDIFF(minute,c.SubmittedAt,c.DecidedAt) AS decimal(18,2))) AS AverageMinutesToDecision,SUM(CASE WHEN c.SettledAt IS NOT NULL THEN c.ApprovedAmount ELSE 0 END) AS SettledAmount FROM gh.InsuranceClaims c JOIN gh.InsurancePolicies i ON i.InsurancePolicyId=c.InsurancePolicyId JOIN gh.InsuranceProviders p ON p.InsuranceProviderId=i.InsuranceProviderId WHERE c.DecidedAt>=@From AND c.DecidedAt<@To GROUP BY p.ProviderName,c.Status ORDER BY p.ProviderName,c.Status");
                case "clinical-signoff":return Tuple.Create("Clinical sign-off accountability","SELECT d.doctorName AS Clinician,e.Status,COUNT(*) AS Consultations,AVG(CASE WHEN e.ClosedAt IS NOT NULL THEN CAST(DATEDIFF(minute,e.StartedAt,e.ClosedAt) AS decimal(18,2)) END) AS AverageMinutesToSign FROM gh.Encounters e JOIN dbo.doctorTbl d ON d.doctorID=e.DoctorId WHERE e.StartedAt>=@From AND e.StartedAt<@To GROUP BY d.doctorName,e.Status ORDER BY d.doctorName,e.Status");
                case "receivables":return Tuple.Create("Current receivables aging","SELECT x.AgeBand,COUNT(*) AS Invoices,SUM(i.TotalAmount-i.PaidAmount) AS Outstanding FROM gh.Invoices i CROSS APPLY(SELECT CASE WHEN i.DueDate IS NULL THEN 'No due date recorded' WHEN i.DueDate>=CAST(SYSUTCDATETIME() AS date) THEN 'Not overdue' WHEN DATEDIFF(day,i.DueDate,SYSUTCDATETIME())<=30 THEN '1-30 days overdue' WHEN DATEDIFF(day,i.DueDate,SYSUTCDATETIME())<=60 THEN '31-60 days overdue' WHEN DATEDIFF(day,i.DueDate,SYSUTCDATETIME())<=90 THEN '61-90 days overdue' ELSE 'Over 90 days overdue' END AS AgeBand)x WHERE i.Status IN('Open','PartiallyPaid') AND i.TotalAmount>i.PaidAmount GROUP BY x.AgeBand");
                case "patient-flow":return Tuple.Create("Recorded patient flow transitions","SELECT h.EntityType,h.PreviousStatus,h.NewStatus,COUNT(*) AS RecordedTransitions FROM gh.WorkflowHistory h WHERE h.EntityType IN('Appointment','LabOrder','PrescriptionOrder','Admission','Triage') AND h.OccurredAt>=@From AND h.OccurredAt<@To GROUP BY h.EntityType,h.PreviousStatus,h.NewStatus ORDER BY h.EntityType,h.NewStatus");
                default:throw new ArgumentException("Choose an available operational report.");
            }
        }
    }
}
