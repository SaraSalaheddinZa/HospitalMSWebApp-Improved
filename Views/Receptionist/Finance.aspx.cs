using System;
using System.Globalization;
using HospitalMSWebApp.Infrastructure;
namespace HospitalMSWebApp.Views.Receptionist
{
    public class Finance : WorkspacePage
    {
        protected override string Role {get{return "Receptionist";}}
        protected override string Heading {get{return "Billing & insurance";}}
        private decimal Amount(string key){decimal n;if(!decimal.TryParse(V(key),NumberStyles.Number,CultureInfo.InvariantCulture,out n))throw new ArgumentException("Enter a valid "+key+".");return n;}
        protected override void Submit()
        {
            switch(V("action")){
                case "invoice": var i=Workflows.CreateInvoice((int)Id("patientId"),Required("description"),Amount("amount"),null);Audit("InvoiceCreated","Invoice",i);break;
                case "line": var l=Workflows.AddInvoiceLine(Id("invoiceId"),Required("description"),Amount("quantity"),Amount("price"),CurrentUser.UserId);Audit("InvoiceLineAdded","InvoiceItem",l);break;
                case "payment": Workflows.TakePayment(Id("invoiceId"),Amount("amount"),Required("method"),V("reference"),CurrentUser.UserId);Audit("PaymentReceived","Invoice",V("invoiceId"));break;
                case "provider": var provider=Workflows.SaveInsuranceProvider(string.IsNullOrWhiteSpace(V("providerId"))?(int?)null:(int)Id("providerId"),Required("name"),Required("code"),V("phone"),V("email"),V("active")=="Active",CurrentUser.UserId);Audit("ProviderSaved","InsuranceProvider",provider);break;
                case "policy-status": Workflows.SetPolicyActive(Id("policyId"),V("active")=="Active",CurrentUser.UserId);Audit("PolicyStatusChanged","InsurancePolicy",V("policyId"));break;
                case "policy": var p=Workflows.AddPolicy((int)Id("patientId"),(int)Id("providerId"),Required("number"),Amount("coverage"),Date("from"),Date("to"));Audit("PolicyCreated","InsurancePolicy",p);break;
                case "claim": var c=Workflows.SubmitClaim(Id("invoiceId"),Id("policyId"),CurrentUser.UserId);Audit("ClaimSubmitted","InsuranceClaim",c);break;
                default: throw new ArgumentException("Unknown action.");
            }
        }
        private string InvoiceSelect(){return Options("Invoice","invoiceId","SELECT i.InvoiceId,i.InvoiceNumber+' · '+p.patientName+' · balance '+CONVERT(varchar(30),i.TotalAmount-i.PaidAmount) FROM gh.Invoices i JOIN dbo.patientTbl p ON p.patientId=i.PatientId WHERE i.Status IN('Open','PartiallyPaid') ORDER BY i.CreatedAt DESC");}
        protected override string Content()
        {
            return "<p><a href='Workspace.aspx'>← Reception</a></p>"
                +Panel("Invoices and balances",Table(Db.Query("SELECT i.InvoiceNumber,p.patientName AS Patient,i.TotalAmount,i.PaidAmount,i.TotalAmount-i.PaidAmount AS Balance,x.Reserved AS PendingInsurance,i.TotalAmount-i.PaidAmount-x.Reserved AS PatientPortion,i.Status FROM gh.Invoices i JOIN dbo.patientTbl p ON p.patientId=i.PatientId OUTER APPLY(SELECT ISNULL(SUM(CASE WHEN c.Status='Approved' THEN c.ApprovedAmount ELSE c.ClaimedAmount END),0) AS Reserved FROM gh.InsuranceClaims c WHERE c.InvoiceId=i.InvoiceId AND c.Status IN('Submitted','UnderReview','Approved'))x ORDER BY i.CreatedAt DESC")))
                +Panel("Create invoice",Form("invoice",PatientOptions()+Field("Description","description")+Field("Amount","amount","number"),"Create invoice"))
                +Panel("Add invoice line",Form("line",InvoiceSelect()+Field("Description","description")+Field("Quantity","quantity","number")+Field("Unit price","price","number"),"Add line"))
                +Panel("Invoice item history",Table(Db.Query("SELECT TOP 100 i.InvoiceNumber,x.Description,x.Quantity,x.UnitPrice,x.LineTotal FROM gh.InvoiceItems x JOIN gh.Invoices i ON i.InvoiceId=x.InvoiceId ORDER BY x.InvoiceItemId DESC")))
                +Panel("Record payment","<p>Record funds already received. Card and transfer payments require their transaction reference; the same reference cannot be entered twice for this invoice and method.</p>"+Form("payment",InvoiceSelect()+Field("Amount","amount","number")+Choice("Method","method","Cash","Card","Transfer")+Field("Reference (required for card or transfer)","reference","text",false),"Record payment"))
                +Panel("Insurance providers",Table(Db.Query("SELECT ProviderName,ProviderCode,Phone,Email,CASE WHEN IsActive=1 THEN 'Active' ELSE 'Inactive' END AS Status FROM gh.InsuranceProviders"))+"<p>Choose a provider to replace its contact details, or create a new provider. Inactive providers cannot receive new claims; existing claims are retained.</p>"+Form("provider",OptionalOptions("Provider","providerId","SELECT InsuranceProviderId,ProviderName FROM gh.InsuranceProviders ORDER BY ProviderName","Create new provider")+Field("Name","name")+Field("Code","code")+Field("Phone","phone","tel",false)+Field("Email","email","email",false)+Choice("Status","active","Active","Inactive"),"Save provider"))
                +Panel("Policy eligibility","<p class='text-muted'>Eligibility reflects recorded policy dates and active status. Insurer verification is handled outside this application.</p>"+Table(Db.Query(@"SELECT p.InsurancePolicyId AS ID,x.patientName AS Patient,v.ProviderName,p.PolicyNumber,p.CoveragePercent,p.ValidFrom,p.ValidTo,CASE WHEN p.IsActive=1 AND v.IsActive=1 AND (p.ValidFrom IS NULL OR p.ValidFrom<=CAST(SYSUTCDATETIME() AS date)) AND (p.ValidTo IS NULL OR p.ValidTo>=CAST(SYSUTCDATETIME() AS date)) THEN 'Eligible' ELSE 'Ineligible' END AS Status FROM gh.InsurancePolicies p JOIN dbo.patientTbl x ON x.patientId=p.PatientId JOIN gh.InsuranceProviders v ON v.InsuranceProviderId=p.InsuranceProviderId")))
                +Panel("Add policy",Form("policy",PatientOptions()+Options("Provider","providerId","SELECT InsuranceProviderId,ProviderName FROM gh.InsuranceProviders WHERE IsActive=1")+Field("Policy number","number")+Field("Coverage percent","coverage","number")+Field("Valid from","from","date")+Field("Valid to","to","date"),"Add policy"))
                +Panel("Policy availability",Form("policy-status",Options("Policy","policyId","SELECT p.InsurancePolicyId,x.patientName+' · '+p.PolicyNumber FROM gh.InsurancePolicies p JOIN dbo.patientTbl x ON x.patientId=p.PatientId")+Choice("Status","active","Active","Inactive"),"Update policy availability"))
                +Panel("Insurance record history",Table(Db.Query("SELECT TOP 100 h.EntityType,h.EntityId,h.NewStatus,h.Detail,u.DisplayName AS RecordedBy,h.OccurredAt FROM gh.WorkflowHistory h JOIN gh.UserAccounts u ON u.UserId=h.ActorUserId WHERE h.EntityType IN('InsuranceProvider','InsurancePolicy','Claim') ORDER BY h.HistoryId DESC")))
                +Panel("Record claim",Form("claim",InvoiceSelect()+Options("Policy","policyId","SELECT p.InsurancePolicyId,x.patientName+' · '+p.PolicyNumber FROM gh.InsurancePolicies p JOIN dbo.patientTbl x ON x.patientId=p.PatientId WHERE p.IsActive=1"),"Record claim"))
                +Panel("Claims",Table(Db.Query("SELECT c.ClaimNumber,i.InvoiceNumber,p.patientName AS Patient,c.ClaimedAmount,c.ApprovedAmount,c.Status,c.DecisionNote,c.SettlementReference,c.SubmittedAt FROM gh.InsuranceClaims c JOIN gh.Invoices i ON i.InvoiceId=c.InvoiceId JOIN dbo.patientTbl p ON p.patientId=i.PatientId ORDER BY c.SubmittedAt DESC")));
        }
    }
}
