using System;
using System.Data;
namespace HospitalMSWebApp.Services
{
    public sealed partial class HospitalWorkflowService
    {
        // Returns one extra row so the UI can paginate without an expensive or inconsistent count query.
        public DataTable GetAuditHistory(int actor,string source,string entityType,int? performedBy,
            DateTime? from,DateTime? through,string search,int page=1)
        {
            if(source!="Workflow"&&source!="Access")throw new ArgumentException("Choose workflow history or access/request audit.");
            if(page<1||page>100000||(entityType??"").Length>80||(search??"").Length>100||from<System.Data.SqlTypes.SqlDateTime.MinValue.Value||through<System.Data.SqlTypes.SqlDateTime.MinValue.Value||through<from||through>=DateTime.MaxValue.Date)
                throw new ArgumentException("Choose valid audit filters, dates and page number.");
            string query=source=="Workflow"
                ?@"SELECT h.HistoryId AS EventId,h.OccurredAt,h.ActorUserId,h.EntityType,CONVERT(nvarchar(80),h.EntityId) AS Reference,
                    h.PreviousStatus,h.NewStatus AS Action,h.Detail FROM gh.WorkflowHistory h"
                :@"SELECT h.AuditEventId AS EventId,h.OccurredAt,h.ActorUserId,h.EntityType,h.EntityId AS Reference,
                    CAST(NULL AS nvarchar(40)) AS PreviousStatus,h.ActionName AS Action,h.Detail FROM gh.AuditEvents h";
            return database.InTransaction((c,t)=>{
                RequireActor(c,t,actor,"Admin");
                return ReadTable(c,t,@"SELECT h.EventId,h.OccurredAt,COALESCE(u.DisplayName,'Not recorded') AS Actor,
                    h.EntityType,h.Reference,h.PreviousStatus,h.Action,h.Detail FROM ("+query+@")h
                    LEFT JOIN gh.UserAccounts u ON u.UserId=h.ActorUserId
                    WHERE (@Entity='' OR h.EntityType=@Entity) AND (@Actor IS NULL OR h.ActorUserId=@Actor)
                      AND (@From IS NULL OR h.OccurredAt>=@From) AND (@Through IS NULL OR h.OccurredAt<@Through)
                      AND (@Search='' OR CHARINDEX(@Search,COALESCE(h.Action,''))>0 OR CHARINDEX(@Search,COALESCE(h.Detail,''))>0 OR h.Reference=@Search)
                    ORDER BY h.OccurredAt DESC,h.EventId DESC OFFSET @Offset ROWS FETCH NEXT 101 ROWS ONLY",
                    P("@Entity",entityType??""),P("@Actor",performedBy),P("@From",from),P("@Through",through.HasValue?(object)through.Value.Date.AddDays(1):null),P("@Search",search??""),P("@Offset",(page-1)*100));
            });
        }

        public void SaveSystemSetting(string key,string value,int actor)
        {
            if(key!="HospitalDisplayName"&&key!="ReceptionContact")throw new ArgumentException("Choose a supported setting.");
            if(string.IsNullOrWhiteSpace(value)||value.Length>300)throw new ArgumentException("Enter a setting value of up to 300 characters.");
            database.InTransaction((c,t)=>{
                RequireActor(c,t,actor,"Admin");
                var previous=Scalar(c,t,"SELECT SettingValue FROM gh.SystemSettings WITH(UPDLOCK,HOLDLOCK) WHERE SettingKey=@K",P("@K",key));
                if(Convert.ToString(previous)==value)throw new InvalidOperationException("The setting already has this value.");
                Execute(c,t,@"UPDATE gh.SystemSettings SET SettingValue=@V,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@U WHERE SettingKey=@K;
                    IF @@ROWCOUNT=0 INSERT INTO gh.SystemSettings(SettingKey,SettingValue,UpdatedByUserId) VALUES(@K,@V,@U)",P("@K",key),P("@V",value),P("@U",actor));
                string before=previous==null?"[not configured]":Convert.ToString(previous);
                if(before.Length>600)before=before.Substring(0,600)+"…";
                // Settings use string keys, so record them in the string-key audit table in the SAME transaction.
                Execute(c,t,@"INSERT INTO gh.AuditEvents(ActorUserId,ActorRole,ActionName,EntityType,EntityId,Detail)
                    VALUES(@U,'Admin','SettingUpdated','SystemSetting',@K,@D)",P("@U",actor),P("@K",key),P("@D","Previous: "+before+"; New: "+value));
                return 0;
            });
        }
    }
}
