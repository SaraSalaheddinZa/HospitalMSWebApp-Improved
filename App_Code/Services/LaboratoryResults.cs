using System;
using System.Data;
namespace HospitalMSWebApp.Services
{
    public sealed partial class HospitalWorkflowService
    {
        public DataTable GetLabCorrectionSnapshot(long item,int actor)
        {
            return database.InTransaction((c,t)=>{
                RequireActor(c,t,actor,"Laboratorian");
                return ReadTable(c,t,@"SELECT i.*,p.patientName AS Patient,p.MedicalRecordNumber AS MRN,c.TestName AS Test
                    FROM gh.vwLabResults i JOIN gh.LabOrders o ON o.LabOrderId=i.LabOrderId
                    JOIN dbo.patientTbl p ON p.patientId=o.PatientId JOIN gh.LabCatalog c ON c.LabTestId=i.LabTestId
                    WHERE i.LabOrderItemId=@I AND i.ResultStatus='Final' AND o.Status IN('Processing','Completed')",P("@I",item));
            });
        }

        public void AmendLabResult(long item,int expectedRevision,string value,string unit,string referenceRange,string flag,string reason,int actor)
        {
            value=(value??"").Trim();unit=(unit??"").Trim();referenceRange=(referenceRange??"").Trim();reason=(reason??"").Trim();
            if(expectedRevision<0||value.Length<1||value.Length>250||unit.Length>40||referenceRange.Length>250||reason.Length<1||reason.Length>700||Array.IndexOf(new[]{"Normal","Low","High","Critical","Abnormal"},flag)<0)
                throw new ArgumentException("Open a finalized result, enter valid corrected values and a correction reason (up to 700 characters).");
            database.InTransaction((c,t)=>{
                RequireActor(c,t,actor,"Laboratorian");
                // Finalization, amendment and clinician review serialize on the same order.
                var order=QueryOne(c,t,@"SELECT o.LabOrderId,o.PatientId,o.OrderedByDoctorId,o.Status FROM gh.LabOrders o WITH(UPDLOCK,HOLDLOCK)
                    JOIN gh.LabOrderItems i ON i.LabOrderId=o.LabOrderId WHERE i.LabOrderItemId=@I",P("@I",item));
                if(order==null||!new[]{"Processing","Completed"}.ContainsValue(Convert.ToString(order["Status"])))throw new InvalidOperationException("Only a finalized result in an active or completed order may be amended.");
                var current=QueryOne(c,t,"SELECT * FROM gh.vwLabResults WHERE LabOrderItemId=@I AND ResultStatus='Final'",P("@I",item));
                if(current==null)throw new InvalidOperationException("Finalize this result before recording a correction.");
                if(Convert.ToInt32(current["Revision"])!=expectedRevision)throw new InvalidOperationException("This result changed after you opened it. Reopen the current result before correcting it.");
                if(Convert.ToString(current["ResultValue"])==value&&Convert.ToString(current["ResultUnit"])==unit&&Convert.ToString(current["ReferenceRange"])==referenceRange&&Convert.ToString(current["ResultFlag"])==flag)
                    throw new InvalidOperationException("The corrected result matches the current result; no amendment was saved.");
                Execute(c,t,@"INSERT INTO gh.LabResultAmendments(LabOrderItemId,Revision,PreviousValue,PreviousUnit,PreviousReferenceRange,PreviousFlag,ResultValue,ResultUnit,ReferenceRange,ResultFlag,Reason,AmendedByUserId)
                    VALUES(@I,@Revision,@OldValue,@OldUnit,@OldRange,@OldFlag,@Value,@Unit,@Range,@Flag,@Reason,@Actor)",
                    P("@I",item),P("@Revision",checked(expectedRevision+1)),P("@OldValue",current["ResultValue"]),P("@OldUnit",current["ResultUnit"]),P("@OldRange",current["ReferenceRange"]),P("@OldFlag",current["ResultFlag"]),
                    P("@Value",value),P("@Unit",unit),P("@Range",referenceRange),P("@Flag",flag),P("@Reason",reason),P("@Actor",actor));
                Execute(c,t,"UPDATE gh.LabOrders SET ResultVersion=ResultVersion+1,ReviewedAt=NULL,ReviewedByDoctorId=NULL WHERE LabOrderId=@O",P("@O",order["LabOrderId"]));
                Execute(c,t,"INSERT INTO gh.WorkflowHistory(EntityType,EntityId,ActorUserId,PreviousStatus,NewStatus,Detail) VALUES('LabResult',@I,@U,'Final','Amended',@D)",P("@I",item),P("@U",actor),P("@D","Revision "+(expectedRevision+1)+": "+reason));
                QueuePatientNotification(c,t,Convert.ToInt32(order["PatientId"]),"Laboratory","Laboratory result corrected","A laboratory result has been corrected. The updated result and correction history are available in your medical record. Contact your care team for clinical advice.");
                Execute(c,t,@"INSERT INTO gh.Notifications(UserId,PatientId,NotificationType,Subject,Message,SentAt)
                    SELECT UserId,@P,'Laboratory',@Subject,'A laboratory result was amended. Open the current results and correction history, then record a new clinical review.',SYSUTCDATETIME()
                    FROM gh.UserAccounts WHERE RoleName='Doctor' AND LegacyUserId=@D AND IsActive=1",P("@P",order["PatientId"]),P("@D",order["OrderedByDoctorId"]),P("@Subject",flag=="Critical"?"Critical corrected laboratory result — review required":"Corrected laboratory result — review required"));
                return 0;
            });
        }

        public DataTable GetLabReviewSnapshot(long order,int doctor,int actor)
        {
            return database.InTransaction((c,t)=>{
                RequireDoctor(c,t,actor,doctor);
                return ReadTable(c,t,@"SELECT o.ResultVersion,o.PatientId,p.patientName AS Patient,p.MedicalRecordNumber AS MRN,c.TestName AS Test,i.ResultValue AS Result,i.ResultUnit AS Unit,
                    i.ReferenceRange,i.ResultFlag AS Flag,i.Revision,i.ResultedAt AS OriginallyFinalized,i.AmendedAt AS LastCorrection
                    FROM gh.LabOrders o JOIN gh.vwLabResults i ON i.LabOrderId=o.LabOrderId JOIN dbo.patientTbl p ON p.patientId=o.PatientId JOIN gh.LabCatalog c ON c.LabTestId=i.LabTestId
                    WHERE o.LabOrderId=@O AND o.OrderedByDoctorId=@D AND o.Status='Completed' AND o.ReviewedAt IS NULL",P("@O",order),P("@D",doctor));
            });
        }

        public DataTable GetLabAmendmentHistory(int patient,int actor)
        {
            return database.InTransaction((c,t)=>{
                RequirePatientRead(c,t,patient,actor);
                return ReadTable(c,t,@"SELECT TOP 100 c.TestName AS Test,a.Revision,a.PreviousValue,a.PreviousUnit,a.PreviousReferenceRange,a.PreviousFlag,
                    a.ResultValue AS CorrectedValue,a.ResultUnit AS CorrectedUnit,a.ReferenceRange AS CorrectedReferenceRange,a.ResultFlag AS Flag,a.Reason,a.AmendedAt,u.DisplayName AS CorrectedBy
                    FROM gh.LabResultAmendments a JOIN gh.LabOrderItems i ON i.LabOrderItemId=a.LabOrderItemId JOIN gh.LabOrders o ON o.LabOrderId=i.LabOrderId
                    JOIN gh.LabCatalog c ON c.LabTestId=i.LabTestId JOIN gh.UserAccounts u ON u.UserId=a.AmendedByUserId WHERE o.PatientId=@P ORDER BY a.AmendedAt DESC,a.LabResultAmendmentId DESC",P("@P",patient));
            });
        }
    }
}
