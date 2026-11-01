using System;
using HospitalMSWebApp.Infrastructure;
namespace HospitalMSWebApp.Views.Notifications
{
    public class Index:WorkspacePage
    {
        protected override string Role {get{return CurrentUser==null?"":CurrentUser.Role;}}
        protected override string Heading {get{return "Inbox";}}
        protected override void OnLoad(EventArgs e){if(!RequireRoles("Admin","Doctor","Nurse","Patient","Pharmacist","Receptionist","Laboratorian"))return;base.OnLoad(e);}
        protected override void Submit()
        {
            if(V("action")!="read")throw new ArgumentException("Unknown action.");
            var id=Id("notificationId");
            if(Db.Execute("UPDATE gh.Notifications SET ReadAt=COALESCE(ReadAt,SYSUTCDATETIME()) WHERE NotificationId=@Id AND UserId=@U",P("@Id",id),P("@U",CurrentUser.UserId))!=1)throw new InvalidOperationException("Notification not found.");
            Audit("NotificationRead","Notification",id);
        }
        protected override string Content()
        {
            bool unread=Request.QueryString["unread"]=="1";
            var messages=Db.Query("SELECT TOP 100 NotificationId,Subject,Message,CreatedAt,ReadAt FROM gh.Notifications WHERE UserId=@U AND (@Unread=0 OR ReadAt IS NULL) ORDER BY CreatedAt DESC",P("@U",CurrentUser.UserId),P("@Unread",unread));
            var b=new System.Text.StringBuilder("<nav class='gh-message-filters' aria-label='Inbox filters'><a href='Index.aspx' "+(!unread?"aria-current='page'":"")+">All messages</a><a href='Index.aspx?unread=1' "+(unread?"aria-current='page'":"")+">Unread messages</a></nav>");
            if(messages.Rows.Count==0)b.Append("<div class='gh-empty'><strong>").Append(unread?"You're all caught up":"No messages yet").Append("</strong><p>Appointment updates, care notifications and hospital messages will appear here.</p></div>");
            foreach(System.Data.DataRow row in messages.Rows){
                bool isUnread=row["ReadAt"]==DBNull.Value;
                b.Append("<article class='gh-message-card ").Append(isUnread?"is-unread":"").Append("'><header><div><h3>").Append(H(Convert.ToString(row["Subject"]))).Append("</h3><time>").Append(Convert.ToDateTime(row["CreatedAt"]).ToString("dd MMM yyyy · HH:mm",System.Globalization.CultureInfo.InvariantCulture)).Append(" UTC</time></div>").Append(GuardianUi.Badge(isUnread?"Unread":"Read")).Append("</header><p>").Append(H(Convert.ToString(row["Message"]))).Append("</p>");
                if(isUnread)b.Append(Form("read","<input type='hidden' name='notificationId' value='"+row["NotificationId"]+"'>","Mark as read"));
                b.Append("</article>");
            }
            return Panel("My notifications",b.ToString());
        }
    }
}
