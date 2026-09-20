using System;
using System.Data.SqlClient;
using System.Web;

namespace HospitalMSWebApp.Infrastructure
{
    public static class AuditService
    {
        public static void Write(AuthenticatedUser actor, string action, string entityType, string entityId, string detail, HttpRequest request)
        {
            try
            {
                new Database().Execute(@"INSERT INTO gh.AuditEvents (ActorUserId, ActorRole, ActionName, EntityType, EntityId, Detail, IpAddress, OccurredAt)
                                         VALUES (@ActorUserId, @ActorRole, @ActionName, @EntityType, @EntityId, @Detail, @IpAddress, SYSUTCDATETIME())",
                    Database.Parameter("@ActorUserId", actor == null ? (object)DBNull.Value : actor.UserId),
                    Database.Parameter("@ActorRole", actor == null ? null : actor.Role),
                    Database.Parameter("@ActionName", action), Database.Parameter("@EntityType", entityType),
                    Database.Parameter("@EntityId", entityId), Database.Parameter("@Detail", detail),
                    Database.Parameter("@IpAddress", GetClientIp(request)));
            }
            catch (SqlException)
            {
                // Auditing must never prevent a clinical transaction when a migration has not yet run.
            }
        }

        private static string GetClientIp(HttpRequest request)
        {
            if (request == null) return null;
            var forwarded = request.Headers["X-Forwarded-For"];
            if (!string.IsNullOrEmpty(forwarded)) return forwarded.Split(',')[0].Trim();
            return request.UserHostAddress;
        }
    }
}
