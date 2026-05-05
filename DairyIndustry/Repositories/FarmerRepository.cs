using DairyIndustry.Data;
using DairyIndustry.Models.Admin;
using DairyIndustry.Models.Collection;
using DairyIndustry.Models.FarmerModel;
using DairyIndustry.Models.ViewModels;
using DairyIndustry.Repositories;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DairyIndustry.Repository
{
    public class FarmerRepository : IFarmerRepository
    {
        private readonly DbHelper _dbHelper;

        public FarmerRepository(DbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        // =========================
        // GET STATES
        // =========================
        public List<StateModel> GetStates()
        {
            var list = new List<StateModel>();

            using (SqlConnection con = _dbHelper.GetConnection())
            using (SqlCommand cmd = new SqlCommand("Location.usp_Location_GetStates", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                con.Open();

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        list.Add(new StateModel
                        {
                            StateId = (int)dr["StateId"],
                            StateName = dr["StateName"].ToString()
                        });
                    }
                }
            }

            return list;
        }

        // =========================
        // GET CITIES BY STATE
        // =========================
        public List<CityModel> GetCitiesByState(int stateId)
        {
            var list = new List<CityModel>();

            using (SqlConnection con = _dbHelper.GetConnection())
            using (SqlCommand cmd = new SqlCommand("Location.usp_Location_GetCitiesByState", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@StateId", stateId);

                con.Open();

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        list.Add(new CityModel
                        {
                            CityId = (int)dr["CityId"],
                            CityName = dr["CityName"].ToString()
                        });
                    }
                }
            }

            return list;
        }
        // =========================
        // GET VILLAGES BY CITY
        // =========================
        public List<VillageModel> GetVillagesByCity(int cityId)
        {
            var list = new List<VillageModel>();

            using (SqlConnection con = _dbHelper.GetConnection())
            using (SqlCommand cmd = new SqlCommand("Location.usp_Location_GetVillagesByCity", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@CityId", cityId);

                con.Open();

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        list.Add(new VillageModel
                        {
                            VillageId = (int)dr["VillageId"],
                            VillageName = dr["VillageName"].ToString()
                        });
                    }
                }
            }

            return list;
        }
        // =========================
        // ADD FARMER (UPDATED )
        // =========================
        public FarmerViewModel AddFarmer(FarmerViewModel model, int staffId)
        {
            using (SqlConnection con = _dbHelper.GetConnection())
            using (SqlCommand cmd = new SqlCommand("Farmer.usp_Center_RegisterFarmer", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@StaffId", staffId);
                cmd.Parameters.AddWithValue("@FarmerName", model.FarmerName);
                cmd.Parameters.AddWithValue("@VillageId", model.VillageId);
                cmd.Parameters.AddWithValue("@Phone", (object?)model.Phone ?? DBNull.Value);

                cmd.Parameters.AddWithValue("@BankName", (object?)model.BankName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@AccountNumber", (object?)model.AccountNumber ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IFSCCode", (object?)model.IFSCCode ?? DBNull.Value);

                cmd.Parameters.AddWithValue("@ProfilePhoto",
                    string.IsNullOrEmpty(model.ProfilePhoto) ? (object)DBNull.Value : model.ProfilePhoto);

                con.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        model.FarmerId = Convert.ToInt32(reader["NewFarmerId"]);
                        model.FarmerCode = reader["FarmerCode"].ToString();

                        if (!string.IsNullOrEmpty(model.Phone) && model.Phone.Length >= 4)
                        {
                            model.DefaultPassword = model.Phone.Substring(model.Phone.Length - 4);
                        }
                    }
                }
            }

            return model;
        }


        //  GET ALL FARMERS
        public List<FarmerViewModel> GetAllFarmers(int staffId, bool? isActive = null, string search = null)
        {
            var list = new List<FarmerViewModel>();

            using (SqlConnection con = _dbHelper.GetConnection())
            using (SqlCommand cmd = new SqlCommand("Farmer.usp_Center_GetAllFarmers", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@StaffId", staffId);
                cmd.Parameters.AddWithValue("@IsActive", (object)isActive ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Search", (object)search ?? DBNull.Value);

                con.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new FarmerViewModel
                        {
                            FarmerId = Convert.ToInt32(reader["FarmerId"]),
                            FarmerCode = reader["FarmerCode"].ToString(),
                            FarmerName = reader["FarmerName"].ToString(),
                            Phone = reader["Phone"]?.ToString(),
                            IsActive = Convert.ToBoolean(reader["IsActive"]),
                            ProfilePhoto = reader["ProfilePhoto"]?.ToString(),

                            //  IMPORTANT (your error fix)
                            VillageName = reader["VillageName"].ToString(),
                            CityName = reader["CityName"].ToString(),

                            BankName = reader["BankName"]?.ToString(),
                            AccountNumber = reader["AccountNumber"]?.ToString(),
                            IFSCCode= reader["IFSCCode"].ToString()
                        });
                    }
                }
            }

            return list;
        }

        // TOGGLE ACTIVE / INACTIVE
        public void ToggleFarmerStatus(int staffId, int farmerId, bool isActive)
        {
            using (SqlConnection con = _dbHelper.GetConnection())
            using (SqlCommand cmd = new SqlCommand("Farmer.usp_Center_ToggleFarmerActive", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@StaffId", staffId);
                cmd.Parameters.AddWithValue("@FarmerId", farmerId);
                cmd.Parameters.AddWithValue("@IsActive", isActive);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        //  UPDATE FARMER
        public int UpdateFarmer(FarmerViewModel model, int staffId)
        {
            using (SqlConnection con = _dbHelper.GetConnection())
            using (SqlCommand cmd = new SqlCommand("Farmer.usp_Center_UpdateFarmer", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@StaffId", staffId);
                cmd.Parameters.AddWithValue("@FarmerId", model.FarmerId);
                cmd.Parameters.AddWithValue("@FarmerName", model.FarmerName);
                cmd.Parameters.AddWithValue("@VillageId", model.VillageId);
                cmd.Parameters.AddWithValue("@Phone", (object)model.Phone ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ProfilePhoto", (object)model.ProfilePhoto ?? DBNull.Value);

                con.Open();

                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public FarmerViewModel GetFarmerById(int farmerId, int staffId)
        {
            FarmerViewModel model = null;

            using (SqlConnection con = _dbHelper.GetConnection())
            using (SqlCommand cmd = new SqlCommand("Farmer.usp_Center_GetFarmerById", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@FarmerId", farmerId);
                cmd.Parameters.AddWithValue("@StaffId", staffId);

                con.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        model = new FarmerViewModel
                        {
                            FarmerId = Convert.ToInt32(reader["FarmerId"]),
                            FarmerName = reader["FarmerName"].ToString(),
                            FarmerCode = reader["FarmerCode"].ToString(),
                            Phone = reader["Phone"]?.ToString(),

                            //  THIS IS THE MAIN FIX
                            StateId = reader["StateId"] != DBNull.Value ? Convert.ToInt32(reader["StateId"]) : (int?)null,
                            CityId = reader["CityId"] != DBNull.Value ? Convert.ToInt32(reader["CityId"]) : (int?)null,
                            VillageId = reader["VillageId"] != DBNull.Value ? Convert.ToInt32(reader["VillageId"]) : (int?)null,

                            // Optional (for display)
                            StateName = reader["StateName"]?.ToString(),
                            CityName = reader["CityName"]?.ToString(),
                            VillageName = reader["VillageName"]?.ToString(),

                            ProfilePhoto = reader["ProfilePhoto"]?.ToString(),

                            // Bank
                            BankName = reader["BankName"]?.ToString(),
                            AccountNumber = reader["AccountNumber"]?.ToString(),
                            IFSCCode = reader["IFSCCode"]?.ToString()
                        };
                    }
                }
            }

            return model;
        }


        //Famer login 

        public FarmerViewModel FarmerLogin(string farmerCode, string password)
        {
            using var con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand("Farmer.usp_Farmer_Login", con);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@FarmerCode", farmerCode.Trim());
            cmd.Parameters.AddWithValue("@Password", password.Trim());

            con.Open();

            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                return new FarmerViewModel
                {
                    FarmerId = Convert.ToInt32(reader["FarmerId"]),
                    FarmerName = reader["FarmerName"].ToString(),
                    FarmerCode = reader["FarmerCode"].ToString(),
                    Phone = reader["Phone"].ToString()
                };
            }

            return null;
        }
        public List<MilkCollectionViewModel> GetTodayMilkEntries(int farmerId)
        {
            var list = new List<MilkCollectionViewModel>();

            using var con = _dbHelper.GetConnection();
            // Direct query — includes ReceiptNumber which the generic history SP omits
            const string sql = @"
                SELECT
                    mc.CollectionId,
                    mc.CollectionDate,
                    mc.Shift,
                    cc.CenterName,
                    mt.MilkTypeName,
                    mc.Quantity,
                    mc.AppliedFat,
                    mc.AppliedCLR,
                    mc.RatePerLiter,
                    mc.Amount,
                    fr.ReceiptNumber
                FROM Collection.MilkCollection mc
                INNER JOIN Collection.CollectionCenters cc ON cc.CenterId   = mc.CenterId
                INNER JOIN Finance.MilkTypes            mt ON mt.MilkTypeId = mc.MilkTypeId
                LEFT  JOIN Collection.FarmerReceipts    fr ON fr.CollectionId = mc.CollectionId
                WHERE mc.FarmerId      = @FarmerId
                  AND mc.CollectionDate = CAST(GETDATE() AS DATE)
                ORDER BY
                    CASE mc.Shift WHEN 'Morning' THEN 1 WHEN 'Evening' THEN 2 ELSE 3 END";

            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@FarmerId", farmerId);
            con.Open();

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new MilkCollectionViewModel
                {
                    CollectionId = Convert.ToInt32(reader["CollectionId"]),
                    CollectionDate = Convert.ToDateTime(reader["CollectionDate"]),
                    Shift = reader["Shift"].ToString(),
                    CenterName = reader["CenterName"].ToString(),
                    MilkTypeName = reader["MilkTypeName"].ToString(),
                    Quantity = Convert.ToDecimal(reader["Quantity"]),

                    AppliedFat = reader["AppliedFat"] == DBNull.Value ? null : Convert.ToDecimal(reader["AppliedFat"]),
                    AppliedCLR = reader["AppliedCLR"] == DBNull.Value ? null : Convert.ToDecimal(reader["AppliedCLR"]),

                    RatePerLiter = Convert.ToDecimal(reader["RatePerLiter"]),
                    Amount = Convert.ToDecimal(reader["Amount"]),

                    ReceiptNumber = reader["ReceiptNumber"] == DBNull.Value ? null : reader["ReceiptNumber"].ToString()
                });
            }

            return list;
        }

        //all milk entries
        public List<MilkCollectionViewModel> GetAllMilkEntries(int farmerId)
        {
            var list = new List<MilkCollectionViewModel>();

            using var con = _dbHelper.GetConnection();

            const string sql = @"
        SELECT
            mc.CollectionId,
            mc.CollectionDate,
            mc.Shift,
            cc.CenterName,
            mt.MilkTypeName,
            mc.Quantity,
            mc.AppliedFat,
            mc.AppliedCLR,
            mc.RatePerLiter,
            mc.Amount,
            fr.ReceiptNumber
        FROM Collection.MilkCollection mc
        INNER JOIN Collection.CollectionCenters cc ON cc.CenterId = mc.CenterId
        INNER JOIN Finance.MilkTypes mt ON mt.MilkTypeId = mc.MilkTypeId
        LEFT JOIN Collection.FarmerReceipts fr ON fr.CollectionId = mc.CollectionId
        WHERE mc.FarmerId = @FarmerId
        ORDER BY mc.CollectionDate DESC, mc.Shift";

            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@FarmerId", farmerId);

            con.Open();

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new MilkCollectionViewModel
                {
                    CollectionId = Convert.ToInt32(reader["CollectionId"]),
                    CollectionDate = Convert.ToDateTime(reader["CollectionDate"]),
                    Shift = reader["Shift"].ToString(),
                    CenterName = reader["CenterName"].ToString(),
                    MilkTypeName = reader["MilkTypeName"].ToString(),
                    Quantity = Convert.ToDecimal(reader["Quantity"]),

                    AppliedFat = reader["AppliedFat"] == DBNull.Value ? null : Convert.ToDecimal(reader["AppliedFat"]),
                    AppliedCLR = reader["AppliedCLR"] == DBNull.Value ? null : Convert.ToDecimal(reader["AppliedCLR"]),

                    RatePerLiter = Convert.ToDecimal(reader["RatePerLiter"]),
                    Amount = Convert.ToDecimal(reader["Amount"]),

                    ReceiptNumber = reader["ReceiptNumber"] == DBNull.Value ? null : reader["ReceiptNumber"].ToString()
                });
            }

            return list;
        }

        //profile farmer

        public FarmerProfileModel GetFarmerProfile(int farmerId)
        {
            using var con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand("Farmer.usp_GetFarmerProfile", con);

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@FarmerId", farmerId);

            con.Open();

            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                return new FarmerProfileModel
                {
                    FarmerId = Convert.ToInt32(reader["FarmerId"]),
                    FarmerName = reader["FarmerName"].ToString(),
                    FarmerCode = reader["FarmerCode"].ToString(),
                    Phone = reader["Phone"].ToString(),
                    IsActive = Convert.ToBoolean(reader["IsActive"]),
                    ProfilePhoto = reader["ProfilePhoto"]?.ToString(),

                    VillageName = reader["VillageName"]?.ToString(),
                    CityName = reader["CityName"]?.ToString(),
                    StateName = reader["StateName"]?.ToString(),

                    CenterName = reader["CenterName"]?.ToString(),

                    BankName = reader["BankName"]?.ToString(),
                    AccountNumber = reader["AccountNumber"]?.ToString(),
                    IFSCCode = reader["IFSCCode"]?.ToString()
                };
            }

            return null;
        }


        //farmer dashboard

        public FarmerDashboardViewModel GetDashboard(int farmerId)
        {
            var vm = new FarmerDashboardViewModel();

            using var con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand("Farmer.usp_Farmer_GetDashboard", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@FarmerId", farmerId);
            con.Open();

            using var reader = cmd.ExecuteReader();

            // RS1 — today
            if (reader.Read())
            {
                vm.TodayMorningQty = reader.GetDecimal(reader.GetOrdinal("TodayMorningQty"));
                vm.TodayMorningAmount = reader.GetDecimal(reader.GetOrdinal("TodayMorningAmount"));
                vm.TodayEveningQty = reader.GetDecimal(reader.GetOrdinal("TodayEveningQty"));
                vm.TodayEveningAmount = reader.GetDecimal(reader.GetOrdinal("TodayEveningAmount"));
                vm.TodayTotalQty = reader.GetDecimal(reader.GetOrdinal("TodayTotalQty"));
                vm.TodayTotalAmount = reader.GetDecimal(reader.GetOrdinal("TodayTotalAmount"));
            }

            // RS2 — month
            if (reader.NextResult() && reader.Read())
            {
                vm.MonthTotalQty = reader.GetDecimal(reader.GetOrdinal("MonthTotalQty"));
                vm.MonthTotalAmount = reader.GetDecimal(reader.GetOrdinal("MonthTotalAmount"));
            }

            // RS3 — pending
            if (reader.NextResult() && reader.Read())
                vm.PendingAmount = reader.GetDecimal(reader.GetOrdinal("PendingAmount"));

            // RS4 — last payment (may be empty row)
            if (reader.NextResult() && reader.Read())
            {
                vm.LastPaymentAmount = reader.GetDecimal(reader.GetOrdinal("LastPaymentAmount"));
                vm.LastPaymentDate = reader.GetDateTime(reader.GetOrdinal("LastPaymentDate"));
            }

            // RS5 — rejections
            if (reader.NextResult() && reader.Read())
            {
                vm.TotalRejectionsLast30Days = reader.GetInt32(reader.GetOrdinal("TotalRejectionsLast30Days"));
                vm.TotalRejectedQtyLast30Days = reader.GetDecimal(reader.GetOrdinal("TotalRejectedQtyLast30Days"));
            }

            return vm;
        }
        //public FarmerDashboardViewModel GetDashboard(int farmerId)
        //{
        //    var vm = new FarmerDashboardViewModel();

        //    using var con = _dbHelper.GetConnection();
        //    using var cmd = new SqlCommand("Farmer.usp_Farmer_GetDashboard", con);
        //    cmd.CommandType = CommandType.StoredProcedure;
        //    cmd.Parameters.AddWithValue("@FarmerId", farmerId);
        //    con.Open();

        //    using var reader = cmd.ExecuteReader();

        //    // RS1 — today's data
        //    if (reader.Read())
        //    {
        //        vm.TodayMorningQty = reader.GetDecimal(reader.GetOrdinal("TodayMorningQty"));
        //        vm.TodayMorningAmount = reader.GetDecimal(reader.GetOrdinal("TodayMorningAmount"));
        //        vm.TodayEveningQty = reader.GetDecimal(reader.GetOrdinal("TodayEveningQty"));
        //        vm.TodayEveningAmount = reader.GetDecimal(reader.GetOrdinal("TodayEveningAmount"));
        //        vm.TodayTotalQty = reader.GetDecimal(reader.GetOrdinal("TodayTotalQty"));
        //        vm.TodayTotalAmount = reader.GetDecimal(reader.GetOrdinal("TodayTotalAmount"));
        //    }

        //    // RS2 — month totals
        //    if (reader.NextResult() && reader.Read())
        //    {
        //        vm.MonthTotalQty = reader.GetDecimal(reader.GetOrdinal("MonthTotalQty"));
        //        vm.MonthTotalAmount = reader.GetDecimal(reader.GetOrdinal("MonthTotalAmount"));
        //    }

        //    // RS3 — pending amount
        //    if (reader.NextResult() && reader.Read())
        //        vm.PendingAmount = reader.GetDecimal(reader.GetOrdinal("PendingAmount"));

        //    // RS4 — last payment (may be empty)
        //    if (reader.NextResult() && reader.Read())
        //    {
        //        vm.LastPaymentAmount = reader.GetDecimal(reader.GetOrdinal("LastPaymentAmount"));
        //        vm.LastPaymentDate = reader.GetDateTime(reader.GetOrdinal("LastPaymentDate"));
        //    }

        //    return vm;
        //}


        //self registration 


        public void SelfRegisterFarmer(SelfRegisterViewModel model)
        {
            using var con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand("Farmer.usp_Farmer_SelfRegister", con);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@FarmerName", model.FarmerName.Trim());
            cmd.Parameters.AddWithValue("@VillageId", model.VillageId);
            cmd.Parameters.AddWithValue("@CenterId", model.CenterId);
            cmd.Parameters.AddWithValue("@Phone", model.Phone.Trim());
            cmd.Parameters.AddWithValue("@BankName", (object)model.BankName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@AccountNumber", (object)model.AccountNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IFSCCode", (object)model.IFSCCode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ProfilePhoto", DBNull.Value);

            con.Open();
            // SP raises an error on duplicates — SqlException will bubble up to controller
            cmd.ExecuteNonQuery();
        }

        public FarmerStatusViewModel GetFarmerStatusByPhone(string phone)
        {
            using var con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand("Farmer.usp_GetFarmerStatusByPhone", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Phone", phone.Trim());

            con.Open();
            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
                return null;

            return new FarmerStatusViewModel
            {
                FarmerId = Convert.ToInt32(reader["FarmerId"]),
                FarmerName = reader["FarmerName"].ToString(),
                FarmerCode = reader["FarmerCode"] == DBNull.Value ? null : reader["FarmerCode"].ToString(),
                Phone = phone,
                ApprovalStatus = reader["ApprovalStatus"].ToString(),
                ApprovalRemark = reader["ApprovalRemark"] == DBNull.Value ? null : reader["ApprovalRemark"].ToString(),
                CenterName = reader["CenterName"].ToString(),
                Searched = true
            };
        }

        public List<PendingApprovalViewModel> GetPendingApprovals(int staffId)
        {
            var list = new List<PendingApprovalViewModel>();

            using var con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand("Farmer.usp_Center_GetPendingApprovals", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@StaffId", staffId);

            con.Open();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new PendingApprovalViewModel
                {
                    FarmerId = Convert.ToInt32(reader["FarmerId"]),
                    FarmerName = reader["FarmerName"].ToString(),
                    Phone = reader["Phone"]?.ToString(),
                    ProfilePhoto = reader["ProfilePhoto"] == DBNull.Value ? null : reader["ProfilePhoto"].ToString(),
                    VillageName = reader["VillageName"]?.ToString(),
                    CityName = reader["CityName"]?.ToString(),
                    StateName = reader["StateName"]?.ToString(),
                    CenterName = reader["CenterName"]?.ToString(),
                    BankName = reader["BankName"] == DBNull.Value ? null : reader["BankName"].ToString(),
                    AccountNumber = reader["AccountNumber"] == DBNull.Value ? null : reader["AccountNumber"].ToString(),
                    IFSCCode = reader["IFSCCode"] == DBNull.Value ? null : reader["IFSCCode"].ToString(),
                    ApprovalRemark = reader["ApprovalRemark"] == DBNull.Value ? null : reader["ApprovalRemark"].ToString()
                });
            }

            return list;
        }

        public ApprovalResultModel ApproveFarmer(int staffId, int farmerId)
        {
            using var con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand("Farmer.usp_Center_ApproveFarmer", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@StaffId", staffId);
            cmd.Parameters.AddWithValue("@FarmerId", farmerId);

            con.Open();
            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
                throw new Exception("Approval failed. Please try again.");

            return new ApprovalResultModel
            {
                FarmerId = Convert.ToInt32(reader["FarmerId"]),
                FarmerCode = reader["FarmerCode"].ToString(),
                DefaultPassword = reader["DefaultPassword"].ToString()
            };
        }

        public void RejectFarmer(int staffId, int farmerId, string remark)
        {
            using var con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand("Farmer.usp_Center_RejectFarmer", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@StaffId", staffId);
            cmd.Parameters.AddWithValue("@FarmerId", farmerId);
            cmd.Parameters.AddWithValue("@ApprovalRemark", (object)remark ?? DBNull.Value);

            con.Open();
            cmd.ExecuteNonQuery();
        }


        //milk rejection entries (history) for farmer
        public List<FarmerRejectionViewModel> GetRejectionHistory(
        int farmerId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var list = new List<FarmerRejectionViewModel>();

            using var con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand("Farmer.usp_Farmer_GetRejections", con);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@FarmerId", farmerId);
            cmd.Parameters.AddWithValue("@FromDate", (object)fromDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ToDate", (object)toDate ?? DBNull.Value);

            con.Open();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new FarmerRejectionViewModel
                {
                    RejectionId = Convert.ToInt32(reader["RejectionId"]),
                    RejectionDate = Convert.ToDateTime(reader["RejectionDate"]),
                    Shift = reader["Shift"]?.ToString(),
                    ShiftWindow = reader["ShiftWindow"]?.ToString(),
                    MilkTypeName = reader["MilkTypeName"]?.ToString(),
                    Quantity = Convert.ToDecimal(reader["Quantity"]),
                    AppliedFat = reader["AppliedFat"] == DBNull.Value ? null
                                      : Convert.ToDecimal(reader["AppliedFat"]),
                    AppliedCLR = reader["AppliedCLR"] == DBNull.Value ? null
                                      : Convert.ToDecimal(reader["AppliedCLR"]),
                    RejectionReason = reader["RejectionReason"]?.ToString(),
                    Remarks = reader["Remarks"]?.ToString(),
                    CenterName = reader["CenterName"]?.ToString(),
                    RecordedByStaff = reader["RecordedByStaff"]?.ToString()
                });
            }

            return list;
        }

    }
}