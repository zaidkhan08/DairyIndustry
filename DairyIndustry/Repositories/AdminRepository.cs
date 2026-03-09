using DairyIndustry.Data;
using DairyIndustry.Models.Admin;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DairyIndustry.Repositories
{
    public class AdminRepository : IAdminRepository
    {
        private readonly DbHelper _db;

        public AdminRepository(DbHelper db)
        {
            _db = db;
        }

        // ════════════════════════════════════════════════════════
        // ROLES
        // ════════════════════════════════════════════════════════

        public int CreateRole(string roleName)
        {
            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Admin.usp_Admin_CreateRole", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@RoleName", roleName);

                    con.Open();
                    var result = cmd.ExecuteScalar();
                    return Convert.ToInt32(result);
                }
            }
        }

        public List<RoleModel> GetAllRoles()
        {
            var list = new List<RoleModel>();

            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("SELECT RoleId, RoleName FROM Admin.Roles ORDER BY RoleName", con))
                {
                    cmd.CommandType = CommandType.Text;

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new RoleModel
                            {
                                RoleId = Convert.ToInt32(reader["RoleId"]),
                                RoleName = reader["RoleName"].ToString()
                            });
                        }
                    }
                }
            }

            return list;
        }

        // ════════════════════════════════════════════════════════
        // USERS
        // ════════════════════════════════════════════════════════

        public int RegisterUser(string username, string passwordHash, int roleId, int? staffId)
        {
            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Admin.usp_Admin_RegisterUser", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Username", username);
                    cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
                    cmd.Parameters.AddWithValue("@RoleId", roleId);
                    cmd.Parameters.AddWithValue("@StaffId", (object?)staffId ?? DBNull.Value);

                    con.Open();
                    var result = cmd.ExecuteScalar();
                    return Convert.ToInt32(result);
                }
            }
        }

        public User GetUserByUsername(string username)
        {
            User user = null;

            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Admin.usp_Admin_LoginUser", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Username", username);

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            user = new User
                            {
                                UserId = Convert.ToInt32(reader["UserId"]),
                                Username = reader["Username"].ToString(),
                                PasswordHash = reader["PasswordHash"].ToString(),
                                IsActive = Convert.ToBoolean(reader["IsActive"]),
                                StaffId = reader["StaffId"] == DBNull.Value ? null : Convert.ToInt32(reader["StaffId"]),
                                RoleId = Convert.ToInt32(reader["RoleId"]),
                                RoleName = reader["RoleName"].ToString(),
                                CreatedDate = DateTime.MinValue  // not returned by LoginUser SP
                            };
                        }
                    }
                }
            }

            return user;
        }

        public List<User> GetAllUsers()
        {
            var list = new List<User>();

            var query = @"
                SELECT u.UserId, u.Username, u.PasswordHash, u.RoleId, u.StaffId,
                       u.IsActive, u.CreatedDate, r.RoleName
                FROM Admin.Users u
                INNER JOIN Admin.Roles r ON r.RoleId = u.RoleId
                ORDER BY u.Username";

            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.CommandType = CommandType.Text;

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new User
                            {
                                UserId = Convert.ToInt32(reader["UserId"]),
                                Username = reader["Username"].ToString(),
                                PasswordHash = reader["PasswordHash"].ToString(),
                                RoleId = Convert.ToInt32(reader["RoleId"]),
                                RoleName = reader["RoleName"].ToString(),
                                StaffId = reader["StaffId"] == DBNull.Value ? null : Convert.ToInt32(reader["StaffId"]),
                                IsActive = Convert.ToBoolean(reader["IsActive"]),
                                CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
                            });
                        }
                    }
                }
            }

            return list;
        }

        public void UpdateUserStatus(int userId, bool isActive)
        {
            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Admin.usp_Admin_UpdateUserStatus", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@IsActive", isActive);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ════════════════════════════════════════════════════════
        // AUDIT LOG
        // ════════════════════════════════════════════════════════

        public void WriteAuditLog(int userId, string action, string entityName)
        {
            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Admin.usp_Admin_WriteAuditLog", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@Action", action);
                    cmd.Parameters.AddWithValue("@EntityName", entityName);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<AuditLogModel> GetAuditLogs(int? userId, string? entityName, DateTime? fromDate, DateTime? toDate)
        {
            var list = new List<AuditLogModel>();

            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Admin.usp_Admin_GetAuditLogs", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@EntityName", (object?)entityName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FromDate", (object?)fromDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ToDate", (object?)toDate ?? DBNull.Value);

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new AuditLogModel
                            {
                                LogId = Convert.ToInt32(reader["LogId"]),
                                Username = reader["Username"].ToString(),
                                Action = reader["Action"].ToString(),
                                EntityName = reader["EntityName"].ToString(),
                                ActionDate = Convert.ToDateTime(reader["ActionDate"])
                            });
                        }
                    }
                }
            }

            return list;
        }
    }
}
