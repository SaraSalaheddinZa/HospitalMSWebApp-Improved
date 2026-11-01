using System;
using System.Data;
using System.Data.SqlClient;
namespace HospitalMSWebApp.Services
{
    public sealed partial class HospitalWorkflowService
    {
        public DataTable GetPatientJourney(int patient,int actor)
        {
            return database.InTransaction((c,t)=>{
                RequirePatientRead(c,t,patient,actor);
                return ReadTable(c,t,@"SELECT 'Reception' AS Department,COUNT(*) AS OutstandingActions,'Appointment confirmation, attendance or rescheduling' AS NextStep FROM gh.Appointments WHERE PatientId=@P AND Status IN('Booked','Confirmed','RescheduleRequested','CheckedIn','InProgress')
                    UNION ALL SELECT 'Consultation',COUNT(*),'Clinician to complete and sign the consultation' FROM gh.Encounters WHERE PatientId=@P AND Status='Open'
                    UNION ALL SELECT 'Laboratory',COUNT(*),'Sample collection or laboratory processing' FROM gh.LabOrders WHERE PatientId=@P AND Status IN('Ordered','Collected','Processing')
                    UNION ALL SELECT 'Result review',COUNT(*),'Ordering clinician to review completed results' FROM gh.LabOrders WHERE PatientId=@P AND Status='Completed' AND ReviewedAt IS NULL
                    UNION ALL SELECT 'Pharmacy',COUNT(*),'Pharmacy to dispense the remaining prescription' FROM gh.PrescriptionOrders WHERE PatientId=@P AND Status IN('Pending','PartiallyDispensed')
                    UNION ALL SELECT 'Billing',COUNT(*),'Outstanding invoice: patient payment or insurance settlement' FROM gh.Invoices WHERE PatientId=@P AND Status IN('Open','PartiallyPaid') AND TotalAmount>PaidAmount
                    UNION ALL SELECT 'Inpatient care',COUNT(*),'Ongoing inpatient care and discharge planning' FROM gh.Admissions WHERE PatientId=@P AND DischargedAt IS NULL",P("@P",patient));
            });
        }
        private static void RequirePatientRead(SqlConnection c,SqlTransaction t,int patient,int actor)
        {
            RequireActor(c,t,actor,"Doctor","Nurse","Patient");
            var account=QueryOne(c,t,"SELECT RoleName,PatientId FROM gh.UserAccounts WHERE UserId=@U",P("@U",actor));
            if(Convert.ToString(account["RoleName"])=="Patient"&&Convert.ToInt32(account["PatientId"])!=patient)throw new InvalidOperationException("This patient record is not accessible.");
        }
        public DataTable GetPatientEvents(int patient,int actor,string eventType=null,DateTime? from=null,DateTime? through=null)
        {
            if((eventType??"").Length>40||from<System.Data.SqlTypes.SqlDateTime.MinValue.Value||through<System.Data.SqlTypes.SqlDateTime.MinValue.Value||through>=DateTime.MaxValue.Date||through<from)throw new ArgumentException("Choose a valid event type and date range from 1753 onward.");
            return database.InTransaction((c,t)=>{
                RequirePatientRead(c,t,patient,actor);
                return ReadTable(c,t,@"SELECT TOP 200 e.EventAt,e.EventType,e.Detail,COALESCE(u.DisplayName,'Not recorded') AS RecordedBy FROM gh.vwPatientEvents e LEFT JOIN gh.UserAccounts u ON u.UserId=e.ActorUserId WHERE e.PatientId=@P AND (@Type IS NULL OR e.EventType=@Type) AND (@From IS NULL OR e.EventAt>=@From) AND (@To IS NULL OR e.EventAt<@To) ORDER BY e.EventAt DESC,e.EventType,e.Detail",P("@P",patient),P("@Type",string.IsNullOrWhiteSpace(eventType)?null:eventType),P("@From",from),P("@To",through.HasValue?(object)through.Value.Date.AddDays(1):null));
            });
        }
        public DataTable GetOperationalAlerts(int actor)
        {
            return database.InTransaction((c,t)=>{
                RequireActor(c,t,actor,"Admin");
                return ReadTable(c,t,@"SELECT 'Critical results awaiting review' AS Alert,COUNT(*) AS AffectedRecords,'Any finalized critical result whose ordering clinician has not reviewed the order' AS [Rule] FROM gh.LabOrders o WHERE o.ReviewedAt IS NULL AND EXISTS(SELECT 1 FROM gh.vwLabResults i WHERE i.LabOrderId=o.LabOrderId AND i.ResultStatus='Final' AND i.ResultFlag='Critical')
                    UNION ALL SELECT 'Patients waiting over 60 minutes',COUNT(*),'Checked in more than 60 minutes ago; not yet in consultation' FROM gh.Appointments WHERE Status='CheckedIn' AND CheckInAt<DATEADD(minute,-60,SYSUTCDATETIME())
                    UNION ALL SELECT 'Laboratory orders over 24 hours',COUNT(*),'Unfinished order placed more than 24 hours ago; informational threshold, not a clinical SLA' FROM gh.LabOrders WHERE Status IN('Ordered','Collected','Processing') AND OrderedAt<DATEADD(hour,-24,SYSUTCDATETIME())
                    UNION ALL SELECT 'Low medication stock',COUNT(*),'Batch quantity at or below its recorded minimum' FROM gh.MedicationBatches WHERE QuantityOnHand<=MinimumStock
                    UNION ALL SELECT 'Expired stock on hand',COUNT(*),'Past the batch expiration date with a positive on-hand quantity' FROM gh.MedicationBatches WHERE ExpirationDate<CAST(SYSUTCDATETIME() AS date) AND QuantityOnHand>0
                    UNION ALL SELECT 'Overdue invoices',COUNT(*),'Unpaid balance with a due date before today (UTC)' FROM gh.Invoices WHERE Status IN('Open','PartiallyPaid') AND TotalAmount>PaidAmount AND DueDate<CAST(SYSUTCDATETIME() AS date)
                    UNION ALL SELECT 'Beds awaiting cleaning',COUNT(*),'Bed marked Cleaning after transfer or discharge; unavailable for assignment' FROM gh.Beds WHERE Status='Cleaning'
                    UNION ALL SELECT 'Urgent triage waiting',COUNT(*),'Recorded acuity 1 or 2 and not yet under care' FROM gh.TriageAssessments WHERE Status='Waiting' AND AcuityLevel<=2
                    UNION ALL SELECT 'Pending rescheduling requests',COUNT(*),'Patient request awaiting reception action' FROM gh.Appointments WHERE Status='RescheduleRequested'");
            });
        }
        private static DataTable ReadTable(SqlConnection c,SqlTransaction t,string sql,params SqlParameter[] parameters)
        {
            var result=new DataTable();using(var command=Command(c,t,sql,parameters))using(var adapter=new SqlDataAdapter(command))adapter.Fill(result);return result;
        }
    }
}
