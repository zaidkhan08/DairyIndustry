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


        // GET STATES

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

        // GET CITIES BY STATE

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

        // GET VILLAGES BY CITY

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

        public async Task<RegByCenterModel> AddFarmerAsync(RegByCenterModel model, int staffId)
        {
           
            using (SqlConnection con = _dbHelper.GetConnection())
            using (SqlCommand cmd = new SqlCommand("Farmer.usp_Center_RegisterFarmer", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@StaffId", staffId);
                cmd.Parameters.AddWithValue("@FarmerName", model.FarmerName);
                cmd.Parameters.AddWithValue("@Gender", model.Gender);
                cmd.Parameters.AddWithValue("@DateOfBirth", (object?)model.DateOfBirth ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Email", (object?)model.Email ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@VillageId", model.VillageId);
                cmd.Parameters.AddWithValue("@Phone", (object?)model.Phone ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@BankName", (object?)model.BankName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@AccountNumber", (object?)model.AccountNumber ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IFSCCode", (object?)model.IFSCCode ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@AadhaarNumber", model.AadhaarNumber);
                cmd.Parameters.AddWithValue("@ProfilePhoto",
                    string.IsNullOrEmpty(model.ProfilePhoto) ? (object)DBNull.Value : model.ProfilePhoto);
                cmd.Parameters.AddWithValue("@AadhaarCardPath",
                    string.IsNullOrEmpty(model.AadhaarCardPath) ? (object)DBNull.Value : model.AadhaarCardPath);
                cmd.Parameters.AddWithValue("@PassbookPath",
                    string.IsNullOrEmpty(model.PassbookPath) ? (object)DBNull.Value : model.PassbookPath);

                await con.OpenAsync();
                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        model.FarmerId = Convert.ToInt32(reader["NewFarmerId"]);
                        model.FarmerCode = reader["FarmerCode"].ToString();
                        if (!string.IsNullOrEmpty(model.Phone) && model.Phone.Length >= 4)
                            model.DefaultPassword = model.Phone.Substring(model.Phone.Length - 4);
                    }
                }
            }
            return model;
        }
        public async Task<bool> IsEmailAlreadyRegisteredAsync(string email)
        {
            using var con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand(@"
                SELECT COUNT(1) FROM Farmer.Farmers
                WHERE Email = @Email
                AND ApprovalStatus IN ('Pending', 'Approved')", con);
            cmd.Parameters.AddWithValue("@Email", email.Trim());
            await con.OpenAsync();
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }
        public async Task<string> GenerateOtpAsync(string email)
        {
            using var con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand("Farmer.usp_SendEmailOTP", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Email", email);
            await con.OpenAsync();
            var otp = (await cmd.ExecuteScalarAsync())?.ToString();
            return otp;
        }

        public async Task<bool> VerifyOtpAsync(string email, string otp)
        {
            using var con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand("Farmer.usp_VerifyEmailOTP", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue("@OTPCode", otp);
            await con.OpenAsync();
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result) == 1;
        }
        

        //  GET ALL FARMERS
        public List<FarmerViewModel> GetAllFarmers(int staffId)
        {
            var list = new List<FarmerViewModel>();

            using (SqlConnection con = _dbHelper.GetConnection())
            using (SqlCommand cmd = new SqlCommand("Farmer.usp_Center_GetAllFarmers", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@StaffId", staffId);
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
                            Gender = reader["Gender"].ToString(),
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


        public List<CenterRejectedFarmerModel> GetRejectedFarmersByCenter(int staffId)
        {
            var list = new List<CenterRejectedFarmerModel>();

            using var con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand("Farmer.usp_Center_GetRejectedFarmers", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@StaffId", staffId);

            con.Open();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new CenterRejectedFarmerModel
                {
                    FarmerId = Convert.ToInt32(reader["FarmerId"]),
                    FarmerName = reader["FarmerName"]?.ToString(),
                    Phone = reader["Phone"]?.ToString(),
                    Email = reader["Email"] == DBNull.Value ? null : reader["Email"].ToString(),
                    Gender = reader["Gender"] == DBNull.Value ? null : reader["Gender"].ToString(),
                    ProfilePhoto = reader["ProfilePhoto"] == DBNull.Value ? null : reader["ProfilePhoto"].ToString(),
                    ApprovalStatus = reader["ApprovalStatus"]?.ToString(),
                    ApprovalRemark = reader["ApprovalRemark"] == DBNull.Value ? null : reader["ApprovalRemark"].ToString(),
                    AadhaarNumber = reader["AadhaarNumber"] == DBNull.Value ? null : reader["AadhaarNumber"].ToString(),
                    VillageName = reader["VillageName"]?.ToString(),
                    CityName = reader["CityName"]?.ToString(),
                    StateName = reader["StateName"]?.ToString(),
                    CenterName = reader["CenterName"]?.ToString(),
                    BankName = reader["BankName"] == DBNull.Value ? null : reader["BankName"].ToString(),
                    AccountNumber = reader["AccountNumber"] == DBNull.Value ? null : reader["AccountNumber"].ToString(),
                    IFSCCode = reader["IFSCCode"] == DBNull.Value ? null : reader["IFSCCode"].ToString(),
                    CreatedDate = reader["CreatedDate"] == DBNull.Value? DateTime.MinValue: Convert.ToDateTime(reader["CreatedDate"]),
                    DateOfBirth = reader["DateOfBirth"] == DBNull.Value? null: Convert.ToDateTime(reader["DateOfBirth"]),
                    AadhaarDocumentPath = reader["AadhaarDocumentPath"] == DBNull.Value? null: reader["AadhaarDocumentPath"].ToString(),
                    BankPassbookPath = reader["BankPassbookPath"] == DBNull.Value? null: reader["BankPassbookPath"].ToString(),
                });
            }

            return list;
        }

        // =========================
        // GET FARMER BY ID
        // =========================


        public async Task<FarmerEditModel> GetFarmerByIdAsync(int farmerId, int staffId)
        {
            FarmerEditModel model = null;
            using (SqlConnection con = _dbHelper.GetConnection())
            using (SqlCommand cmd = new SqlCommand("Farmer.usp_Center_GetFarmerById", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@FarmerId", SqlDbType.Int).Value = farmerId;
                cmd.Parameters.Add("@StaffId", SqlDbType.Int).Value = staffId;

                await con.OpenAsync();
                using (SqlDataReader r = await cmd.ExecuteReaderAsync())
                {
                    if (await r.ReadAsync())
                    {
                        model = new FarmerEditModel
                        {
                            FarmerId = Convert.ToInt32(r["FarmerId"]),
                            FarmerName = r["FarmerName"] == DBNull.Value ? null : r["FarmerName"].ToString(),
                            FarmerCode = r["FarmerCode"] == DBNull.Value ? null : r["FarmerCode"].ToString(),
                            Phone = r["Phone"] == DBNull.Value ? null : r["Phone"].ToString(),
                            Email = r["Email"] == DBNull.Value ? null : r["Email"].ToString(),
                            Gender = r["Gender"] == DBNull.Value ? null : r["Gender"].ToString(),
                            DateOfBirth = r["DateOfBirth"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["DateOfBirth"]),
                            StateId = r["StateId"] == DBNull.Value ? 0 : Convert.ToInt32(r["StateId"]),
                            CityId = r["CityId"] == DBNull.Value ? 0 : Convert.ToInt32(r["CityId"]),
                            VillageId = r["VillageId"] == DBNull.Value ? 0 : Convert.ToInt32(r["VillageId"]),
                            StateName = r["StateName"] == DBNull.Value ? null : r["StateName"].ToString(),
                            CityName = r["CityName"] == DBNull.Value ? null : r["CityName"].ToString(),
                            VillageName = r["VillageName"] == DBNull.Value ? null : r["VillageName"].ToString(),
                            BankName = r["BankName"] == DBNull.Value ? null : r["BankName"].ToString(),
                            AccountNumber = r["AccountNumber"] == DBNull.Value ? null : r["AccountNumber"].ToString(),
                            IFSCCode = r["IFSCCode"] == DBNull.Value ? null : r["IFSCCode"].ToString(),
                            AadhaarNumber = r["AadhaarNumber"] == DBNull.Value ? null : r["AadhaarNumber"].ToString(),
                            ProfilePhoto = r["ProfilePhoto"] == DBNull.Value ? null : r["ProfilePhoto"].ToString(),
                            AadhaarCardPath = r["AadhaarCardPath"] == DBNull.Value ? null : r["AadhaarCardPath"].ToString(),
                            PassbookPath = r["PassbookPath"] == DBNull.Value ? null : r["PassbookPath"].ToString(),
                            IsActive = r["IsActive"] != DBNull.Value && Convert.ToBoolean(r["IsActive"]),
                            ApprovalStatus = r["ApprovalStatus"] == DBNull.Value ? null : r["ApprovalStatus"].ToString()
                        };
                    }
                }
            }
            return model;
        }
        

        // =========================
        // UPDATE FARMER (NO VALIDATION)
        // =========================
        public async Task<int> UpdateFarmerAsync(FarmerEditModel model, int staffId)
        {
            using (SqlConnection con = _dbHelper.GetConnection())
            using (SqlCommand cmd = new SqlCommand("Farmer.usp_Center_UpdateFarmer", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@StaffId", SqlDbType.Int).Value = staffId;
                cmd.Parameters.Add("@FarmerId", SqlDbType.Int).Value = model.FarmerId;
                cmd.Parameters.Add("@FarmerName", SqlDbType.VarChar).Value = (object?)model.FarmerName ?? DBNull.Value;
                cmd.Parameters.Add("@VillageId", SqlDbType.Int).Value = model.VillageId;
                cmd.Parameters.Add("@Phone", SqlDbType.VarChar).Value = (object?)model.Phone ?? DBNull.Value;
                cmd.Parameters.Add("@Email", SqlDbType.VarChar).Value = (object?)model.Email ?? DBNull.Value;
                cmd.Parameters.Add("@Gender", SqlDbType.VarChar).Value = (object?)model.Gender ?? DBNull.Value;
                cmd.Parameters.Add("@DateOfBirth", SqlDbType.Date).Value = (object?)model.DateOfBirth ?? DBNull.Value;
                cmd.Parameters.Add("@BankName", SqlDbType.VarChar).Value = (object?)model.BankName ?? DBNull.Value;
                cmd.Parameters.Add("@AccountNumber", SqlDbType.VarChar).Value = (object?)model.AccountNumber ?? DBNull.Value;
                cmd.Parameters.Add("@IFSCCode", SqlDbType.VarChar).Value = (object?)model.IFSCCode ?? DBNull.Value;
                cmd.Parameters.Add("@ProfilePhoto", SqlDbType.VarChar).Value = (object?)model.ProfilePhoto ?? DBNull.Value;

                await con.OpenAsync();
                object result = await cmd.ExecuteScalarAsync();
                return result != null ? Convert.ToInt32(result) : 0;
            }
        }

        // =========================
        // DOCUMENT UPDATE
        // =========================
        public async Task<int> UpdateFarmerDocumentAsync(int staffId, int farmerId, string documentType, string filePath)
        {
            using (SqlConnection con = _dbHelper.GetConnection())
            using (SqlCommand cmd = new SqlCommand("Farmer.usp_UpdateFarmerDocument", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@StaffId", SqlDbType.Int).Value = staffId;
                cmd.Parameters.Add("@FarmerId", SqlDbType.Int).Value = farmerId;
                cmd.Parameters.Add("@DocumentType", SqlDbType.VarChar).Value = documentType;
                cmd.Parameters.Add("@FilePath", SqlDbType.VarChar).Value = filePath;

                await con.OpenAsync();
                object result = await cmd.ExecuteScalarAsync();
                return result != null ? Convert.ToInt32(result) : 0;
            }
        }


        //Famer login 


        public List<MilkCollectionModel> GetTodayMilkEntries(int farmerId)
        {
            var list = new List<MilkCollectionModel>();

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
                list.Add(new MilkCollectionModel
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
        public List<AllMilkHistoryModel> GetAllMilkEntriesFarmer(int farmerId)
        {
            var list = new List<AllMilkHistoryModel>();

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
                list.Add(new AllMilkHistoryModel
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

        public FarmerMilkReceiptModel GetReceiptByCollectionId(int collectionId)
        {
            FarmerMilkReceiptModel model = null;

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
                    fr.ReceiptId,
                    fr.ReceiptNumber,
                    fr.ReceiptDate,
                    f.FarmerId,
                    f.FarmerName,
                    f.FarmerCode
                FROM Collection.MilkCollection mc
                INNER JOIN Collection.CollectionCenters cc ON cc.CenterId = mc.CenterId
                INNER JOIN Finance.MilkTypes mt ON mt.MilkTypeId = mc.MilkTypeId
                INNER JOIN Farmer.Farmers f ON f.FarmerId = mc.FarmerId
                LEFT JOIN Collection.FarmerReceipts fr ON fr.CollectionId = mc.CollectionId
                WHERE mc.CollectionId = @CollectionId";

            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@CollectionId", collectionId);

            con.Open();

            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                model = new FarmerMilkReceiptModel
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

                    ReceiptId = reader["ReceiptId"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ReceiptId"]),
                    ReceiptNumber = reader["ReceiptNumber"]?.ToString(),
                    ReceiptDate = reader["ReceiptDate"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(reader["ReceiptDate"]),

                    FarmerId = Convert.ToInt32(reader["FarmerId"]),
                    FarmerName = reader["FarmerName"].ToString(),
                    FarmerCode = reader["FarmerCode"].ToString()
                };
            }

            return model;
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
                    IFSCCode = reader["IFSCCode"]?.ToString(),
                    AadhaarNumber = reader["AadhaarNumber"]?.ToString(),
                    Email = reader["Email"]?.ToString(),
                    Gender = reader["Gender"].ToString(),
                    DateOfBirth= Convert.ToDateTime(reader["DateOfBirth"]),
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
        public async Task<bool> IsPhoneAlreadyRegisteredAsync(string phone)
        {
            using var con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand(
                "SELECT COUNT(1) FROM Farmer.Farmers WHERE Phone = @Phone AND ApprovalStatus IN ('Pending','Approved')", con);
            cmd.Parameters.AddWithValue("@Phone", phone.Trim());
            await con.OpenAsync();
            return (int)await cmd.ExecuteScalarAsync() > 0;
        }

        public async Task<bool> IsAadhaarAlreadyRegisteredAsync(string aadhaar)
        {
            using var con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand(
                "SELECT COUNT(1) FROM Farmer.Farmers WHERE AadhaarNumber = @Aadhaar AND ApprovalStatus IN ('Pending','Approved')", con);
            cmd.Parameters.AddWithValue("@Aadhaar", aadhaar.Trim());
            await con.OpenAsync();
            return (int)await cmd.ExecuteScalarAsync() > 0;
        }
        public async Task SelfRegisterFarmerAsync(SelfRegisterViewModel model)
        {
            using var con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand("Farmer.usp_Farmer_SelfRegister", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@FarmerName", model.FarmerName?.Trim());
            cmd.Parameters.AddWithValue("@VillageId", model.VillageId);
            cmd.Parameters.AddWithValue("@CenterId", model.CenterId);
            cmd.Parameters.AddWithValue("@Phone", model.Phone?.Trim());
            cmd.Parameters.AddWithValue("@Email", model.Email ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Gender", model.Gender ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@DateOfBirth", model.DateOfBirth ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@AadhaarNumber", model.AadhaarNumber ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ProfilePhoto", model.ProfilePhotoPath ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@AadhaarDocPath", model.AadhaarDocPath ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@BankDocPath", model.PassbookDocPath ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@BankName", model.BankName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@AccountNumber", model.AccountNumber ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@IFSCCode", model.IFSCCode ?? (object)DBNull.Value);

            await con.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
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


     
        //Pending Approvals
        public List<PendingApprovalModel> GetPendingApprovals(int staffId)
        {
            var list = new List<PendingApprovalModel>();

            using var con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand("Farmer.usp_Center_GetPendingApprovals", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@StaffId", staffId);

            con.Open();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new PendingApprovalModel
                {

                    FarmerId = Convert.ToInt32(reader["FarmerId"]),
                    FarmerName = reader["FarmerName"].ToString(),
                    Phone = reader["Phone"]?.ToString(),
                    Email = reader["Email"] == DBNull.Value ? null : reader["Email"].ToString(),
                    Gender = reader["Gender"] == DBNull.Value ? null : reader["Gender"].ToString(),
                    DateOfBirth = reader["DateOfBirth"] == DBNull.Value ? null : Convert.ToDateTime(reader["DateOfBirth"]),
                    AadhaarNumber = reader["AadhaarNumber"] == DBNull.Value ? null : reader["AadhaarNumber"].ToString(),
                    ProfilePhoto = reader["ProfilePhoto"] == DBNull.Value ? null : reader["ProfilePhoto"].ToString(),
                    CreatedDate = reader["CreatedDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["CreatedDate"]),

                    ApprovalStatus = reader["ApprovalStatus"]?.ToString(),
                    ApprovalRemark = reader["ApprovalRemark"] == DBNull.Value ? null : reader["ApprovalRemark"].ToString(),

                    VillageName = reader["VillageName"]?.ToString(),
                    CityName = reader["CityName"]?.ToString(),
                    StateName = reader["StateName"]?.ToString(),
                    CenterName = reader["CenterName"]?.ToString(),

                    BankName = reader["BankName"] == DBNull.Value ? null : reader["BankName"].ToString(),
                    AccountNumber = reader["AccountNumber"] == DBNull.Value ? null : reader["AccountNumber"].ToString(),
                    IFSCCode = reader["IFSCCode"] == DBNull.Value ? null : reader["IFSCCode"].ToString(),

                    AadhaarCardPath = reader["AadhaarCardPath"] == DBNull.Value ? null : reader["AadhaarCardPath"].ToString(),
                    PassbookPath = reader["PassbookPath"] == DBNull.Value ? null : reader["PassbookPath"].ToString(),
                });
            }

            return list;
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

        // Approve Farmer - ApprovalResultModel.Email
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
                DefaultPassword = reader["DefaultPassword"].ToString(),
                Email = reader["Email"] == DBNull.Value? null: reader["Email"].ToString()
            };
        }

        //  FarmerLogin
        public FarmerViewModel FarmerLogin(string farmerCode, string password)
        {
            using var con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand("Farmer.usp_Farmer_Login", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@FarmerCode", farmerCode.Trim());
            cmd.Parameters.AddWithValue("@Password", password.Trim());

            con.Open();
            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
                return null;    // invalid credentials

            return new FarmerViewModel
            {
                FarmerId = Convert.ToInt32(reader["FarmerId"]),
                FarmerName = reader["FarmerName"].ToString(),
                FarmerCode = reader["FarmerCode"].ToString(),
                Phone = reader["Phone"].ToString(),
                // Store IsFirstLogin so Login POST can check it
                IsFirstLogin = Convert.ToInt32(reader["IsFirstLogin"]) == 1
            };
        }

        // ChangePassword  
        public string ChangePassword(int farmerId, string currentPassword, string newPassword)
        {
            using var con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand("Farmer.usp_Farmer_ChangePassword", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@FarmerId", farmerId);
            cmd.Parameters.AddWithValue("@CurrentPassword", currentPassword);
            cmd.Parameters.AddWithValue("@NewPassword", newPassword);

            con.Open();
            return cmd.ExecuteScalar()?.ToString() ?? "InvalidPassword";
        }

 


        // ─────────────────────────────────────────────────────────────
        // Add these implementations to your FarmerRepository
        // ─────────────────────────────────────────────────────────────

        public (int FarmerId, string Email, string FarmerName)? GetFarmerEmailByCode(string farmerCode)
        {
            using var con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand("Farmer.usp_Farmer_ForgotPassword_GetEmail", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@FarmerCode", farmerCode.Trim());
            con.Open();
            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return (
                FarmerId: Convert.ToInt32(reader["FarmerId"]),
                Email: reader["Email"]?.ToString() ?? string.Empty,
                FarmerName: reader["FarmerName"].ToString()
            );
        }

        public bool ResetFarmerPassword(string farmerCode, string newPassword)
        {
            using var con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand("Farmer.usp_Farmer_ResetPassword", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@FarmerCode", farmerCode.Trim());
            cmd.Parameters.AddWithValue("@NewPassword", newPassword.Trim());
            con.Open();
            var affected = Convert.ToInt32(cmd.ExecuteScalar());
            return affected > 0;
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
                    AppliedFat = reader["AppliedFat"] == DBNull.Value ? null: Convert.ToDecimal(reader["AppliedFat"]),
                    AppliedCLR = reader["AppliedCLR"] == DBNull.Value ? null: Convert.ToDecimal(reader["AppliedCLR"]),
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