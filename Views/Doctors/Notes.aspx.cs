using System;
using HospitalMSWebApp.Infrastructure;
namespace HospitalMSWebApp.Views.Doctors
{
    public class Notes:WorkspacePage
    {
        protected override string Role{get{return "Doctor";}}
        protected override string Heading{get{return "Consultations & follow-ups";}}
        protected override void Submit()
        {
            var doctor=CurrentUser.RoleEntityId.GetValueOrDefault();
            if(V("action")=="cancel-order"){
                Workflows.CancelClinicalOrder(Id("orderId"),doctor,CurrentUser.UserId,Required("kind"),Required("reason"));Audit("ClinicalOrderCancelled",V("kind"),V("orderId"));return;
            }
            var id=Id("encounterId");
            if(V("action")=="finalize"){Workflows.FinalizeEncounter(id,doctor,CurrentUser.UserId);Audit("EncounterSigned","Encounter",id);}
            else if(V("action")=="draft"){
                int revision;if(!int.TryParse(V("revision"),out revision)||revision<0)throw new ArgumentException("Reload the consultation before saving.");
                DateTime followUp;DateTime? follow=null;
                if(!string.IsNullOrWhiteSpace(V("followUp"))){if(!DateTime.TryParse(V("followUp"),out followUp))throw new ArgumentException("Choose a valid follow-up date.");follow=followUp;}
                Workflows.UpdateDraftEncounter(id,doctor,CurrentUser.UserId,revision,Required("complaint"),V("assessment"),V("plan"),follow);Audit("EncounterDraftUpdated","Encounter",id);
            }
            else if(V("action")=="addendum"){Workflows.AppendClinicalAddendum(id,doctor,CurrentUser.UserId,Required("note"));Audit("ClinicalAddendum","Encounter",id);}
            else if(V("action")=="prescription"||V("action")=="laboratory"){
                bool prescription=V("action")=="prescription";decimal quantity=0;
                if(prescription&&!decimal.TryParse(V("quantity"),System.Globalization.NumberStyles.Number,System.Globalization.CultureInfo.InvariantCulture,out quantity))throw new ArgumentException("Enter a prescribed quantity.");
                var order=Workflows.AddClinicalOrder(id,doctor,CurrentUser.UserId,prescription?"Prescription":"Laboratory",(int)Id("catalogId"),V("dose"),V("frequency"),quantity);Audit("ClinicalOrderAdded",prescription?"PrescriptionOrder":"LabOrder",order);
            }
            else throw new ArgumentException("Unknown action.");
        }
        protected override string Content()
        {
            int doctor=CurrentUser.RoleEntityId.GetValueOrDefault();
            return DraftEditor(doctor)+Panel("My consultations",Table(Db.Query("SELECT e.EncounterId AS ID,p.patientName AS Patient,e.StartedAt,e.ChiefComplaint,e.Assessment,e.TreatmentPlan,e.FollowUpDate,e.Status FROM gh.Encounters e JOIN dbo.patientTbl p ON p.patientId=e.PatientId WHERE e.DoctorId=@D ORDER BY e.StartedAt DESC",P("@D",doctor))))
                +Panel("Finalize consultation",Form("finalize",Options("Consultation","encounterId","SELECT e.EncounterId,p.patientName+' · '+CONVERT(varchar(16),e.StartedAt,120) FROM gh.Encounters e JOIN dbo.patientTbl p ON p.patientId=e.PatientId WHERE e.DoctorId=@D AND e.Status='Open'",P("@D",doctor)),"Finalize"))
                +Panel("Add prescription to consultation",Form("prescription",OpenConsultations(doctor)+Options("Medication","catalogId","SELECT MedicationId,MedicationName+' · '+ISNULL(Strength,'') FROM gh.MedicationCatalog WHERE IsActive=1 ORDER BY MedicationName")+Field("Dose","dose")+Field("Frequency","frequency")+Field("Quantity","quantity","number"),"Add prescription"))
                +Panel("Add laboratory test to consultation",Form("laboratory",OpenConsultations(doctor)+Options("Laboratory test","catalogId","SELECT LabTestId,TestName FROM gh.LabCatalog WHERE IsActive=1 ORDER BY TestName"),"Order test"))
                +Panel("Cancel an unprocessed prescription",Form("cancel-order","<input type='hidden' name='kind' value='Prescription'>"+Options("Prescription","orderId","SELECT o.PrescriptionOrderId,p.patientName+' · '+m.MedicationName+' · '+CONVERT(varchar(16),o.PrescribedAt,120) FROM gh.PrescriptionOrders o JOIN dbo.patientTbl p ON p.patientId=o.PatientId JOIN gh.PrescriptionItems i ON i.PrescriptionOrderId=o.PrescriptionOrderId JOIN gh.MedicationCatalog m ON m.MedicationId=i.MedicationId WHERE o.PrescribedByDoctorId=@D AND o.Status='Pending'",P("@D",doctor))+Field("Cancellation reason","reason"),"Cancel prescription"))
                +Panel("Cancel an uncollected laboratory order",Form("cancel-order","<input type='hidden' name='kind' value='Laboratory'>"+Options("Laboratory order","orderId","SELECT o.LabOrderId,p.patientName+' · '+c.TestName+' · '+CONVERT(varchar(16),o.OrderedAt,120) FROM gh.LabOrders o JOIN dbo.patientTbl p ON p.patientId=o.PatientId JOIN gh.LabOrderItems i ON i.LabOrderId=o.LabOrderId JOIN gh.LabCatalog c ON c.LabTestId=i.LabTestId WHERE o.OrderedByDoctorId=@D AND o.Status='Ordered'",P("@D",doctor))+Field("Cancellation reason","reason"),"Cancel laboratory order"))
                +Panel("Append an addendum",Form("addendum",Options("Finalized consultation","encounterId","SELECT e.EncounterId,p.patientName+' · '+CONVERT(varchar(16),e.StartedAt,120) FROM gh.Encounters e JOIN dbo.patientTbl p ON p.patientId=e.PatientId WHERE e.DoctorId=@D AND e.Status IN('Signed','Amended')",P("@D",doctor))+Field("Addendum","note"),"Append addendum"))
                +Panel("Addendum history",Table(Db.Query("SELECT a.EncounterId,a.Note,a.RecordedAt,u.DisplayName AS Author FROM gh.EncounterAddenda a JOIN gh.Encounters e ON e.EncounterId=a.EncounterId JOIN gh.UserAccounts u ON u.UserId=a.AuthorUserId WHERE e.DoctorId=@D ORDER BY a.RecordedAt DESC",P("@D",doctor))))
                +Panel("Draft revision history",Table(Db.Query("SELECT p.patientName AS Patient,h.Revision,h.ChiefComplaint,h.Assessment,h.TreatmentPlan,h.ChangedAt,u.DisplayName AS ChangedBy FROM gh.EncounterDraftHistory h JOIN gh.Encounters e ON e.EncounterId=h.EncounterId JOIN dbo.patientTbl p ON p.patientId=e.PatientId JOIN gh.UserAccounts u ON u.UserId=h.ChangedByUserId WHERE e.DoctorId=@D ORDER BY h.ChangedAt DESC",P("@D",doctor))))
                +Panel("Upcoming follow-ups",Table(Db.Query("SELECT p.MedicalRecordNumber AS MRN,p.patientName AS Patient,e.FollowUpDate,e.TreatmentPlan FROM gh.Encounters e JOIN dbo.patientTbl p ON p.patientId=e.PatientId WHERE e.DoctorId=@D AND e.FollowUpDate>=CAST(SYSUTCDATETIME() AS date) ORDER BY e.FollowUpDate",P("@D",doctor))));
        }
        private string DraftEditor(int doctor)
        {
            string select="<form method='get' class='row g-3'>"+Options("Open consultation","edit","SELECT e.EncounterId,p.patientName+' · '+p.MedicalRecordNumber+' · '+CONVERT(varchar(16),e.StartedAt,120) FROM gh.Encounters e JOIN dbo.patientTbl p ON p.patientId=e.PatientId WHERE e.DoctorId=@D AND e.Status='Open' ORDER BY e.StartedAt DESC",P("@D",doctor))+"<div class='col-12'><button class='btn btn-primary'>Open draft</button></div></form>";
            long id;if(!long.TryParse(Request.QueryString["edit"],out id))return Panel("Edit an open consultation",select);
            var data=Db.Query("SELECT e.*,p.patientName,p.MedicalRecordNumber,p.patientAllergies FROM gh.Encounters e JOIN dbo.patientTbl p ON p.patientId=e.PatientId WHERE e.EncounterId=@I AND e.DoctorId=@D AND e.Status='Open'",P("@I",id),P("@D",doctor));
            if(data.Rows.Count!=1)return Panel("Edit an open consultation",select+"<p>The selected draft is not available.</p>");
            var r=data.Rows[0];
            string fields="<input type='hidden' name='encounterId' value='"+id+"'><input type='hidden' name='revision' value='"+H(Convert.ToString(r["NoteRevision"]))+"'>";
            fields+=TextArea("Chief complaint","complaint",Convert.ToString(r["ChiefComplaint"]),true)+TextArea("Assessment","assessment",Convert.ToString(r["Assessment"]),false)+TextArea("Treatment plan","plan",Convert.ToString(r["TreatmentPlan"]),false);
            fields+="<div class='col-md-4'><label class='form-label'>Follow-up date<input class='form-control' type='date' name='followUp' value='"+(r["FollowUpDate"]==DBNull.Value?"":Convert.ToDateTime(r["FollowUpDate"]).ToString("yyyy-MM-dd"))+"'></label></div>";
            return Panel("Edit an open consultation",select+"<p>"+H(Convert.ToString(r["patientName"]))+" · "+H(Convert.ToString(r["MedicalRecordNumber"]))+"</p><p><strong>Allergies:</strong> "+H(Convert.ToString(r["patientAllergies"]))+"</p>"+Form("draft",fields,"Save draft"));
        }
        private string OpenConsultations(int doctor){return Options("Open consultation","encounterId","SELECT e.EncounterId,p.patientName+' · '+p.MedicalRecordNumber+' · '+CONVERT(varchar(16),e.StartedAt,120) FROM gh.Encounters e JOIN dbo.patientTbl p ON p.patientId=e.PatientId WHERE e.DoctorId=@D AND e.Status='Open' ORDER BY e.StartedAt DESC",P("@D",doctor));}
        private static string TextArea(string label,string name,string value,bool required){return "<div class='col-12'><label class='form-label'>"+H(label)+"<textarea class='form-control' rows='4' name='"+name+"' "+(required?"required":"")+">"+H(value)+"</textarea></label></div>";}
    }
}
