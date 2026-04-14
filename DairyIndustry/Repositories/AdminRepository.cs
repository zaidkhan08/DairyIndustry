using DairyIndustry.Data;
using DairyIndustry.Models.Admin;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
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
        public void AssignUserToPlant(int userId, int plantId)
        {
            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Admin.usp_Admin_AssignUserToPlant", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@PlantId", plantId);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }
        public void AssignUserToCenter(int userId, int centerId)
        {
            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Admin.usp_Admin_AssignUserToCenter", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@CenterId", centerId);

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
        // ════════════════════════════════════════════════════════
        // LOCATION — STATE
        // ════════════════════════════════════════════════════════

        public int AddState(string stateName)
        {
            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Location.usp_Location_AddState", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@StateName", stateName);

                    con.Open();
                    var result = cmd.ExecuteScalar();
                    return Convert.ToInt32(result);
                }
            }
        }

        public List<StateModel> GetAllStates()
        {
            var list = new List<StateModel>();

            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("SELECT StateId, StateName FROM Location.State ORDER BY StateName", con))
                {
                    cmd.CommandType = CommandType.Text;
                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new StateModel
                            {
                                StateId = Convert.ToInt32(reader["StateId"]),
                                StateName = reader["StateName"].ToString()
                            });
                        }
                    }
                }
            }

            return list;
        }

        // ════════════════════════════════════════════════════════
        // LOCATION — CITY
        // ════════════════════════════════════════════════════════

        public int AddCity(string cityName, int stateId)
        {
            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Location.usp_Location_AddCity", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@CityName", cityName);
                    cmd.Parameters.AddWithValue("@StateId", stateId);

                    con.Open();
                    var result = cmd.ExecuteScalar();
                    return Convert.ToInt32(result);
                }
            }
        }

        public List<CityModel> GetAllCities()
        {
            var list = new List<CityModel>();

            var query = @"
                SELECT c.CityId, c.CityName, c.StateId, s.StateName
                FROM Location.City c
                INNER JOIN Location.State s ON s.StateId = c.StateId
                ORDER BY c.CityName";

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
                            list.Add(new CityModel
                            {
                                CityId = Convert.ToInt32(reader["CityId"]),
                                CityName = reader["CityName"].ToString(),
                                StateId = Convert.ToInt32(reader["StateId"]),
                                StateName = reader["StateName"].ToString()
                            });
                        }
                    }
                }
            }

            return list;
        }

        public List<CityModel> GetCitiesByState(int stateId)
        {
            var list = new List<CityModel>();

            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Location.usp_Location_GetCitiesByState", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@StateId", stateId);

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new CityModel
                            {
                                CityId = Convert.ToInt32(reader["CityId"]),
                                CityName = reader["CityName"].ToString()
                            });
                        }
                    }
                }
            }

            return list;
        }

        // ════════════════════════════════════════════════════════
        // LOCATION — VILLAGE
        // ════════════════════════════════════════════════════════

        public int AddVillage(string villageName, int cityId)
        {
            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Location.usp_Location_AddVillage", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@VillageName", villageName);
                    cmd.Parameters.AddWithValue("@CityId", cityId);

                    con.Open();
                    var result = cmd.ExecuteScalar();
                    return Convert.ToInt32(result);
                }
            }
        }

        public List<VillageModel> GetAllVillages()
        {
            var list = new List<VillageModel>();

            var query = @"
                SELECT v.VillageId, v.VillageName, v.CityId,
                       c.CityName, s.StateName
                FROM Location.Village v
                INNER JOIN Location.City  c ON c.CityId  = v.CityId
                INNER JOIN Location.State s ON s.StateId = c.StateId
                ORDER BY v.VillageName";

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
                            list.Add(new VillageModel
                            {
                                VillageId = Convert.ToInt32(reader["VillageId"]),
                                VillageName = reader["VillageName"].ToString(),
                                CityId = Convert.ToInt32(reader["CityId"]),
                                CityName = reader["CityName"].ToString(),
                                StateName = reader["StateName"].ToString()
                            });
                        }
                    }
                }
            }

            return list;
        }

        public List<VillageModel> GetVillagesByCity(int cityId)
        {
            var list = new List<VillageModel>();

            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Location.usp_Location_GetVillagesByCity", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@CityId", cityId);

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new VillageModel
                            {
                                VillageId = Convert.ToInt32(reader["VillageId"]),
                                VillageName = reader["VillageName"].ToString()
                            });
                        }
                    }
                }
            }

            return list;
        }

        // ════════════════════════════════════════════════════════
        // MILK TYPES
        // ════════════════════════════════════════════════════════

        public int AddMilkType(string milkTypeName)
        {
            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Finance.usp_Finance_AddMilkType", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@MilkTypeName", milkTypeName);

                    con.Open();
                    var result = cmd.ExecuteScalar();
                    return Convert.ToInt32(result);
                }
            }
        }

        public List<MilkTypeModel> GetAllMilkTypes()
        {
            var list = new List<MilkTypeModel>();

            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("SELECT MilkTypeId, MilkTypeName FROM Finance.MilkTypes ORDER BY MilkTypeName", con))
                {
                    cmd.CommandType = CommandType.Text;
                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new MilkTypeModel
                            {
                                MilkTypeId = Convert.ToInt32(reader["MilkTypeId"]),
                                MilkTypeName = reader["MilkTypeName"].ToString()
                            });
                        }
                    }
                }
            }

            return list;
        }

        // ════════════════════════════════════════════════════════
        // RATE CHART
        // ════════════════════════════════════════════════════════

        public int AddRateChart(int milkTypeId, decimal fatFrom, decimal fatTo, decimal clrFrom, decimal clrTo, decimal ratePerLiter, DateTime effectiveFrom)
        {
            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Finance.usp_Finance_SetRateChart", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@MilkTypeId", milkTypeId);
                    cmd.Parameters.AddWithValue("@FatFrom", fatFrom);
                    cmd.Parameters.AddWithValue("@FatTo", fatTo);
                    cmd.Parameters.AddWithValue("@CLRFrom", clrFrom);
                    cmd.Parameters.AddWithValue("@CLRTo", clrTo);
                    cmd.Parameters.AddWithValue("@RatePerLiter", ratePerLiter);
                    cmd.Parameters.AddWithValue("@EffectiveFrom", effectiveFrom);

                    con.Open();
                    var result = cmd.ExecuteScalar();
                    return Convert.ToInt32(result);
                }
            }
        }

        public List<RateChartModel> GetAllRateCharts()
        {
            var list = new List<RateChartModel>();

            var query = @"
                SELECT rc.RateChartId, rc.MilkTypeId, rc.FatFrom, rc.FatTo,
                       rc.CLRFrom, rc.CLRTo, rc.RatePerLiter, rc.EffectiveFrom,
                       mt.MilkTypeName
                FROM Finance.RateCharts rc
                INNER JOIN Finance.MilkTypes mt ON mt.MilkTypeId = rc.MilkTypeId
                ORDER BY rc.EffectiveFrom DESC";

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
                            list.Add(new RateChartModel
                            {
                                RateChartId = Convert.ToInt32(reader["RateChartId"]),
                                MilkTypeId = Convert.ToInt32(reader["MilkTypeId"]),
                                MilkTypeName = reader["MilkTypeName"].ToString(),
                                FatFrom = Convert.ToDecimal(reader["FatFrom"]),
                                FatTo = Convert.ToDecimal(reader["FatTo"]),
                                CLRFrom = Convert.ToDecimal(reader["CLRFrom"]),
                                CLRTo = Convert.ToDecimal(reader["CLRTo"]),
                                RatePerLiter = Convert.ToDecimal(reader["RatePerLiter"]),
                                EffectiveFrom = Convert.ToDateTime(reader["EffectiveFrom"])
                            });
                        }
                    }
                }
            }

            return list;
        }

        public RateChartModel GetActiveRate(int milkTypeId, decimal fat, decimal clr, DateTime? asOfDate)
        {
            RateChartModel rate = null;

            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Finance.usp_Finance_GetActiveRate", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@MilkTypeId", milkTypeId);
                    cmd.Parameters.AddWithValue("@Fat", fat);
                    cmd.Parameters.AddWithValue("@CLR", clr);
                    cmd.Parameters.AddWithValue("@AsOfDate", (object?)asOfDate ?? DBNull.Value);

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            rate = new RateChartModel
                            {
                                RateChartId = Convert.ToInt32(reader["RateChartId"]),
                                RatePerLiter = Convert.ToDecimal(reader["RatePerLiter"]),
                                EffectiveFrom = Convert.ToDateTime(reader["EffectiveFrom"]),
                                FatFrom = Convert.ToDecimal(reader["FatFrom"]),
                                FatTo = Convert.ToDecimal(reader["FatTo"]),
                                CLRFrom = Convert.ToDecimal(reader["CLRFrom"]),
                                CLRTo = Convert.ToDecimal(reader["CLRTo"])
                            };
                        }
                    }
                }
            }

            return rate;
        }
        // ════════════════════════════════════════════════════════
        // STAFF
        // ════════════════════════════════════════════════════════

        public int AddStaff(string firstName, string lastName, string phone, string email,
     int roleId, DateTime? doj,
     string bankName, string accountNumber, string ifscCode,
     decimal salary,
     string profilePhoto = null,
     int? centerId = null,
     int? plantId = null)
        {
            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("HR.usp_HR_AddStaff", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@FirstName", firstName);
                    cmd.Parameters.AddWithValue("@LastName", lastName);
                    cmd.Parameters.AddWithValue("@Phone", (object?)phone ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Email", (object?)email ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@RoleId", roleId);
                    cmd.Parameters.AddWithValue("@DOJ", (object?)doj ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@BankName", (object?)bankName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@AccountNumber", (object?)accountNumber ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IFSCCode", (object?)ifscCode ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ProfilePhoto", (object?)profilePhoto ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Salary", salary);
                    cmd.Parameters.AddWithValue("@CenterId", (object?)centerId ?? DBNull.Value); // NEW
                    cmd.Parameters.AddWithValue("@PlantId", (object?)plantId ?? DBNull.Value); // NEW
                    con.Open();
                    var result = cmd.ExecuteScalar();
                    return Convert.ToInt32(result);
                }
            }
        }

        public List<StaffModel> GetAllStaff(int? roleId = null, bool? isActive = null)
        {
            var list = new List<StaffModel>();

            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("HR.usp_HR_GetStaff", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@RoleId", (object?)roleId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IsActive", (object?)isActive ?? DBNull.Value);

                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new StaffModel
                            {
                                StaffId = Convert.ToInt32(reader["StaffId"]),
                                FirstName = reader["FirstName"].ToString(),
                                LastName = reader["LastName"].ToString(),
                                Phone = reader["Phone"] == DBNull.Value ? null : reader["Phone"].ToString(),
                                Email = reader["Email"] == DBNull.Value ? null : reader["Email"].ToString(),
                                RoleId = Convert.ToInt32(reader["RoleId"]),
                                RoleName = reader["RoleName"].ToString(),
                                DOJ = reader["DOJ"] == DBNull.Value ? null : Convert.ToDateTime(reader["DOJ"]),
                                IsActive = Convert.ToBoolean(reader["IsActive"]),
                                Salary = reader["Salary"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Salary"]),
                                ProfilePhoto = reader["ProfilePhoto"] == DBNull.Value ? null : reader["ProfilePhoto"].ToString(),
                                BankName = reader["BankName"] == DBNull.Value ? null : reader["BankName"].ToString(),
                                AccountNumber = reader["AccountNumber"] == DBNull.Value ? null : reader["AccountNumber"].ToString(),
                                IFSCCode = reader["IFSCCode"] == DBNull.Value ? null : reader["IFSCCode"].ToString(),
                                CenterId = reader["CenterId"] == DBNull.Value ? null : Convert.ToInt32(reader["CenterId"]),
                                CenterName = reader["CenterName"] == DBNull.Value ? null : reader["CenterName"].ToString(),
                                PlantId = reader["PlantId"] == DBNull.Value ? null : Convert.ToInt32(reader["PlantId"]),
                                PlantName = reader["PlantName"] == DBNull.Value ? null : reader["PlantName"].ToString()
                            });
                        }
                    }
                }
            }

            return list;
        }

        public void ToggleStaffActive(int staffId, bool isActive)
        {
            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("HR.usp_HR_ToggleActive", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@StaffId", staffId);
                    cmd.Parameters.AddWithValue("@IsActive", isActive);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }
        public List<StaffModel> GetUnlinkedStaff()
        {
            var list = new List<StaffModel>();

            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand(@"
            SELECT s.StaffId, s.FirstName, s.LastName, s.RoleId, r.RoleName
            FROM HR.Staffs s
            INNER JOIN Admin.Roles r ON r.RoleId = s.RoleId
            WHERE s.IsActive = 1
              AND s.StaffId NOT IN (
                  SELECT StaffId FROM Admin.Users WHERE StaffId IS NOT NULL
              )
            ORDER BY s.LastName, s.FirstName", con))
                {
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new StaffModel
                            {
                                StaffId = Convert.ToInt32(reader["StaffId"]),
                                FirstName = reader["FirstName"].ToString(),
                                LastName = reader["LastName"].ToString(),
                                RoleId = Convert.ToInt32(reader["RoleId"]),
                                RoleName = reader["RoleName"].ToString()
                            });
                        }
                    }
                }
            }

            return list;
        }
        public StaffModel GetStaffById(int staffId)
        {
            StaffModel staff = null;

            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("HR.usp_HR_GetStaffById", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@StaffId", staffId);

                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            staff = new StaffModel
                            {
                                StaffId = Convert.ToInt32(reader["StaffId"]),
                                FirstName = reader["FirstName"].ToString(),
                                LastName = reader["LastName"].ToString(),
                                Phone = reader["Phone"] == DBNull.Value ? null : reader["Phone"].ToString(),
                                Email = reader["Email"] == DBNull.Value ? null : reader["Email"].ToString(),
                                RoleId = Convert.ToInt32(reader["RoleId"]),
                                RoleName = reader["RoleName"].ToString(),
                                DOJ = reader["DOJ"] == DBNull.Value ? null : Convert.ToDateTime(reader["DOJ"]),
                                IsActive = Convert.ToBoolean(reader["IsActive"]),
                                Salary = reader["Salary"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Salary"]),
                                ProfilePhoto = reader["ProfilePhoto"] == DBNull.Value ? null : reader["ProfilePhoto"].ToString(),
                                BankName = reader["BankName"] == DBNull.Value ? null : reader["BankName"].ToString(),
                                AccountNumber = reader["AccountNumber"] == DBNull.Value ? null : reader["AccountNumber"].ToString(),
                                IFSCCode = reader["IFSCCode"] == DBNull.Value ? null : reader["IFSCCode"].ToString(),
                                CenterId = reader["CenterId"] == DBNull.Value ? null : Convert.ToInt32(reader["CenterId"]),
                                CenterName = reader["CenterName"] == DBNull.Value ? null : reader["CenterName"].ToString(),
                                PlantId = reader["PlantId"] == DBNull.Value ? null : Convert.ToInt32(reader["PlantId"]),
                                PlantName = reader["PlantName"] == DBNull.Value ? null : reader["PlantName"].ToString()
                            };
                        }
                    }
                }
            }

            return staff;
        }
        public void UpdateStaff(int staffId, string firstName, string lastName,
                        string phone, string email, int roleId, DateTime? doj,
                        string bankName, string accountNumber, string ifscCode,
                        decimal salary, string profilePhoto,
                        int? centerId, int? plantId)
        {
            using (SqlConnection con = _db.GetConnection())
            {
                con.Open();

                // ── Step 1: Get existing BankAccountId for this staff ──
                int? bankAccountId = null;
                using (SqlCommand getBank = new SqlCommand(
                    "SELECT BankAccountId FROM HR.Staffs WHERE StaffId = @StaffId", con))
                {
                    getBank.Parameters.AddWithValue("@StaffId", staffId);
                    var result = getBank.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                        bankAccountId = Convert.ToInt32(result);
                }

                // ── Step 2: Update or Insert bank account ──
                if (!string.IsNullOrEmpty(bankName) &&
                    !string.IsNullOrEmpty(accountNumber) &&
                    !string.IsNullOrEmpty(ifscCode))
                {
                    if (bankAccountId.HasValue)
                    {
                        // Update existing bank account
                        using (SqlCommand bankCmd = new SqlCommand(@"
                    UPDATE Finance.BankAccounts
                    SET BankName      = @BankName,
                        AccountNumber = @AccountNumber,
                        IFSCCode      = @IFSCCode
                    WHERE BankAccountId = @BankAccountId", con))
                        {
                            bankCmd.Parameters.AddWithValue("@BankName", bankName);
                            bankCmd.Parameters.AddWithValue("@AccountNumber", accountNumber);
                            bankCmd.Parameters.AddWithValue("@IFSCCode", ifscCode);
                            bankCmd.Parameters.AddWithValue("@BankAccountId", bankAccountId.Value);
                            bankCmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        // Insert new bank account and get new ID
                        using (SqlCommand bankCmd = new SqlCommand(@"
                    INSERT INTO Finance.BankAccounts (BankName, AccountNumber, IFSCCode)
                    VALUES (@BankName, @AccountNumber, @IFSCCode);
                    SELECT SCOPE_IDENTITY();", con))
                        {
                            bankCmd.Parameters.AddWithValue("@BankName", bankName);
                            bankCmd.Parameters.AddWithValue("@AccountNumber", accountNumber);
                            bankCmd.Parameters.AddWithValue("@IFSCCode", ifscCode);
                            bankAccountId = Convert.ToInt32(bankCmd.ExecuteScalar());
                        }
                    }
                }

                // ── Step 3: Update HR.Staffs — all fields ──
                using (SqlCommand cmd = new SqlCommand(@"
            UPDATE HR.Staffs
            SET FirstName    = @FirstName,
                LastName     = @LastName,
                Phone        = @Phone,
                Email        = @Email,
                RoleId       = @RoleId,
                DOJ          = @DOJ,
                Salary       = @Salary,
                ProfilePhoto = CASE WHEN @ProfilePhoto IS NOT NULL
                                    THEN @ProfilePhoto
                                    ELSE ProfilePhoto END,
                BankAccountId = CASE WHEN @BankAccountId IS NOT NULL
                                     THEN @BankAccountId
                                     ELSE BankAccountId END,
                CenterId     = @CenterId,
                PlantId      = @PlantId
            WHERE StaffId = @StaffId", con))
                {
                    cmd.Parameters.AddWithValue("@StaffId", staffId);
                    cmd.Parameters.AddWithValue("@FirstName", firstName);
                    cmd.Parameters.AddWithValue("@LastName", lastName);
                    cmd.Parameters.AddWithValue("@Phone", (object?)phone ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Email", (object?)email ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@RoleId", roleId);
                    cmd.Parameters.AddWithValue("@DOJ", (object?)doj ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Salary", salary);
                    cmd.Parameters.AddWithValue("@ProfilePhoto", (object?)profilePhoto ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@BankAccountId", (object?)bankAccountId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CenterId", (object?)centerId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PlantId", (object?)plantId ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        // ════════════════════════════════════════════════════════
        // PLANT
        // ════════════════════════════════════════════════════════
        public int AddPlant(string PlantName, string Location)
        {
            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Production.usp_Production_AddPlant", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@PlantName", PlantName);
                    cmd.Parameters.AddWithValue("@Location", Location);
                    con.Open();
                    var result = cmd.ExecuteScalar();
                    return Convert.ToInt32(result);
                }
            }
        }

        public List<PlantModel> GetAllPlants(bool? isActive = true)
        {
            var list = new List<PlantModel>();
            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand("Production.usp_Production_GetPlants", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@IsActive", (object)(isActive.HasValue ? (object)(isActive.Value ? 1 : 0) : DBNull.Value));
                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new PlantModel
                        {
                            PlantId = Convert.ToInt32(reader["PlantId"]),
                            PlantName = reader["PlantName"].ToString(),
                            Location = reader["Location"].ToString(),
                            IsActive = Convert.ToBoolean(reader["IsActive"])
                        });
                    }
                }
            }
            return list;
        }

        public void TogglePlant(int id, bool isActive)
        {
            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand("Production.usp_Production_TogglePlant", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@PlantId", id);
                cmd.Parameters.AddWithValue("@IsActive", isActive ? 1 : 0);
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void DeletePlant(int id)
        {
            using (SqlConnection con = _db.GetConnection())
            {
                string query = "delete from Production.ProcessingPlants where PlantId=@id";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void UpdatePlant(PlantModel plant)
        {
            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand("Production.usp_Production_UpdatePlant", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@PlantId", plant.PlantId);
                cmd.Parameters.AddWithValue("@PlantName", plant.PlantName);
                cmd.Parameters.AddWithValue("@Location", (object)plant.Location ?? DBNull.Value);
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }
        public PlantModel getPlantById(int id)
        {
            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand("Production.usp_Production_GetPlantById", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@PlantId", id);
                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new PlantModel
                        {
                            PlantId = Convert.ToInt32(reader["PlantId"]),
                            PlantName = reader["PlantName"].ToString(),
                            Location = reader["Location"].ToString(),
                            IsActive = Convert.ToBoolean(reader["IsActive"])
                        };
                    }
                }
            }
            return null;
        }
        // ════════════════════════════════════════════════════════
        // COLLECTION
        // ════════════════════════════════════════════════════════

        public int AddCollection(string CenterName, int VillageID, decimal Capacity, string Location)
        {
            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Collection.usp_Collection_AddCenter", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@CenterName", CenterName);
                    cmd.Parameters.AddWithValue("@VillageID", VillageID);
                    cmd.Parameters.AddWithValue("@Capacity", Capacity);
                    cmd.Parameters.AddWithValue("@Location", Location);
                    con.Open();
                    var result = cmd.ExecuteScalar();
                    return Convert.ToInt32(result);
                }
            }
        }
        public List<CollectionCenterModel> GetAllCollection(bool? isActive = true)
        {
            var list = new List<CollectionCenterModel>();
            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand("Collection.usp_Collection_GetCenters", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new CollectionCenterModel
                        {
                            CenterId = Convert.ToInt32(reader["CenterId"]),
                            CenterName = reader["CenterName"].ToString(),
                            VillageId = Convert.ToInt32(reader["VillageId"]),
                            Capacity = Convert.ToInt32(reader["Capacity"]),
                            Location = reader["Location"].ToString(),
                            IsActive = Convert.ToBoolean(reader["IsActive"])
                        });
                    }
                }
            }
            return list;
        }
        public void ToggleCollection(int id, bool isActive)
        {
            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand("Collection.usp_Collection_ToggleCenter", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@CenterId", id);
                cmd.Parameters.AddWithValue("@IsActive", isActive ? 1 : 0);
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }
        public void UpdateCollection(CollectionCenterModel collection)
        {
            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Collection.usp_Collection_UpdateCenter", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@CenterId", collection.CenterId);
                    cmd.Parameters.AddWithValue("@CenterName", collection.CenterName);
                    cmd.Parameters.AddWithValue("@Capacity", collection.Capacity);
                    cmd.Parameters.AddWithValue("@VillageId", collection.VillageId);
                    cmd.Parameters.AddWithValue("@Location", collection.Location);
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }
        public CollectionCenterModel getCollectionById(int id)
        {
            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand("Collection.usp_Collection_GetCenterById", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@CenterId", id);
                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new CollectionCenterModel
                        {
                            CenterId = Convert.ToInt32(reader["CenterId"]),
                            CenterName = reader["CenterName"].ToString(),
                            VillageId = Convert.ToInt32(reader["VillageId"]),
                            Capacity = Convert.ToInt32(reader["Capacity"]),
                            Location = reader["Location"].ToString(),
                            IsActive = Convert.ToBoolean(reader["IsActive"])
                        };
                    }
                }
            }
            return null;
        }
        // ════════════════════════════════════════════════════════
        // Production
        // ════════════════════════════════════════════════════════

        public int AddProduct(string productName, string productType, decimal mrp,
                              string unit, int? shelfLifeDays, string description,
                              int createdBy)
        {
            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Production.usp_Production_AddProduct", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ProductName", productName);
                    cmd.Parameters.AddWithValue("@ProductType", productType);
                    cmd.Parameters.AddWithValue("@MRP", mrp);
                    cmd.Parameters.AddWithValue("@Unit", unit);
                    cmd.Parameters.AddWithValue("@ShelfLifeDays", (object?)shelfLifeDays ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Description", (object?)description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CreatedBy", createdBy);

                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                            return Convert.ToInt32(reader["ProductId"]);
                    }
                }
            }
            return 0;
        }

        // ── Get All Products ──────────────────────────────────
        public List<ProductModel> GetAllProducts(string productType = null, bool? isActive = true)
        {
            var list = new List<ProductModel>();

            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Production.usp_Production_GetAllProducts", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ProductType", (object?)productType ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IsActive", (object?)isActive ?? DBNull.Value);

                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            list.Add(MapProduct(reader));
                    }
                }
            }

            return list;
        }

        // ── Get Product By ID ─────────────────────────────────
        public ProductModel GetProductById(int productId)
        {
            ProductModel product = null;

            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Production.usp_Production_GetProductById", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ProductId", productId);

                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                            product = MapProduct(reader);
                    }
                }
            }

            return product;
        }

        // ── Get Product Types for dropdown ────────────────────
        public List<string> GetProductTypes()
        {
            var list = new List<string>();

            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Production.usp_Production_GetProductTypes", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            list.Add(reader["ProductType"].ToString());
                    }
                }
            }

            return list;
        }

        // ── Update Product ────────────────────────────────────
        public void UpdateProduct(int productId, string productName, string productType,
                          decimal mrp, string unit, int? shelfLifeDays,
                          string description, int modifiedBy)
        {
            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Production.usp_Production_UpdateProduct", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ProductId", productId);
                    cmd.Parameters.AddWithValue("@ProductName", productName);
                    cmd.Parameters.AddWithValue("@ProductType", productType);
                    cmd.Parameters.AddWithValue("@MRP", mrp);
                    cmd.Parameters.AddWithValue("@Unit", unit);
                    cmd.Parameters.AddWithValue("@ShelfLifeDays", (object?)shelfLifeDays ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Description", (object?)description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ModifiedBy", modifiedBy);

                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        reader.Read();
                    }
                }
            }
        }

        // ── Toggle Active / Inactive ──────────────────────────
        public void ToggleProductStatus(int productId, bool isActive, int modifiedBy)
        {
            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Production.usp_Production_ToggleProductStatus", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ProductId", productId);
                    cmd.Parameters.AddWithValue("@IsActive", isActive);
                    cmd.Parameters.AddWithValue("@ModifiedBy", modifiedBy);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ── Private mapper ────────────────────────────────────
        private static ProductModel MapProduct(SqlDataReader reader)
        {
            return new ProductModel
            {
                ProductId = Convert.ToInt32(reader["ProductId"]),
                ProductName = reader["ProductName"].ToString(),
                ProductType = reader["ProductType"].ToString(),
                MRP = Convert.ToDecimal(reader["MRP"]),
                Unit = reader["Unit"].ToString(),
                ShelfLifeDays = reader["ShelfLifeDays"] == DBNull.Value ? null : Convert.ToInt32(reader["ShelfLifeDays"]),
                Description = reader["Description"] == DBNull.Value ? null : reader["Description"].ToString(),
                IsActive = Convert.ToBoolean(reader["IsActive"]),
                CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                CreatedBy = reader["CreatedBy"] == DBNull.Value ? null : Convert.ToInt32(reader["CreatedBy"]),
                CreatedByName = reader["CreatedByName"] == DBNull.Value ? null : reader["CreatedByName"].ToString(),
                ModifiedDate = reader["ModifiedDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["ModifiedDate"]),
                ModifiedBy = reader["ModifiedBy"] == DBNull.Value ? null : Convert.ToInt32(reader["ModifiedBy"]),
                ModifiedByName = reader["ModifiedByName"] == DBNull.Value ? null : reader["ModifiedByName"].ToString()
            };
        }

        //Added By Zaid

        public int? GetPlantIdByUser(int userId)
        {
            using (var con = _db.GetConnection())
            using (var cmd = new SqlCommand(
                "SELECT PlantId FROM Admin.UserPlants WHERE UserId = @UserId", con))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                con.Open();
                var result = cmd.ExecuteScalar();
                return result == null || result == DBNull.Value
                       ? null
                       : Convert.ToInt32(result);
            }
        }
        public List<ProductModel> GetActiveProducts()
        {
            var list = new List<ProductModel>();
            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Production.usp_Production_GetActiveProducts", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ProductType", DBNull.Value);
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new ProductModel
                            {
                                ProductId = Convert.ToInt32(reader["ProductId"]),
                                ProductName = reader["ProductName"].ToString(),
                                ProductType = reader["ProductType"].ToString(),
                                Unit = reader["Unit"].ToString()
                            });
                        }
                    }
                }
            }
            return list;
        }
        public List<ProductionBatchModel> GetProductionBatches(int? plantId = null, int? productId = null,
                                                        string batchStatus = null,
                                                        DateTime? fromDate = null, DateTime? toDate = null)
        {
            var list = new List<ProductionBatchModel>();

            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Production.usp_Production_GetProductionBatches", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@PlantId", (object?)plantId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ProductId", (object?)productId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@BatchStatus", (object?)batchStatus ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FromDate", (object?)fromDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ToDate", (object?)toDate ?? DBNull.Value);

                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new ProductionBatchModel
                            {
                                ProductionBatchId = Convert.ToInt32(reader["ProductionBatchId"]),
                                ProductionDate = Convert.ToDateTime(reader["ProductionDate"]),
                                MilkUsedQuantity = Convert.ToDecimal(reader["MilkUsedQuantity"]),
                                BatchStatus = reader["BatchStatus"].ToString(),
                                ProductId = Convert.ToInt32(reader["ProductId"]),
                                ProductName = reader["ProductName"].ToString(),
                                ProductType = reader["ProductType"].ToString(),
                                Unit = reader["Unit"].ToString(),
                                PlantId = Convert.ToInt32(reader["PlantId"]),
                                PlantName = reader["PlantName"].ToString()
                            });
                        }
                    }
                }
            }

            return list;
        }
        public List<MilkTransferModel> GetMilkTransfers(int? plantId = null, int? centerId = null,
                                                  DateTime? fromDate = null, DateTime? toDate = null)
        {
            var list = new List<MilkTransferModel>();

            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Production.usp_Production_GetMilkTransfers", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@PlantId", (object?)plantId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CenterId", (object?)centerId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FromDate", (object?)fromDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ToDate", (object?)toDate ?? DBNull.Value);

                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new MilkTransferModel
                            {
                                TransferId = Convert.ToInt32(reader["TransferId"]),
                                DispatchDate = Convert.ToDateTime(reader["DispatchDate"]),
                                ReceivedDate = reader["ReceivedDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["ReceivedDate"]),
                                DispatchQty = Convert.ToDecimal(reader["DispatchQty"]),
                                ReceivedQty = reader["ReceivedQty"] == DBNull.Value ? null : Convert.ToDecimal(reader["ReceivedQty"]),
                                LossQty = reader["LossQty"] == DBNull.Value ? null : Convert.ToDecimal(reader["LossQty"]),
                                LossPercent = Convert.ToDecimal(reader["LossPercent"]),
                                TransferStatus = reader["TransferStatus"].ToString(),
                                CenterId = Convert.ToInt32(reader["CenterId"]),
                                CenterName = reader["CenterName"].ToString(),
                                PlantId = Convert.ToInt32(reader["PlantId"]),
                                PlantName = reader["PlantName"].ToString(),
                                VehicleId = Convert.ToInt32(reader["VehicleId"]),
                                VehicleNumber = reader["VehicleNumber"].ToString(),
                                VehicleCapacity = reader["VehicleCapacity"] == DBNull.Value ? null : Convert.ToDecimal(reader["VehicleCapacity"]),
                                DriverId = reader["DriverId"] == DBNull.Value ? null : Convert.ToInt32(reader["DriverId"]),
                                DriverName = reader["DriverName"] == DBNull.Value ? null : reader["DriverName"].ToString(),
                                DriverPhone = reader["DriverPhone"] == DBNull.Value ? null : reader["DriverPhone"].ToString(),
                                TestedFat = reader["TestedFat"] == DBNull.Value ? null : Convert.ToDecimal(reader["TestedFat"]),
                                TestedCLR = reader["TestedCLR"] == DBNull.Value ? null : Convert.ToDecimal(reader["TestedCLR"]),
                                TestDate = reader["TestDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["TestDate"]),
                                BatchId = Convert.ToInt32(reader["BatchId"]),
                                Shift = reader["Shift"].ToString(),
                                BatchDate = Convert.ToDateTime(reader["BatchDate"]),
                                BatchAvgFat = reader["BatchAvgFat"] == DBNull.Value ? null : Convert.ToDecimal(reader["BatchAvgFat"]),
                                BatchAvgCLR = reader["BatchAvgCLR"] == DBNull.Value ? null : Convert.ToDecimal(reader["BatchAvgCLR"])
                            });
                        }
                    }
                }
            }

            return list;
        }


        //Collection center

        public List<CollectionCenterModel> GetAllCenters()
        {
            var list = new List<CollectionCenterModel>();
            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT CenterId, CenterName, Location FROM Collection.CollectionCenters ORDER BY CenterName", con))
                {
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new CollectionCenterModel
                            {
                                CenterId = Convert.ToInt32(reader["CenterId"]),
                                CenterName = reader["CenterName"].ToString(),
                                Location = reader["Location"] == DBNull.Value ? null : reader["Location"].ToString()
                            });
                        }
                    }
                }
            }
            return list;
        }


        //Distributors

        public List<Distributor> GetDistributors()
        {
            var list = new List<Distributor>();
            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Sales.usp_Sales_GetDistributors", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new Distributor
                            {
                                DistributorId = Convert.ToInt32(reader["DistributorId"]),
                                DistributorName = reader["DistributorName"].ToString(),
                                Location = reader["Location"]?.ToString(),
                                ContactNumber = reader["ContactNumber"]?.ToString(),
                                Email = reader["Email"]?.ToString(),
                                Address = reader["Address"]?.ToString(),
                                GSTIN = reader["GSTIN"]?.ToString(),
                                Status = reader["Status"]?.ToString() ?? "Pending"
                            });
                        }
                    }
                }
                return list;
            }
        }
        public int AddDistributor(Distributor distributor)
        {
            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Sales.usp_Sales_AddDistributor", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    con.Open();

                    cmd.Parameters.AddWithValue("@DistributorName", distributor.DistributorName);
                    cmd.Parameters.AddWithValue("@Location", (object?)distributor.Location ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ContactNumber", (object?)distributor.ContactNumber ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Email", (object?)distributor.Email ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Address", (object?)distributor.Address ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@GSTIN", (object?)distributor.GSTIN ?? DBNull.Value);

                    var result = cmd.ExecuteScalar();

                    return Convert.ToInt32(result);
                }
            }
        }
        public bool UpdateDistributorStatus(int distributorId, string status)
        {
            using (SqlConnection con = _db.GetConnection())
            {
                using var cmd = new SqlCommand("Sales.usp_Sales_UpdateDistributorStatus", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@DistributorId", distributorId);
                cmd.Parameters.AddWithValue("@Status", status);
                con.Open();
                return cmd.ExecuteNonQuery() > 0;
            }

        }

        public Distributor? GetDistributorById(int distributorId)
        {
            using (SqlConnection con = _db.GetConnection())
            {

                using var cmd = new SqlCommand("Sales.usp_Sales_GetDistributorById", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@DistributorId", distributorId);
                con.Open();
                using var dr = cmd.ExecuteReader();
                return dr.Read() ? MapDistributor(dr) : null;
            }
        }
        public List<Distributor> GetPendingDistributors()
        {
            var list = new List<Distributor>();
            using (SqlConnection con = _db.GetConnection())
            {
                using var cmd = new SqlCommand("Sales.usp_Sales_GetPendingDistributors", con);
                cmd.CommandType = CommandType.StoredProcedure;
                con.Open();
                using var dr = cmd.ExecuteReader();
                while (dr.Read())
                    list.Add(MapDistributor(dr));
                return list;
            }
        }

        // ── private helper — keeps all Read() calls in one place ──
        private static Distributor MapDistributor(SqlDataReader dr) => new()
        {
            DistributorId = (int)dr["DistributorId"],
            DistributorName = dr["DistributorName"].ToString()!,
            Location = dr["Location"] as string,
            ContactNumber = dr["ContactNumber"] as string,
            Email = dr["Email"] as string,
            Address = dr["Address"] as string,
            GSTIN = dr["GSTIN"] as string,
            Status = dr["Status"].ToString()!,
            RegisteredOn = (DateTime)dr["RegisteredOn"]
        };

        //Assign plant and collection to user

        public List<UserAssignmentViewModel> GetAllUserPlantAssignments()
        {
            var list = new List<UserAssignmentViewModel>();

            using (SqlConnection con = _db.GetConnection())
            {
                con.Open();
                string query = @"
            SELECT
                u.UserId,
                u.Username,
                r.RoleName,
                up.UserPlantId,
                up.PlantId,
                pp.PlantName
            FROM Admin.Users u
            INNER JOIN Admin.Roles r
                ON r.RoleId = u.RoleId
            LEFT JOIN Admin.UserPlants up
                ON up.UserId = u.UserId
            LEFT JOIN Production.ProcessingPlants pp
                ON pp.PlantId = up.PlantId
            ORDER BY u.Username";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            list.Add(new UserAssignmentViewModel
                            {
                                UserId = Convert.ToInt32(dr["UserId"]),
                                Username = dr["Username"].ToString(),
                                RoleName = dr["RoleName"].ToString(),
                                UserPlantId = dr["UserPlantId"] == DBNull.Value ? null : Convert.ToInt32(dr["UserPlantId"]),
                                PlantId = dr["PlantId"] == DBNull.Value ? null : Convert.ToInt32(dr["PlantId"]),
                                PlantName = dr["PlantName"] == DBNull.Value ? null : dr["PlantName"].ToString()
                            });
                        }
                    }
                }
            }

            return list;
        }

        public List<UserAssignmentViewModel> GetAllUserCenterAssignments()
        {
            var list = new List<UserAssignmentViewModel>();

            using (SqlConnection con = _db.GetConnection())
            {
                con.Open();
                string query = @"
            SELECT
                u.UserId,
                u.Username,
                r.RoleName,
                uc.UserCenterId,
                uc.CenterId,
                cc.CenterName
            FROM Admin.Users u
            INNER JOIN Admin.Roles r
                ON r.RoleId = u.RoleId
            LEFT JOIN Admin.UserCenters uc
                ON uc.UserId = u.UserId
            LEFT JOIN Collection.CollectionCenters cc
                ON cc.CenterId = uc.CenterId
            ORDER BY u.Username";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            list.Add(new UserAssignmentViewModel
                            {
                                UserId = Convert.ToInt32(dr["UserId"]),
                                Username = dr["Username"].ToString(),
                                RoleName = dr["RoleName"].ToString(),
                                UserCenterId = dr["UserCenterId"] == DBNull.Value ? null : Convert.ToInt32(dr["UserCenterId"]),
                                CenterId = dr["CenterId"] == DBNull.Value ? null : Convert.ToInt32(dr["CenterId"]),
                                CenterName = dr["CenterName"] == DBNull.Value ? null : dr["CenterName"].ToString()
                            });
                        }
                    }
                }
            }

            return list;
        }


        public int CreateOrder(AdminOrderModel model)
        {
            int orderId = 0;
            using (SqlConnection con = _db.GetConnection())
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand("Sales.usp_Sales_CreateOrder", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@DistributorId", model.DistributorId);
                    cmd.Parameters.AddWithValue("@PlantId", model.PlantId);   // added
                    cmd.Parameters.AddWithValue("@OrderDate", model.OrderDate.Date);
                    orderId = Convert.ToInt32(cmd.ExecuteScalar());
                }
                using (SqlCommand cmd = new SqlCommand("Sales.usp_Sales_AddOrderDetail", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@OrderId", orderId);
                    cmd.Parameters.AddWithValue("@ProductId", model.ProductId);
                    cmd.Parameters.AddWithValue("@Quantity", model.Quantity);
                    cmd.Parameters.AddWithValue("@UnitPrice", model.UnitPrice);
                    cmd.ExecuteNonQuery();
                }
            }
            return orderId;
        }

        public List<PlantModel> GetActivePlants()
        {
            var list = new List<PlantModel>();
            using (SqlConnection con = _db.GetConnection())
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand("Production.usp_Production_GetPlants", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@IsActive", 1);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            list.Add(new PlantModel
                            {
                                PlantId = Convert.ToInt32(dr["PlantId"]),
                                PlantName = dr["PlantName"].ToString()!,
                                Location = dr["Location"]?.ToString()
                            });
                        }
                    }
                }
            }
            return list;
        }
        public List<OrderSummary> GetAllOrders(int? distributorId, string? status, DateTime? fromDate, DateTime? toDate)
        {
            var list = new List<OrderSummary>();
            using (SqlConnection con = _db.GetConnection())
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand("Sales.usp_Sales_GetOrders", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@DistributorId", (object?)distributorId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@OrderStatus", (object?)status ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FromDate", (object?)fromDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ToDate", (object?)toDate ?? DBNull.Value);

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            list.Add(new OrderSummary
                            {
                                OrderId = Convert.ToInt32(dr["OrderId"]),
                                OrderDate = Convert.ToDateTime(dr["OrderDate"]),
                                OrderStatus = dr["OrderStatus"].ToString()!,
                                TotalAmount = Convert.ToDecimal(dr["TotalAmount"]),
                                DistributorName = dr["DistributorName"].ToString()!,
                                Location = dr["Location"]?.ToString(),
                                ContactNumber = dr["ContactNumber"]?.ToString()
                            });
                        }
                    }
                }
            }
            return list;
        }

        public bool UpdateOrderStatus(int orderId, string status)
        {
            using (SqlConnection con = _db.GetConnection())
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand("Sales.usp_Sales_UpdateOrderStatus", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@OrderId", orderId);
                    cmd.Parameters.AddWithValue("@OrderStatus", status);
                    cmd.ExecuteNonQuery();
                }
            }
            return true;
        }

        //Notification

        public List<NotificationModel> GetNotifications()
        {
            var list = new List<NotificationModel>();
            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand("Admin.usp_Notifications_GetUnread", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                con.Open();
                using var dr = cmd.ExecuteReader();
                while (dr.Read())
                    list.Add(new NotificationModel
                    {
                        NotificationId = Convert.ToInt32(dr["NotificationId"]),
                        Category = dr["Category"].ToString(),
                        Title = dr["Title"].ToString(),
                        Message = dr["Message"].ToString(),
                        EntityId = dr["EntityId"] == DBNull.Value ? 0 : Convert.ToInt32(dr["EntityId"]),
                        EntityType = dr["EntityType"] == DBNull.Value ? "" : dr["EntityType"].ToString(),
                        CreatedAt = Convert.ToDateTime(dr["CreatedAt"]),
                        Severity = dr["Severity"].ToString(),
                        ActionUrl = dr["ActionUrl"] == DBNull.Value ? "#" : dr["ActionUrl"].ToString(),
                        IsRead = false
                    });
            }
            return list;
        }

        public int GetNotificationCount()
        {
            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand("Admin.usp_Notifications_GetUnreadCount", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                con.Open();
                var result = cmd.ExecuteScalar();
                return result == DBNull.Value ? 0 : Convert.ToInt32(result);
            }
        }

        public bool MarkNotificationRead(int notificationId)
        {
            try
            {
                using (SqlConnection con = _db.GetConnection())
                using (SqlCommand cmd = new SqlCommand("Admin.usp_Notifications_MarkRead", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@NotificationId", notificationId);
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
                return true;
            }
            catch { return false; }
        }

        public bool MarkAllNotificationsRead()
        {
            try
            {
                using (SqlConnection con = _db.GetConnection())
                using (SqlCommand cmd = new SqlCommand("Admin.usp_Notifications_MarkAllRead", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
                return true;
            }
            catch { return false; }
        }

    }
}
