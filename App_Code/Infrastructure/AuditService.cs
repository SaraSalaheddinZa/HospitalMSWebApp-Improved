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
            catch (SqlException ex)
            {
                // Clinical workflow history is transactional; report failure of this supplementary request log.
                System.Diagnostics.Trace.TraceError("Audit event persistence failed: "+ex.Message);
            }
        }

        private static string GetClientIp(HttpRequest request)
        {
            if (request == null) return null;
            // Never trust a client-supplied forwarding header without a configured trusted proxy.
            return request.UserHostAddress;
        }
    }
}
