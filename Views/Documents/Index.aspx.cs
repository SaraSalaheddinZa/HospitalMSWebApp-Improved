using System;
using System.Data;
using System.IO;
using System.Text;
using HospitalMSWebApp.Infrastructure;
namespace HospitalMSWebApp.Views.Documents
{
    public class Index : WorkspacePage
    {
        private bool downloaded;
        protected override string Role {get{return CurrentUser==null?"":CurrentUser.Role;}}
        protected override string Heading {get{return "Patient documents";}}
        protected override void OnLoad(EventArgs e)
        {
            if(!RequireRoles("Admin","Doctor","Nurse","Receptionist","Patient"))return;
            base.OnLoad(e);
            long id;
            if(Request.HttpMethod=="GET" && long.TryParse(Request.QueryString["download"],out id))
            {
                var rows=Workflows.GetDocument(id,CurrentUser.UserId);
                if(rows.Rows.Count!=1){Response.StatusCode=404;downloaded=true;return;}
                Audit("DocumentDownloaded","Document",id);
                Response.Clear();Response.ContentType=Convert.ToString(rows.Rows[0]["ContentType"]);
                Response.AddHeader("X-Content-Type-Options","nosniff");
                Response.AddHeader("Content-Disposition","attachment; filename*=UTF-8''"+Uri.EscapeDataString(Convert.ToString(rows.Rows[0]["FileName"])));
                Response.BinaryWrite((byte[])rows.Rows[0]["Content"]);downloaded=true;
                Context.ApplicationInstance.CompleteRequest();
            }
        }
        protected override void Render(System.Web.UI.HtmlTextWriter writer){if(!downloaded)base.Render(writer);}
        protected override void Submit()
        {
            if(CurrentUser.Role!="Admin"&&CurrentUser.Role!="Doctor"&&CurrentUser.Role!="Receptionist")throw new ArgumentException("Your role cannot upload documents.");
            var file=Request.Files["document"];
            if(file==null||file.ContentLength<1||file.ContentLength>5*1024*1024)throw new ArgumentException("Choose a PDF, PNG or JPEG up to 5 MB.");
            byte[] bytes;using(var buffer=new MemoryStream()){file.InputStream.CopyTo(buffer);bytes=buffer.ToArray();}
            long id=Workflows.StoreDocument((int)Id("patientId"),file.FileName,bytes,CurrentUser.UserId);
            Audit("DocumentUploaded","Document",id);
        }
        protected override string Content()
        {
            string search=(Request.QueryString["q"]??"").Trim();bool staff=CurrentUser.Role!="Patient";
            var table=Db.Query(@"SELECT d.DocumentId,d.FileName,d.UploadedAt,p.patientName AS Patient,p.MedicalRecordNumber AS MRN FROM gh.Documents d JOIN gh.DocumentContents x ON x.DocumentId=d.DocumentId JOIN dbo.patientTbl p ON p.patientId=d.PatientId WHERE d.IsActive=1 AND (@Staff=1 OR d.PatientId=@P) AND (@Q='' OR d.FileName LIKE '%'+@Q+'%' OR p.patientName LIKE '%'+@Q+'%' OR p.MedicalRecordNumber LIKE '%'+@Q+'%') ORDER BY d.UploadedAt DESC",P("@Staff",staff),P("@P",CurrentUser.PatientId),P("@Q",search));
            var b=new StringBuilder("<form method='get' class='input-group mb-4'><input type='search' name='q' class='form-control' aria-label='Find documents' maxlength='100' placeholder='");
            b.Append(staff?"Patient name, MRN or file name":"Find a document by file name").Append("' value='").Append(H(search)).Append("'><button class='btn btn-primary'>Search</button></form><div class='gh-document-list'>");
            foreach(DataRow row in table.Rows){
                b.Append("<article class='gh-document-card'><span class='gh-document-symbol' aria-hidden='true'>▤</span><div><h3>").Append(H(Convert.ToString(row["FileName"]))).Append("</h3>");
                if(staff)b.Append("<p>").Append(H(Convert.ToString(row["Patient"])+" · "+Convert.ToString(row["MRN"]))).Append("</p>");
                b.Append("<small>Uploaded ").Append(Convert.ToDateTime(row["UploadedAt"]).ToString("dd MMM yyyy · HH:mm",System.Globalization.CultureInfo.InvariantCulture)).Append(" UTC</small></div><a class='btn btn-outline-primary' href='Index.aspx?download=").Append(row["DocumentId"]).Append("' aria-label='Download ").Append(H(Convert.ToString(row["FileName"]))).Append("'>Download</a></article>");
            }
            b.Append("</div>");if(table.Rows.Count==0)b.Append("<div class='gh-empty'><strong>No matching documents</strong><p>Documents uploaded by the care team will appear here.</p></div>");
            string result=Panel("Documents",b.ToString());
            if(CurrentUser.Role=="Admin"||CurrentUser.Role=="Doctor"||CurrentUser.Role=="Receptionist")
                result+=Panel("Upload document",Form("upload",PatientOptions()+"<div class='col-md-6'><label class='form-label'>PDF, PNG or JPEG (maximum 5 MB)<input class='form-control' type='file' name='document' accept='.pdf,.png,.jpg,.jpeg' required></label></div>","Upload").Replace("method='post'","method='post' enctype='multipart/form-data'"));
            return result;
        }
    }
}
