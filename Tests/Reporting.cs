using System;
using System.Data;
using HospitalMSWebApp.Infrastructure;
using HospitalMSWebApp.Services;
class Reporting
{
    static void Assert(bool ok,string text){if(!ok)throw new Exception(text);Console.WriteLine("PASS: "+text);}
    static void Reject(Action action,string text){bool denied=false;try{action();}catch(InvalidOperationException){denied=true;}catch(ArgumentException){denied=true;}Assert(denied,text);}
    static void Main()
    {
        Environment.SetEnvironmentVariable("HOSPITALMS_CONNECTIONSTRING",@"Data Source=.\SQLEXP2017;Initial Catalog=HospitalMS-Sara7593-Wasan7639;Integrated Security=True;MultipleActiveResultSets=True");
        var db=new Database();var work=new HospitalWorkflowService();
        int admin=Convert.ToInt32(db.Scalar("SELECT UserId FROM gh.UserAccounts WHERE Email='dev.admin@guardianhealth.test'"));
        var today=DateTime.UtcNow.Date;
        foreach(string key in HospitalWorkflowService.OperationalReportKeys){
            var data=work.GetOperationalReport(key,admin,today.AddDays(-30),today);
            Assert(data.Columns.Count>0,"Operational report executes against real schema: "+key);
            Assert(CsvExport.Encode(data).Length>0,"Report exports headers even when empty: "+key);
        }
        foreach(DataRow row in db.Query("SELECT UserId,RoleName FROM gh.UserAccounts WHERE Email LIKE 'dev.%@guardianhealth.test' AND RoleName<>'Admin'").Rows){
            int actor=Convert.ToInt32(row[0]);Reject(()=>work.GetOperationalReport("collections",actor,today,today),"Report service rejects role "+row[1]);
        }
        Reject(()=>work.GetOperationalReport("collections; SELECT 1",admin,today,today),"Report selection is allowlisted");
        Reject(()=>work.GetOperationalReport("collections",admin,today,today.AddDays(-1)),"Report rejects inverted dates");
        Reject(()=>work.GetOperationalReport("collections",admin,today,DateTime.MaxValue),"Report rejects overflowing end date");
        Reject(()=>work.GetOperationalReport("collections",admin,DateTime.MinValue,today),"Report rejects dates outside SQL parameter range");
        var cells=new DataTable();cells.Columns.Add("Name");cells.Columns.Add("Quantity",typeof(decimal));
        foreach(string input in new[]{"=HYPERLINK(\"https://example.invalid\")"," +SUM(1,2)","@function","-formula","\t=1","\n=1"}){
            cells.Rows.Clear();cells.Rows.Add(input,-2m);string csv=CsvExport.Encode(cells);
            Assert(csv.Contains("\"'"+input.Replace("\"","\"\"")),"CSV protects stored formula-like text");
            Assert(csv.Contains(",\"-2\""),"CSV retains genuine signed numeric ledger values");
        }
        cells.Rows.Clear();cells.Rows.Add("Quoted \"name\", with\nnewline",2m);
        Assert(CsvExport.Encode(cells).Contains("\"Quoted \"\"name\"\", with\nnewline\""),"CSV quotes embedded delimiters, quotes and newlines");
        cells.Rows.Clear();cells.Rows.Add(DBNull.Value,1.25m);
        var originalCulture=System.Threading.Thread.CurrentThread.CurrentCulture;
        try{System.Threading.Thread.CurrentThread.CurrentCulture=new System.Globalization.CultureInfo("fr-FR");Assert(CsvExport.Encode(cells).Contains("\"\",\"1.25\""),"CSV encodes nulls and numbers independently of server culture");}
        finally{System.Threading.Thread.CurrentThread.CurrentCulture=originalCulture;}
    }
}
