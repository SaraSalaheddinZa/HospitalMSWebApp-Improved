using System;
using HospitalMSWebApp.Infrastructure;
namespace HospitalMSWebApp.Views.Pharmacist
{
    public class Suppliers : WorkspacePage
    {
        protected override string Role {get{return "Pharmacist";}}
        protected override string Heading {get{return "Suppliers & medication catalog";}}
        private decimal Money(string field){decimal value;if(!decimal.TryParse(Required(field),System.Globalization.NumberStyles.Number,System.Globalization.CultureInfo.InvariantCulture,out value))throw new ArgumentException("Enter a valid "+field+".");return value;}
        protected override void Submit()
        {
            if(V("action")=="delivery"){
                Workflows.ReceiveStock((int)Id("medicationId"),Required("batch"),Date("expires"),Money("quantity"),Money("minimum"),Money("cost"),(int)Id("supplierId"),CurrentUser.UserId,Required("reference"));Audit("SupplierDeliveryReceived","Supplier",V("supplierId"));
            }else if(V("action")=="count"){
                decimal count=Money("quantity");
                long version;if(!long.TryParse(V("movementVersion"),out version)||version<0)throw new ArgumentException("Open a batch count before reconciling.");
                Workflows.ReconcileStock(Id("batchId"),count,Required("reason"),CurrentUser.UserId,version,Money("observedQuantity"));Audit("StockReconciled","MedicationBatch",V("batchId"));
            }else if(V("action")=="supplier"){
                int? id=string.IsNullOrEmpty(V("supplierId"))?(int?)null:(int)Id("supplierId");
                var result=Workflows.SaveSupplier(id,Required("name"),V("contact"),V("phone"),V("email"),CurrentUser.UserId);Audit("SupplierSaved","Supplier",result);
            }else if(V("action")=="medication"){
                Db.Execute("INSERT INTO gh.MedicationCatalog(MedicationCode,MedicationName,Form,Strength) VALUES(@C,@N,@F,@S)",P("@C",Required("code")),P("@N",Required("name")),P("@F",Required("form")),P("@S",Required("strength")));Audit("MedicationCreated","Medication",V("code"));
            }else if(V("action")=="supplier-status"){
                Workflows.SetSupplierActive((int)Id("supplierId"),V("active")=="Active",CurrentUser.UserId);Audit("SupplierStatusChanged","Supplier",V("supplierId"));
            }else throw new ArgumentException("Unknown action.");
        }
        protected override string Content()
        {
            return "<p><a href='Workspace.aspx'>← Pharmacy</a></p>"
                +Panel("Suppliers",Table(Db.Query("SELECT SupplierName AS Name,ContactName,Phone,Email,CASE WHEN IsActive=1 THEN 'Active' ELSE 'Inactive' END AS Status FROM gh.Suppliers ORDER BY SupplierName")))
                +Panel("Supplier availability",Form("supplier-status",Options("Supplier","supplierId","SELECT SupplierId,SupplierName FROM gh.Suppliers ORDER BY SupplierName")+Choice("Status","active","Active","Inactive"),"Update supplier availability"))
                +Panel("Add or update supplier","<p class='gh-form-help'>Choose an existing supplier to replace its contact details, or leave Create new supplier selected. Review all fields before saving.</p>"+Form("supplier",OptionalOptions("Supplier","supplierId","SELECT SupplierId,SupplierName FROM gh.Suppliers ORDER BY SupplierName","Create new supplier")+Field("Name","name")+Field("Contact","contact","text",false)+Field("Telephone","phone","tel",false)+Field("Email","email","email",false),"Save supplier"))
                +Panel("Medication catalog",Table(Db.Query("SELECT MedicationCode,MedicationName,Form,Strength FROM gh.MedicationCatalog WHERE IsActive=1 ORDER BY MedicationName")))
                +Panel("Receive supplier delivery","<p>For new or existing batches, use a unique reference per supplier delivery line (for example, invoice number plus line number). Replays are rejected. An existing batch must have the same supplier and expiry; its original minimum and batch unit cost are retained. Each delivery's actual cost is recorded separately below.</p>"+Form("delivery",Options("Medication","medicationId","SELECT MedicationId,MedicationName+COALESCE(' · '+Strength,'') FROM gh.MedicationCatalog WHERE IsActive=1")+Options("Supplier","supplierId","SELECT SupplierId,SupplierName FROM gh.Suppliers WHERE IsActive=1")+Field("Delivery-line reference","reference")+Field("Batch number","batch")+Field("Expiry date (UTC)","expires","date")+Field("Quantity received","quantity","number")+Field("Minimum stock for new batch","minimum","number")+Field("Delivery unit cost","cost","number"),"Record supplier delivery"))
                +Panel("Supplier delivery history",Table(Db.Query("SELECT TOP 100 s.SupplierName,r.ReceiptReference,m.MedicationName,b.BatchNumber,r.Quantity,r.UnitCost,r.ReceivedAt,u.DisplayName AS ReceivedBy FROM gh.StockReceipts r JOIN gh.Suppliers s ON s.SupplierId=r.SupplierId JOIN gh.MedicationBatches b ON b.MedicationBatchId=r.MedicationBatchId JOIN gh.MedicationCatalog m ON m.MedicationId=b.MedicationId JOIN gh.UserAccounts u ON u.UserId=r.ReceivedByUserId ORDER BY r.StockReceiptId DESC")))
                +Panel("Add medication",Form("medication",Field("Code","code")+Field("Medication","name")+Field("Form","form")+Field("Strength","strength"),"Add medication"))
                +Panel("Reconcile physical stock count",StockCountForm())
                +Panel("Reconciliation history",Table(Db.Query("SELECT TOP 100 m.MedicationName,b.BatchNumber,h.Detail AS Reason,u.DisplayName AS RecordedBy,h.OccurredAt FROM gh.WorkflowHistory h JOIN gh.MedicationBatches b ON b.MedicationBatchId=h.EntityId JOIN gh.MedicationCatalog m ON m.MedicationId=b.MedicationId JOIN gh.UserAccounts u ON u.UserId=h.ActorUserId WHERE h.EntityType='StockCount' ORDER BY h.HistoryId DESC")))
                +Panel("Supplier history",Table(Db.Query("SELECT TOP 100 s.SupplierName,u.DisplayName AS RecordedBy,h.NewStatus,h.Detail,h.OccurredAt FROM gh.WorkflowHistory h JOIN gh.Suppliers s ON s.SupplierId=h.EntityId JOIN gh.UserAccounts u ON u.UserId=h.ActorUserId WHERE h.EntityType='Supplier' ORDER BY h.HistoryId DESC")));
        }
        private string StockCountForm()
        {
            string html="<form method='get' action='#reconcile-physical-stock-count' class='row g-3'>"+Options("Batch to count","countBatch","SELECT b.MedicationBatchId,m.MedicationName+' · '+b.BatchNumber+' · on hand '+CONVERT(varchar(30),b.QuantityOnHand) FROM gh.MedicationBatches b JOIN gh.MedicationCatalog m ON m.MedicationId=b.MedicationId")+"<div class='col-12'><button class='btn btn-primary'>Open batch count</button></div></form>";
            long batch;if(!long.TryParse(Request.QueryString["countBatch"],out batch)||batch<1)return html+"<p>Open a batch before counting. If any stock movement occurs before submission, the adjustment will be rejected and a fresh count required.</p>";
            var rows=Workflows.GetStockCountSnapshot(batch,CurrentUser.UserId);
            if(rows.Rows.Count!=1)return html+"<p>Batch not found. Choose an existing batch.</p>";
            var row=rows.Rows[0];
            return html+"<p>Counting <strong>"+H(Convert.ToString(row["MedicationName"])+" · "+Convert.ToString(row["BatchNumber"]))+"</strong>. Recorded quantity when opened: "+H(Convert.ToString(row["QuantityOnHand"]))+". Recount if stock moves during this task.</p>"+Form("count","<input type='hidden' name='batchId' value='"+batch+"'><input type='hidden' name='movementVersion' value='"+Convert.ToString(row["MovementVersion"])+"'><input type='hidden' name='observedQuantity' value='"+Convert.ToDecimal(row["QuantityOnHand"]).ToString(System.Globalization.CultureInfo.InvariantCulture)+"'>"+Field("Counted quantity","quantity","number")+Field("Reason for adjustment","reason"),"Reconcile");
        }
    }
}
