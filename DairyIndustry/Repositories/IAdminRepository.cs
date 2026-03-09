using DairyIndustry.Models.Admin;
using System.Data;

namespace DairyIndustry.Repositories
{
    
        public interface IAdminRepository
        {
            // ── ROLES ──────────────────────────────────────────────
            int CreateRole(string roleName);
            List<RoleModel> GetAllRoles();

            // ── USERS ──────────────────────────────────────────────
            int RegisterUser(string username, string passwordHash, int roleId, int? staffId);
            User GetUserByUsername(string username);
            List<User> GetAllUsers();
            void UpdateUserStatus(int userId, bool isActive);

            // ── AUDIT LOG ──────────────────────────────────────────
            void WriteAuditLog(int userId, string action, string entityName);
            List<AuditLogModel> GetAuditLogs(int? userId, string? entityName, DateTime? fromDate, DateTime? toDate);
        }
    
}
