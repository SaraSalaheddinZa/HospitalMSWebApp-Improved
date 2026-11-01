using System;
using HospitalMSWebApp.Infrastructure;
namespace HospitalMSWebApp.Views.Search
{
    public class Index:WorkspacePage
    {
        protected override string Role {get{return CurrentUser==null?"":CurrentUser.Role;}}
        protected override string Heading {get{return "Hospital search";}}
        protected override void OnLoad(EventArgs e){if(!RequireRoles("Admin","Receptionist","Doctor","Nurse","Laboratorian","Pharmacist"))return;base.OnLoad(e);}
        protected override void Submit(){throw new ArgumentException("This page is read-only.");}
        protected override string Content()
        {
            string q=(Request.QueryString["q"]??"").Trim();
            string html="<form method='get' class='input-group mb-4'><input class='form-control' type='search' name='q' aria-label='Search authorized hospital records' maxlength='100' value='"+H(q)+"' placeholder='Name, MRN, invoice or medication'><button class='btn btn-primary'>Search</button></form>";
            if(q.Length<2)return html+Panel("Search","Enter at least two characters.");
            Audit("GlobalSearch","Search",null);
            html+=Panel("Patient directory",Table(Db.Query("SELECT TOP 50 MedicalRecordNumber AS MRN,patientName AS Patient,patientPhone AS Phone FROM dbo.patientTbl WHERE patientName LIKE '%'+@Q+'%' OR MedicalRecordNumber LIKE '%'+@Q+'%' ORDER BY patientName",P("@Q",q))));
            if(Role=="Admin"||Role=="Receptionist")html+=Panel("Invoices",Table(Db.Query("SELECT TOP 50 InvoiceNumber,TotalAmount,PaidAmount,Status FROM gh.Invoices WHERE InvoiceNumber LIKE '%'+@Q+'%' ORDER BY CreatedAt DESC",P("@Q",q))));
            if(Role=="Doctor"||Role=="Pharmacist"||Role=="Admin")html+=Panel("Medications",Table(Db.Query("SELECT TOP 50 MedicationCode,MedicationName,Form,Strength FROM gh.MedicationCatalog WHERE MedicationName LIKE '%'+@Q+'%' OR MedicationCode LIKE '%'+@Q+'%' ORDER BY MedicationName",P("@Q",q))));
            if(Role=="Doctor"||Role=="Laboratorian"||Role=="Admin")html+=Panel("Laboratory catalog",Table(Db.Query("SELECT TOP 50 TestCode,TestName,SpecimenType,ReferenceRange FROM gh.LabCatalog WHERE TestName LIKE '%'+@Q+'%' OR TestCode LIKE '%'+@Q+'%'",P("@Q",q))));
            return html;
        }
    }
}
