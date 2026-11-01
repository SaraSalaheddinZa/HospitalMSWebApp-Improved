using System;
using HospitalMSWebApp.Infrastructure;
namespace HospitalMSWebApp.Views.Doctors
{
    public class Results:WorkspacePage
    {
        protected override string Role{get{return "Doctor";}}
        protected override string Heading{get{return "Laboratory review";}}
        protected override void Submit()
        {
            int version;if(!int.TryParse(V("resultVersion"),out version)||version<0)throw new ArgumentException("Open the current results before recording review.");
            var id=Id("orderId");Workflows.ReviewLab(id,CurrentUser.RoleEntityId.GetValueOrDefault(),CurrentUser.UserId,version);Audit("LabReviewed","LabOrder",id);
        }
        protected override string Content()
        {
            var doctor=CurrentUser.RoleEntityId.GetValueOrDefault();
            string review="<form method='get' action='#acknowledge-clinical-review' class='row g-3'>"+Options("Completed order awaiting review","reviewOrder","SELECT o.LabOrderId,p.MedicalRecordNumber+' · '+p.patientName+' · '+CONVERT(varchar(16),o.OrderedAt,120) FROM gh.LabOrders o JOIN dbo.patientTbl p ON p.patientId=o.PatientId WHERE o.OrderedByDoctorId=@D AND o.Status='Completed' AND o.ReviewedAt IS NULL",P("@D",doctor))+"<div class='col-12'><button class='btn btn-outline-primary'>Open current results</button></div></form>";
            long order;if(long.TryParse(Request.QueryString["reviewOrder"],out order)&&order>0){
                var results=Workflows.GetLabReviewSnapshot(order,doctor,CurrentUser.UserId);
                if(results.Rows.Count==0)review+="<p role='status'>No unreviewed completed order is available for this selection.</p>";
                else{
                    int version=Convert.ToInt32(results.Rows[0]["ResultVersion"]),patient=Convert.ToInt32(results.Rows[0]["PatientId"]);
                    results.Columns.Remove("ResultVersion");results.Columns.Remove("PatientId");
                    review+=Table(results)+"<h3>Correction history for this patient</h3>"+Table(Workflows.GetLabAmendmentHistory(patient,CurrentUser.UserId));
                    review+=Form("review","<input type='hidden' name='orderId' value='"+order+"'><input type='hidden' name='resultVersion' value='"+version+"'><p>Review the current results and any corrections above. If the laboratory changes a result before submission, reopen this order to review the new version.</p>","Acknowledge current results");
                }
            }
            return Panel("My laboratory orders",Table(Db.Query(@"SELECT o.LabOrderId AS [Order],p.MedicalRecordNumber AS MRN,p.patientName AS Patient,c.TestName AS Test,i.ResultValue AS Result,i.ResultUnit AS Unit,i.ReferenceRange,i.ResultFlag AS Flag,i.Revision AS Corrections,i.AmendedAt AS LastCorrection,o.Status,o.ReviewedAt FROM gh.LabOrders o JOIN gh.vwLabResults i ON i.LabOrderId=o.LabOrderId JOIN dbo.patientTbl p ON p.patientId=o.PatientId JOIN gh.LabCatalog c ON c.LabTestId=i.LabTestId WHERE o.OrderedByDoctorId=@D ORDER BY o.OrderedAt DESC",P("@D",doctor))))
                +Panel("Acknowledge clinical review",review);
        }
    }
}
