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
                    string staffType, DateTime? doj,
                    string bankName, string accountNumber, string ifscCode,
                    string profilePhoto = null)
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
                    cmd.Parameters.AddWithValue("@StaffType", staffType);
                    cmd.Parameters.AddWithValue("@DOJ", (object?)doj ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@BankName", (object?)bankName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@AccountNumber", (object?)accountNumber ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IFSCCode", (object?)ifscCode ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ProfilePhoto", (object?)profilePhoto ?? DBNull.Value);

                    con.Open();
                    var result = cmd.ExecuteScalar();
                    return Convert.ToInt32(result);
                }
            }
        }

        public List<StaffModel> GetAllStaff(string staffType = null, bool? isActive = null)
        {
            var list = new List<StaffModel>();

            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("HR.usp_HR_GetStaff", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@StaffType", (object?)staffType ?? DBNull.Value);
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
                                StaffType = reader["StaffType"].ToString(),
                                DOJ = reader["DOJ"] == DBNull.Value ? null : Convert.ToDateTime(reader["DOJ"]),
                                IsActive = Convert.ToBoolean(reader["IsActive"]),
                                BankName = reader["BankName"] == DBNull.Value ? null : reader["BankName"].ToString(),
                                AccountNumber = reader["AccountNumber"] == DBNull.Value ? null : reader["AccountNumber"].ToString()
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
        // ════════════════════════════════════════════════════════
        // PLANT
        // ════════════════════════════════════════════════════════
        public int AddPlant(string PlantName,string Location)
        {
            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Production.usp_Production_AddPlant", con))
                {
                    cmd.CommandType=CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@PlantName", PlantName);
                    cmd.Parameters.AddWithValue("@Location", Location);
                    con.Open();
                    var result=cmd.ExecuteScalar();
                    return Convert.ToInt32(result);
                }
            }
        }

        public List<PlantModel> GetAllPlants()
        {
            var list = new List<PlantModel>();    
            using (SqlConnection con = _db.GetConnection())
            {
                string query = "select * from Production.ProcessingPlants";
                using (SqlCommand cmd = new SqlCommand(query,con))
                {
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
                            });
                        }
                    }
                }
            }
            return list;
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
            {
                string query = @"UPDATE Production.ProcessingPlants 
                         SET PlantName = @PlantName,
                             Location  = @Location
                         WHERE PlantId = @PlantId";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    cmd.Parameters.AddWithValue("@PlantId", plant.PlantId);
                    cmd.Parameters.AddWithValue("@PlantName", plant.PlantName);
                    cmd.Parameters.AddWithValue("@Location", plant.Location);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public PlantModel getPlantById(int id)
        {
            PlantModel plant=new PlantModel();
            using (SqlConnection con = _db.GetConnection())
            {
                string query = "select * from Production.ProcessingPlants where PlantId=@id";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    cmd.Parameters.AddWithValue("@id", id);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if(reader.Read())
                        {
                            plant = new PlantModel
                            {
                                PlantId = Convert.ToInt32(reader["PlantId"]),
                                PlantName = reader["PlantName"].ToString(),
                                Location = reader["Location"].ToString(),
                            };
                        }
                    }
                }
            }
            return plant;
        }
    }
}
