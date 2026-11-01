using System;
using System.Transactions;
using HospitalMSWebApp.Infrastructure;
using HospitalMSWebApp.Services;
class SupplierReceipts
{
    static void Assert(bool ok,string text){if(!ok)throw new Exception(text);Console.WriteLine("PASS: "+text);}
    static void Main()
    {
        Environment.SetEnvironmentVariable("HOSPITALMS_CONNECTIONSTRING",@"Data Source=.\SQLEXP2017;Initial Catalog=HospitalMS-Sara7593-Wasan7639;Integrated Security=True;MultipleActiveResultSets=True");
        var db=new Database();var service=new HospitalWorkflowService();
        int pharmacist=Convert.ToInt32(db.Scalar("SELECT UserId FROM gh.UserAccounts WHERE Email='dev.pharmacist@guardianhealth.test'"));
        int reception=Convert.ToInt32(db.Scalar("SELECT UserId FROM gh.UserAccounts WHERE Email='dev.reception@guardianhealth.test'"));
        int before=Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM gh.StockReceipts"));
        foreach(string scenario in new[]{"valid","duplicate-reference","duplicate-changed-batch","wrong-supplier","wrong-expiry","inactive-supplier","wrong-role","missing-reference","long-reference","missing-supplier","negative-cost","fractional-precision"})
        using(var fixture=new TransactionScope()){
            string code="QA-"+Guid.NewGuid().ToString("N").Substring(0,12);
            int medication=Convert.ToInt32(db.Scalar("INSERT INTO gh.MedicationCatalog(MedicationCode,MedicationName) VALUES(@C,@C);SELECT CAST(SCOPE_IDENTITY() AS int)",Database.Parameter("@C",code)));
            int supplier=service.SaveSupplier(null,code,"Contact","","",pharmacist);
            var expiry=DateTime.UtcNow.Date.AddYears(1);
            service.ReceiveStock(medication,code,expiry,10,2,1,supplier,pharmacist,"Invoice-A/1");
            long batch=Convert.ToInt64(db.Scalar("SELECT MedicationBatchId FROM gh.MedicationBatches WHERE MedicationId=@M",Database.Parameter("@M",medication)));
            if(scenario=="valid"){
                service.ReceiveStock(medication,code,expiry,5,9,2,supplier,pharmacist,"Invoice-B/1");
                Assert(Convert.ToDecimal(db.Scalar("SELECT QuantityOnHand FROM gh.MedicationBatches WHERE MedicationBatchId=@B",Database.Parameter("@B",batch)))==15,"Distinct supplier delivery lines add to the existing batch");
                Assert(Convert.ToDecimal(db.Scalar("SELECT QuantityReceived FROM gh.MedicationBatches WHERE MedicationBatchId=@B",Database.Parameter("@B",batch)))==15,"Lifetime received quantity includes repeat deliveries");
                Assert(Convert.ToDecimal(db.Scalar("SELECT SUM(Quantity) FROM gh.StockMovements WHERE MedicationBatchId=@B",Database.Parameter("@B",batch)))==15,"Repeat delivery ledger reconciles with stock on hand");
                Assert(Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM gh.StockReceipts WHERE MedicationBatchId=@B",Database.Parameter("@B",batch)))==2,"Each delivery has its own immutable supplier receipt");
                Assert(Convert.ToDecimal(db.Scalar("SELECT SUM(Quantity*UnitCost) FROM gh.StockReceipts WHERE MedicationBatchId=@B",Database.Parameter("@B",batch)))==20,"Receipt costs retain the actual delivery cost history");
                Assert(Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM gh.MedicationBatches WHERE MedicationBatchId=@B AND MinimumStock=2 AND UnitCost=1",Database.Parameter("@B",batch)))==1,"Repeat delivery does not silently overwrite batch settings");
                Assert(Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM gh.WorkflowHistory h JOIN gh.StockReceipts r ON r.StockReceiptId=h.EntityId WHERE h.EntityType='SupplierDelivery' AND r.MedicationBatchId=@B AND h.ActorUserId=@U",Database.Parameter("@B",batch),Database.Parameter("@U",pharmacist)))==2,"Both receipt lines have attributed transactional history");
                continue;
            }
            int? selectedSupplier=supplier;string reference="Invoice-B/1",selectedBatch=code;decimal cost=1,quantity=5;int actor=pharmacist;
            if(scenario.StartsWith("duplicate"))reference="Invoice-A/1";
            if(scenario=="duplicate-changed-batch")selectedBatch=code+"-other";
            if(scenario=="wrong-supplier")selectedSupplier=service.SaveSupplier(null,code+"-other","","","",pharmacist);
            if(scenario=="wrong-expiry")expiry=expiry.AddDays(1);
            if(scenario=="inactive-supplier")service.SetSupplierActive(supplier,false,pharmacist);
            if(scenario=="wrong-role")actor=reception;
            if(scenario=="missing-reference")reference=" ";
            if(scenario=="long-reference")reference=new string('x',81);
            if(scenario=="missing-supplier")selectedSupplier=null;
            if(scenario=="negative-cost")cost=-1;
            if(scenario=="fractional-precision")quantity=0.001m;
            bool rejected=false;
            try{service.ReceiveStock(medication,selectedBatch,expiry,quantity,2,cost,selectedSupplier,actor,reference);}
            catch(ArgumentException){rejected=true;}catch(InvalidOperationException){rejected=true;}
            Assert(rejected,"Supplier receipt guard: "+scenario);
        }
        Assert(Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM gh.StockReceipts"))==before,"Supplier receipt fixtures leave no retained receipt records");
    }
}
