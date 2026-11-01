using System;
using System.Data;
using System.Transactions;
using HospitalMSWebApp.Infrastructure;
using HospitalMSWebApp.Services;
class StockReconciliation
{
    static void Assert(bool ok,string message){if(!ok)throw new Exception(message);Console.WriteLine("PASS: "+message);}
    static void Count(HospitalWorkflowService service,long batch,int actor,decimal quantity,DataRow observed,string reason="QA physical count"){
        service.ReconcileStock(batch,quantity,reason,actor,Convert.ToInt64(observed["MovementVersion"]),Convert.ToDecimal(observed["QuantityOnHand"]));
    }
    static void Main()
    {
        Environment.SetEnvironmentVariable("HOSPITALMS_CONNECTIONSTRING",@"Data Source=.\SQLEXP2017;Initial Catalog=HospitalMS-Sara7593-Wasan7639;Integrated Security=True;MultipleActiveResultSets=True");
        var db=new Database();var service=new HospitalWorkflowService();
        int pharmacist=Convert.ToInt32(db.Scalar("SELECT UserId FROM gh.UserAccounts WHERE Email='dev.pharmacist@guardianhealth.test'"));
        int reception=Convert.ToInt32(db.Scalar("SELECT UserId FROM gh.UserAccounts WHERE Email='dev.reception@guardianhealth.test'"));
        foreach(string scenario in new[]{"valid","stale-receipt","net-zero-movements","stale-adjustment","replayed-count","snapshot-role","adjustment-role","wrong-quantity","negative-count","missing-reason","unchanged-count","quantity-precision"})
        using(var fixture=new TransactionScope()){
            string code="QA-"+Guid.NewGuid().ToString("N").Substring(0,12);
            int medication=Convert.ToInt32(db.Scalar("INSERT INTO gh.MedicationCatalog(MedicationCode,MedicationName) VALUES(@C,@C);SELECT CAST(SCOPE_IDENTITY() AS int)",Database.Parameter("@C",code)));
            int supplier=service.SaveSupplier(null,code,"","","",pharmacist);
            var expiry=DateTime.UtcNow.Date.AddYears(1);
            service.ReceiveStock(medication,code,expiry,10,0,1,supplier,pharmacist,"Initial");
            long batch=Convert.ToInt64(db.Scalar("SELECT MedicationBatchId FROM gh.MedicationBatches WHERE MedicationId=@M",Database.Parameter("@M",medication)));
            var original=service.GetStockCountSnapshot(batch,pharmacist).Rows[0];
            if(scenario=="valid"){
                Count(service,batch,pharmacist,7,original);
                var fresh=service.GetStockCountSnapshot(batch,pharmacist).Rows[0];
                Assert(Convert.ToDecimal(fresh["QuantityOnHand"])==7,"Fresh physical count updates on-hand quantity");
                Assert(Convert.ToInt64(fresh["MovementVersion"])>Convert.ToInt64(original["MovementVersion"]),"Reconciliation advances the observed movement version");
                Assert(Convert.ToDecimal(db.Scalar("SELECT SUM(Quantity) FROM gh.StockMovements WHERE MedicationBatchId=@B",Database.Parameter("@B",batch)))==7,"Reconciliation preserves quantity and ledger agreement");
                Assert(Convert.ToString(db.Scalar("SELECT TOP 1 Detail FROM gh.WorkflowHistory WHERE EntityType='StockCount' AND EntityId=@B ORDER BY HistoryId DESC",Database.Parameter("@B",batch))).Contains("Previous: 10; Counted: 7"),"Reconciliation audit retains observed and counted quantities");
                continue;
            }
            if(scenario=="stale-receipt"||scenario=="net-zero-movements"){
                service.ReceiveStock(medication,code,expiry,5,0,1,supplier,pharmacist,"Later");
                if(scenario=="net-zero-movements")Count(service,batch,pharmacist,10,service.GetStockCountSnapshot(batch,pharmacist).Rows[0]);
            }
            if(scenario=="stale-adjustment"||scenario=="replayed-count")Count(service,batch,pharmacist,7,original);
            if(scenario=="wrong-quantity")original["QuantityOnHand"]=11m;
            bool rejected=false;
            try{
                if(scenario=="snapshot-role")service.GetStockCountSnapshot(batch,reception);
                else Count(service,batch,scenario=="adjustment-role"?reception:pharmacist,scenario=="negative-count"?-1m:scenario=="unchanged-count"?10m:scenario=="quantity-precision"?1.001m:7m,original,scenario=="missing-reason"?"":"QA physical count");
            }catch(ArgumentException){rejected=true;}catch(InvalidOperationException){rejected=true;}
            Assert(rejected,"Stock count guard: "+scenario);
        }
        Console.WriteLine("All stock reconciliation fixtures roll back.");
    }
}
