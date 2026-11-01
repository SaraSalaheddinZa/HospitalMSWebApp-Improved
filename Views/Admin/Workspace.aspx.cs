using System;
using HospitalMSWebApp.Infrastructure;
namespace HospitalMSWebApp.Views.Admin
{
    public class Workspace : WorkspacePage
    {
        protected override string Role { get { return "Admin"; } }
        protected override string Heading { get { return "Hospital administration"; } }
        protected override string SubmissionReturnUrl
        {
            get
            {
                int staffId;
                if(V("action")=="staffProfile" && int.TryParse(V("userId"),out staffId) && staffId>0)
                    return Request.Path+"?staffUser="+staffId.ToString(System.Globalization.CultureInfo.InvariantCulture)+"#nurse-and-pharmacist-profiles";
                if(V("action")=="setting")return Request.Path+"#system-configuration";
                return base.SubmissionReturnUrl;
            }
        }
        protected override void Submit()
        {
            switch(V("action")){
                case "staffStatus": Workflows.SetStaffActive((int)Id("userId"),V("active")=="Active",CurrentUser.UserId);Audit("StaffStatusChanged","UserAccount",V("userId"));break;
                case "staffProfile":
                    Workflows.UpdateStaffProfile((int)Id("userId"),Required("name"),Required("email"),V("phone"),V("departmentId")==""?(int?)null:(int)Id("departmentId"),V("confirmEmail")=="yes",V("profileVersion"),CurrentUser.UserId);break;
                case "department": var d=Workflows.AddDepartment(Required("code"),Required("name"),V("location"),V("phone"));Audit("DepartmentCreated","Department",d);break;
                case "ward": var w=Workflows.AddWard(Required("name"),Required("type"),null);Audit("WardCreated","Ward",w);break;
                case "room": var r=Workflows.AddRoom((int)Id("wardId"),Required("number"),Required("type"));Audit("RoomCreated","Room",r);break;
                case "bed": var b=Workflows.AddBed((int)Id("roomId"),Required("number"));Audit("BedCreated","Bed",b);break;
                case "staff":
                    string error; if(!PasswordSecurity.IsAcceptable(Required("password"),out error))throw new ArgumentException(error);
                    if(V("role")!="Nurse"&&V("role")!="Pharmacist")throw new ArgumentException("Select a supported staff role.");
                    var u=Workflows.CreateStaffAccount(Required("number"),V("role"),Required("name"),Required("email"),V("phone"),null,V("password"));Audit("StaffCreated","UserAccount",u);break;
                case "setting":
                    Workflows.SaveSystemSetting(V("key"),Required("value"),CurrentUser.UserId);break;
                default: throw new ArgumentException("Unknown action.");
            }
        }
        protected override string Content()
        {
            var filter=(Request.QueryString["filter"]??"").Trim();
            DateTime fromValue,toValue;
            DateTime? from=DateTime.TryParseExact(Request.QueryString["from"],"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out fromValue)?fromValue:(DateTime?)null;
            DateTime? to=DateTime.TryParseExact(Request.QueryString["to"],"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out toValue)&&toValue<DateTime.MaxValue.Date?toValue.AddDays(1):(DateTime?)null;
            return "<div class='row'>"+Metric("Active accounts","SELECT COUNT(*) FROM gh.UserAccounts WHERE IsActive=1")+Metric("Occupied beds","SELECT COUNT(*) FROM gh.Beds WHERE Status='Occupied'")+Metric("Pending laboratory orders","SELECT COUNT(*) FROM gh.LabOrders WHERE Status NOT IN('Completed','Cancelled')")+Metric("Outstanding balance","SELECT ISNULL(SUM(TotalAmount-PaidAmount),0) FROM gh.Invoices WHERE Status NOT IN('Voided','Draft')")+"</div>"
                +Panel("Operational attention","<p>Live counts on page load. These are transparent operational rules, not automated clinical decisions. <a href='CommandCenter.aspx'>Open command center worklists</a> · <a href='Operations.aspx'>Open operations</a> · <a href='Reports.aspx'>Review reports</a></p>"+Table(Workflows.GetOperationalAlerts(CurrentUser.UserId)))
                +Panel("Staff directory",Table(Db.Query("SELECT DisplayName AS Name,RoleName AS Role,Email,CASE WHEN IsActive=1 THEN 'Active' ELSE 'Inactive' END AS Status FROM gh.UserAccounts WHERE RoleName<>'Patient' ORDER BY RoleName,DisplayName")))
                +Panel("Nurse and pharmacist access",Form("staffStatus",Options("Account","userId","SELECT UserId,DisplayName+' · '+RoleName FROM gh.UserAccounts WHERE RoleName IN('Nurse','Pharmacist')")+Choice("Access","active","Active","Inactive"),"Update access"))
                +Panel("Nurse and pharmacist profiles",StaffProfileEditor())
                +Panel("Create nurse or pharmacist",Form("staff",Field("Staff number","number")+Choice("Role","role","Nurse","Pharmacist")+Field("Name","name")+Field("Email","email","email")+Field("Telephone","phone","tel",false)+Field("Temporary password","password","password"),"Create staff account"))
                +Panel("Departments",Table(Db.Query("SELECT DepartmentCode AS Code,DepartmentName AS Department,Location,Phone FROM gh.Departments ORDER BY DepartmentName"))+Form("department",Field("Code","code")+Field("Name","name")+Field("Location","location","text",false)+Field("Phone","phone","tel",false),"Create department"))
                +Panel("Ward, room and bed directory",Table(Db.Query("SELECT w.WardName AS Ward,r.RoomNumber AS Room,b.BedNumber AS Bed,b.Status FROM gh.Wards w LEFT JOIN gh.Rooms r ON r.WardId=w.WardId LEFT JOIN gh.Beds b ON b.RoomId=r.RoomId ORDER BY w.WardName,r.RoomNumber,b.BedNumber")))
                +Panel("Create ward",Form("ward",Field("Name","name")+Choice("Type","type","General","ICU","Emergency","Maternity","Pediatric"),"Create ward"))
                +Panel("Create room",Form("room",Options("Ward","wardId","SELECT WardId,WardName FROM gh.Wards WHERE IsActive=1")+Field("Room number","number")+Choice("Type","type","General","Private","Isolation","ICU"),"Create room"))
                +Panel("Create bed",Form("bed",Options("Room","roomId","SELECT r.RoomId,w.WardName+' / '+r.RoomNumber FROM gh.Rooms r JOIN gh.Wards w ON w.WardId=r.WardId WHERE r.IsActive=1")+Field("Bed number","number"),"Create bed"))
                +Panel("System configuration",Table(Db.Query("SELECT SettingKey,SettingValue,UpdatedAt FROM gh.SystemSettings WHERE SettingKey IN('HospitalDisplayName','ReceptionContact')"))+Form("setting",Choice("Setting","key","HospitalDisplayName","ReceptionContact")+Field("Value","value"),"Save setting"))
                +"<div id='audit'>"+Panel("Audit review","<form method='get' class='row g-2 mb-3'><label class='col-md-4'>Action, role or entity<input class='form-control' name='filter' value='"+H(filter)+"'></label><label class='col-md-3'>From (UTC)<input type='date' class='form-control' name='from' value='"+H(Request.QueryString["from"])+"'></label><label class='col-md-3'>Through (UTC)<input type='date' class='form-control' name='to' value='"+H(Request.QueryString["to"])+"'></label><div class='col-md-2 align-self-end'><button class='btn btn-primary'>Filter</button></div></form><p class='text-muted'>Most recent 200 matching events. Dates are inclusive.</p>"+Table(Db.Query(@"SELECT TOP 200 a.OccurredAt,u.DisplayName AS Actor,a.ActorRole,a.ActionName,a.EntityType,a.EntityId,a.Detail FROM gh.AuditEvents a LEFT JOIN gh.UserAccounts u ON u.UserId=a.ActorUserId WHERE (@F='' OR a.ActionName LIKE '%'+@F+'%' OR a.ActorRole LIKE '%'+@F+'%' OR a.EntityType LIKE '%'+@F+'%') AND (@From IS NULL OR a.OccurredAt>=@From) AND (@To IS NULL OR a.OccurredAt<@To) ORDER BY a.OccurredAt DESC",P("@F",filter),P("@From",from),P("@To",to))))+"</div>";
        }
        private string StaffProfileEditor()
        {
            var html=new System.Text.StringBuilder("<form method='get' action='#nurse-and-pharmacist-profiles' class='row g-3'>"+
                Options("Staff member","staffUser","SELECT UserId,DisplayName+' · '+RoleName FROM gh.UserAccounts WHERE RoleName IN('Nurse','Pharmacist') ORDER BY DisplayName")+
                "<div class='col-12'><button class='btn btn-primary'>Open staff profile</button></div></form>");
            int selected;
            if(!int.TryParse(Request.QueryString["staffUser"],out selected))return html.Append("<p>Select a staff member to maintain contact details and department assignment. Access changes remain a separate action.</p>").ToString();
            var rows=Workflows.GetStaffProfile(selected,CurrentUser.UserId);
            if(rows.Rows.Count!=1)return html.Append("<p role='alert'>That nurse or pharmacist profile is unavailable.</p>").ToString();
            var row=rows.Rows[0];
            html.Append("<p>").Append(H(Convert.ToString(row["FullName"]))).Append(" · ").Append(H(Convert.ToString(row["RoleName"]))).Append(" · Staff number ").Append(H(Convert.ToString(row["StaffNumber"]))).Append(" · ").Append(GuardianUi.Badge(Convert.ToBoolean(row["IsActive"])?"Active":"Inactive")).Append("</p>");
            var fields=new System.Text.StringBuilder("<input type='hidden' name='userId' value='"+selected+"'><input type='hidden' name='profileVersion' value='"+H(Convert.ToString(row["ProfileVersion"]))+"'>");
            fields.Append(ProfileField("Name","name",Convert.ToString(row["FullName"]),"text",true));
            fields.Append(ProfileField("Sign-in email","email",Convert.ToString(row["Email"]),"email",true));
            fields.Append(ProfileField("Telephone","phone",Convert.ToString(row["Phone"]),"tel",false));
            fields.Append("<div class='col-md-4'><label class='form-label'>Department<select class='form-select' name='departmentId'><option value=''>Unassigned</option>");
            foreach(System.Data.DataRow department in Db.Query("SELECT DepartmentId,DepartmentName FROM gh.Departments WHERE IsActive=1 ORDER BY DepartmentName").Rows)
                fields.Append("<option value='").Append(H(Convert.ToString(department[0]))).Append("' ").Append(Convert.ToString(department[0])==Convert.ToString(row["DepartmentId"])?"selected":"").Append(">").Append(H(Convert.ToString(department[1]))).Append("</option>");
            fields.Append("</select></label></div><div class='col-12'><label><input type='checkbox' name='confirmEmail' value='yes'> Confirm changing the existing sign-in email (ends existing sessions).</label><p>Password, account status, lockout and staff number are not changed by this form. Reopen the profile if another administrator updates it.</p></div>");
            html.Append(Form("staffProfile",fields.ToString(),"Save staff profile"));
            return html.ToString();
        }
        private static string ProfileField(string label,string name,string value,string type,bool required)
        {
            return "<div class='col-md-4'><label class='form-label'>"+H(label)+"<input class='form-control' name='"+H(name)+"' type='"+type+"' value='"+H(value)+"' "+(required?"required":"")+"></label></div>";
        }
    }
}
