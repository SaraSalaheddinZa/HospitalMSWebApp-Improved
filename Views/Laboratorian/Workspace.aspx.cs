using System;
using HospitalMSWebApp.Infrastructure;
namespace HospitalMSWebApp.Views.Laboratorian
{
    public class Workspace : WorkspacePage
    {
        protected override string Role { get { return "Laboratorian"; } }
        protected override string Heading { get { return "Laboratory"; } }
        protected override string SubmissionReturnUrl { get { long id;bool correction=V("action")=="amend";return "Workspace.aspx"+(long.TryParse(V(correction?"itemId":"selectedItem"),out id)&&id>0?(correction?"?correctItem="+id+"#correct-a-finalized-result":"?item="+id+"#selected-laboratory-task"):""); } }
        protected override void Submit()
        {
            if(V("action")=="stage"){var id=Id("orderId");Workflows.ProgressLab(id,Required("stage"),CurrentUser.UserId);Audit("LabStageChanged","LabOrder",id);}
            else if(V("action")=="result"){var id=Id("itemId");Workflows.FinalizeLabResult(id,Required("result"),V("unit"),Required("flag"),CurrentUser.UserId);Audit("LabResultFinalized","LabOrderItem",id);}
            else if(V("action")=="amend"){
                int revision;if(!int.TryParse(V("revision"),out revision)||revision<0)throw new ArgumentException("Open a current finalized result.");
                var id=Id("itemId");Workflows.AmendLabResult(id,revision,Required("result"),V("unit"),V("referenceRange"),Required("flag"),Required("reason"),CurrentUser.UserId);Audit("LabResultAmended","LabOrderItem",id);
            }
            else throw new ArgumentException("Unknown action.");
        }
        protected override string Content()
        {
            var q=(Request.QueryString["q"]??"").Trim();
            string stage=Request.QueryString["stage"]??"Active";
            var stages=new[]{"Active","Ordered","Collected","Processing","Completed","Cancelled","All"};
            if(Array.IndexOf(stages,stage)<0)stage="Active";
            string filter="<label>Work stage<select class='form-select' name='stage'>";
            foreach(var value in stages)filter+="<option value='"+value+"'"+(value==stage?" selected":"")+">"+(value=="Active"?"All unfinished work":value)+"</option>";
            filter+="</select></label>";
            return "<ol class='gh-lifecycle' aria-label='Laboratory workflow'><li>1 · Ordered</li><li>2 · Collected</li><li>3 · Processing</li><li>4 · Final results</li></ol><div class='row'>"+Metric("Awaiting collection","SELECT COUNT(*) FROM gh.LabOrders WHERE Status='Ordered'")+Metric("Collected","SELECT COUNT(*) FROM gh.LabOrders WHERE Status='Collected'")+Metric("Processing","SELECT COUNT(*) FROM gh.LabOrders WHERE Status='Processing'")+Metric("Completed","SELECT COUNT(*) FROM gh.LabOrders WHERE Status='Completed'")+"</div>"
                +Panel("Order worklist","<form method='get' class='d-flex flex-wrap gap-2 mb-3'><input class='form-control' name='q' aria-label='Search patients or medical record numbers' value='"+H(q)+"' placeholder='Patient name or MRN'>"+filter+"<button class='btn btn-primary'>Filter worklist</button></form><p>Up to 100 matching items, oldest orders first. Open a test to review its patient and next valid action. Stage changes apply to the whole order.</p>"+GuardianUi.Table(Db.Query(@"SELECT TOP 100 i.LabOrderItemId AS Item,p.MedicalRecordNumber AS MRN,p.patientName AS Patient,c.TestName AS Test,c.SpecimenType AS Specimen,i.ReferenceRange,i.ResultFlag AS Flag,o.Status,i.ResultStatus,o.OrderedAt
                FROM gh.LabOrders o JOIN gh.vwLabResults i ON i.LabOrderId=o.LabOrderId JOIN gh.LabCatalog c ON c.LabTestId=i.LabTestId JOIN dbo.patientTbl p ON p.patientId=o.PatientId
                WHERE (@Q='' OR p.patientName LIKE '%'+@Q+'%' OR p.MedicalRecordNumber LIKE '%'+@Q+'%') AND (@Stage='All' OR (@Stage='Active' AND o.Status IN('Ordered','Collected','Processing') AND i.ResultStatus<>'Final') OR o.Status=@Stage)
                ORDER BY o.OrderedAt,i.LabOrderItemId",P("@Q",q),P("@Stage",stage)),"Item","Workspace.aspx?item=","Open laboratory task","#selected-laboratory-task"))
                +SelectedTask()
                +Panel("Sample collection and processing","<p><a href='Workspace.aspx?stage=Ordered'>Awaiting collection</a> · <a href='Workspace.aspx?stage=Collected'>Collected orders ready for processing</a>. Open a test from the worklist to record the next stage for its order.</p>")
                +Panel("Finalize result","<p><a href='Workspace.aspx?stage=Processing'>Open the processing worklist</a>, then select a test to enter its result with patient, specimen and reference context.</p>")
                +Correction()
                +Panel("Finalized result history",Table(Db.Query(@"SELECT TOP 100 i.LabOrderItemId AS Item,p.patientName AS Patient,c.TestName AS Test,i.ResultValue,i.ResultUnit,i.ReferenceRange,i.ResultFlag,i.ResultedAt AS OriginallyFinalized,i.Revision AS Corrections,i.AmendedAt AS LastCorrection,u.DisplayName AS OriginallyRecordedBy FROM gh.vwLabResults i JOIN gh.LabOrders o ON o.LabOrderId=i.LabOrderId JOIN dbo.patientTbl p ON p.patientId=o.PatientId JOIN gh.LabCatalog c ON c.LabTestId=i.LabTestId LEFT JOIN gh.UserAccounts u ON u.UserId=i.ResultedByUserId WHERE i.ResultStatus='Final' ORDER BY i.ResultedAt DESC")));
        }
        private string SelectedTask()
        {
            long item;if(!long.TryParse(Request.QueryString["item"],out item)||item<1)return "";
            var rows=Db.Query(@"SELECT o.LabOrderId,o.Status,i.ResultStatus,p.patientName,p.MedicalRecordNumber,c.TestName,c.SpecimenType,i.ReferenceRange
                FROM gh.LabOrderItems i JOIN gh.LabOrders o ON o.LabOrderId=i.LabOrderId JOIN dbo.patientTbl p ON p.patientId=o.PatientId JOIN gh.LabCatalog c ON c.LabTestId=i.LabTestId WHERE i.LabOrderItemId=@I",P("@I",item));
            if(rows.Rows.Count!=1)return Panel("Selected laboratory task","<p>This test was not found. Refresh the worklist.</p>");
            var r=rows.Rows[0];string status=Convert.ToString(r["Status"]),result=Convert.ToString(r["ResultStatus"]);
            string body="<p><strong>"+H(Convert.ToString(r["patientName"]))+" · "+H(Convert.ToString(r["MedicalRecordNumber"]))+"</strong></p><p>"+H(Convert.ToString(r["TestName"]))+" · Specimen: "+H(Convert.ToString(r["SpecimenType"]))+" · Reference: "+H(Convert.ToString(r["ReferenceRange"]))+"</p>"+GuardianUi.Badge(status)+" "+GuardianUi.Badge(result);
            string context="<input type='hidden' name='selectedItem' value='"+item+"'>";
            if(status=="Ordered"||status=="Collected"){
                string next=status=="Ordered"?"Collected":"Processing";
                body+="<p>This action advances the whole order, including its other tests. Confirm specimen and patient identity before proceeding.</p>"+Form("stage",context+"<input type='hidden' name='orderId' value='"+r["LabOrderId"]+"'><input type='hidden' name='stage' value='"+next+"'>",next=="Collected"?"Record sample collected":"Start processing");
            }else if(status=="Processing"&&result=="Processing")body+="<p>Finalizing releases this result to the patient and notifies the care team. Subsequent corrections preserve the original.</p>"+Form("result",context+"<input type='hidden' name='itemId' value='"+item+"'>"+Field("Result value","result")+Field("Unit","unit","text",false)+Choice("Flag","flag","Normal","Low","High","Critical","Abnormal"),"Finalize result and notify care team");
            else body+="<p>No collection or result-entry action is available in this state.</p>"+(result=="Final"?"<a href='Workspace.aspx?correctItem="+item+"#correct-a-finalized-result'>Review finalized result for correction</a>":"");
            return Panel("Selected laboratory task",body);
        }
        private string Correction()
        {
            string body="<p>Corrections preserve the original result and all earlier versions. A reason is required, and the ordering clinician must review the corrected result.</p><form method='get' action='#correct-a-finalized-result' class='row g-3'>"
                +Options("Finalized test","correctItem","SELECT TOP 100 i.LabOrderItemId,p.MedicalRecordNumber+' · '+p.patientName+' · '+c.TestName+' · '+CONVERT(varchar(16),i.ResultedAt,120) FROM gh.LabOrderItems i JOIN gh.LabOrders o ON o.LabOrderId=i.LabOrderId JOIN dbo.patientTbl p ON p.patientId=o.PatientId JOIN gh.LabCatalog c ON c.LabTestId=i.LabTestId WHERE i.ResultStatus='Final' AND o.Status IN('Processing','Completed') ORDER BY i.ResultedAt DESC")+"<div class='col-12'><button class='btn btn-outline-primary'>Open result for correction</button></div></form>";
            long item;if(long.TryParse(Request.QueryString["correctItem"],out item)&&item>0){
                var snapshot=Workflows.GetLabCorrectionSnapshot(item,CurrentUser.UserId);
                if(snapshot.Rows.Count==0)body+="<p role='status'>This result is not available for correction.</p>";
                else{
                    var row=snapshot.Rows[0];
                    body+="<p><strong>"+H(Convert.ToString(row["Patient"]))+" · "+H(Convert.ToString(row["MRN"]))+" · "+H(Convert.ToString(row["Test"]))+"</strong></p>";
                    body+="<p>Current result: "+H(Convert.ToString(row["ResultValue"]))+" "+H(Convert.ToString(row["ResultUnit"]))+"; reference: "+H(Convert.ToString(row["ReferenceRange"]))+"; flag: "+H(Convert.ToString(row["ResultFlag"]))+". Correction revision: "+row["Revision"]+".</p>";
                    body+=Form("amend","<input type='hidden' name='itemId' value='"+item+"'><input type='hidden' name='revision' value='"+row["Revision"]+"'>"+Field("Corrected result value","result")+Field("Corrected unit","unit","text",false)+Field("Corrected reference range","referenceRange","text",false)+Choice("Flag","flag","Normal","Low","High","Critical","Abnormal")+Field("Reason for correction","reason"),"Save correction and notify care team");
                }
            }
            if(item>0)body=body.Replace("<option value='"+item+"'","<option value='"+item+"' selected");
            return Panel("Correct a finalized result",body)+Panel("Result correction history",Table(Db.Query(@"SELECT TOP 100 p.patientName AS Patient,c.TestName AS Test,a.Revision,a.PreviousValue,a.PreviousUnit,a.PreviousReferenceRange,a.PreviousFlag,a.ResultValue AS CorrectedValue,a.ResultUnit AS CorrectedUnit,a.ReferenceRange AS CorrectedReferenceRange,a.ResultFlag AS Flag,a.Reason,a.AmendedAt,u.DisplayName AS CorrectedBy FROM gh.LabResultAmendments a JOIN gh.LabOrderItems i ON i.LabOrderItemId=a.LabOrderItemId JOIN gh.LabOrders o ON o.LabOrderId=i.LabOrderId JOIN dbo.patientTbl p ON p.patientId=o.PatientId JOIN gh.LabCatalog c ON c.LabTestId=i.LabTestId JOIN gh.UserAccounts u ON u.UserId=a.AmendedByUserId ORDER BY a.LabResultAmendmentId DESC")));
        }
    }
}
