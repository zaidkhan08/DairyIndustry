using DairyIndustry.Data;
using DairyIndustry.Models.Admin;
using DairyIndustry.Models.Collection;
using DairyIndustry.Models.FarmerModel;
using DairyIndustry.Repositories;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DairyIndustry.Repository
{
    public class CollectionCenterRepository : ICollectionCenterRepository
    {
        private readonly DbHelper _dbHelper;

        public CollectionCenterRepository(DbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public string GetCurrentShift()
        {
            var now = DateTime.Now.TimeOfDay;

            // Morning: 9:55 AM to 10:21 AM
            if (now >= new TimeSpan(9, 55, 0) && now < new TimeSpan(13, 0
                , 0))
                return "Morning";

            // Evening: 4:00 PM to 7:00 PM
            if (now >= new TimeSpan(16, 0, 0) && now < new TimeSpan(19, 0, 0))
                return "Evening";

            return "No Active Shift";
        }
        public StaffDashboardViewModel GetStaffDashboard(int staffId)
        {
            var model = new StaffDashboardViewModel();

            using var con = _dbHelper.GetConnection();
            con.Open();

            // =========================
            // 1️⃣ STAFF INFO
            // =========================
            using (var cmd = new SqlCommand("Collection.usp_Staff_Info", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@StaffId", staffId);

                using var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    model.StaffId = Convert.ToInt32(reader["StaffId"]);
                    model.StaffName = reader["StaffName"]?.ToString();
                    model.StaffType = reader["StaffType"]?.ToString();
                    model.StaffPhoto = reader["StaffPhoto"]?.ToString();

                    model.CenterId = Convert.ToInt32(reader["CenterId"]);
                    model.CenterName = reader["CenterName"]?.ToString();
                    model.VillageName = reader["VillageName"]?.ToString();
                    model.CityName = reader["CityName"]?.ToString();
                    model.StateName = reader["StateName"]?.ToString();

                    //  NEW
                    model.Capacity = reader["Capacity"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Capacity"]);
                    model.CurrentStock = reader["CurrentStock"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["CurrentStock"]);
                    model.AvailableSpace = reader["AvailableSpace"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["AvailableSpace"]);
                }
            }

            // =========================
            // 2️⃣ SHIFTS
            // =========================
            using (var cmd = new SqlCommand("Collection.usp_Staff_Shifts", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@StaffId", staffId);

                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    model.Shifts.Add(new ShiftBatchInfo
                    {
                        Shift = reader["Shift"]?.ToString(),
                        ShiftWindow = reader["ShiftWindow"]?.ToString(),
                        BatchId = reader["BatchId"] == DBNull.Value ? null : Convert.ToInt32(reader["BatchId"]),
                        BatchStatus = reader["BatchStatus"]?.ToString(),
                        TotalQuantity = reader["TotalQuantity"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["TotalQuantity"]),
                        AvgFat = reader["AvgFat"] == DBNull.Value ? null : Convert.ToDecimal(reader["AvgFat"]),
                        AvgCLR = reader["AvgCLR"] == DBNull.Value ? null : Convert.ToDecimal(reader["AvgCLR"]),
                        EntryCount = reader["EntryCount"] == DBNull.Value ? 0 : Convert.ToInt32(reader["EntryCount"]),
                        IsCurrentShift = reader["IsCurrentShift"] != DBNull.Value && Convert.ToInt32(reader["IsCurrentShift"]) == 1
                    });
                }
            }

            // =========================
            // 3️⃣ SUMMARY
            // =========================
            using (var cmd = new SqlCommand("Collection.usp_Staff_Summary", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@StaffId", staffId);

                using var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    model.TotalMilkToday = reader["TotalMilkToday"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["TotalMilkToday"]);
                    model.TotalAmountToday = reader["TotalAmountToday"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["TotalAmountToday"]);
                    model.TotalEntriesToday = reader["TotalEntriesToday"] == DBNull.Value ? 0 : Convert.ToInt32(reader["TotalEntriesToday"]);
                    model.ActiveFarmerCount = reader["ActiveFarmerCount"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ActiveFarmerCount"]);
                    model.PendingPaymentAmount = reader["PendingPaymentAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["PendingPaymentAmount"]);
                }
            }

            return model;
        }

        public int AddMilkCollection(int staffId, int farmerId, int milkTypeId,
                             decimal quantity, decimal appliedFat, decimal appliedCLR)
        {
            using var con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand("Collection.usp_AddMilkEntry", con);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@StaffId", staffId);
            cmd.Parameters.AddWithValue("@FarmerId", farmerId);
            cmd.Parameters.AddWithValue("@MilkTypeId", milkTypeId);
            cmd.Parameters.AddWithValue("@Quantity", quantity);
            cmd.Parameters.AddWithValue("@AppliedFat", appliedFat);
            cmd.Parameters.AddWithValue("@AppliedCLR", appliedCLR);

            try
            {
                con.Open();

               
                cmd.ExecuteNonQuery();

                return 1;
            }
            catch (SqlException ex)
            {
              
                throw new Exception(ex.Message);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // GET TODAY'S ENTRIES — Collection.usp_GetTodayEntries
        // FIX: Old code used wrong SP "SP_07_MilkEntry_GetToday",
        //      passed @CenterId (SP wants @StaffId), and only mapped
        //      5 fields. SP returns 2 result sets — we read set 1 (entries).
        // ─────────────────────────────────────────────────────────────
        public List<MilkCollectionViewModel> GetTodayMilkEntries(int staffId, string shift = null)
        {
            var list = new List<MilkCollectionViewModel>();

            using var con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand("Collection.usp_GetTodayEntries", con);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@StaffId", staffId);
            cmd.Parameters.AddWithValue("@Shift", (object)shift ?? DBNull.Value);

            con.Open();
            using var reader = cmd.ExecuteReader();

            // Result set 1 — individual entries
            while (reader.Read())
            {
                list.Add(new MilkCollectionViewModel
                {
                    CollectionId = Convert.ToInt32(reader["CollectionId"]),
                    CollectionDate = Convert.ToDateTime(reader["CollectionDate"]),
                    Shift = reader["Shift"]?.ToString(),
                    ShiftWindow = reader["ShiftWindow"]?.ToString(),
                    FarmerId = Convert.ToInt32(reader["FarmerId"]),
                    FarmerCode = reader["FarmerCode"]?.ToString(),
                    FarmerName = reader["FarmerName"]?.ToString(),
                    MilkTypeName = reader["MilkTypeName"]?.ToString(),
                    Quantity = Convert.ToDecimal(reader["Quantity"]),
                    AppliedFat = reader["AppliedFat"] == DBNull.Value ? null : Convert.ToDecimal(reader["AppliedFat"]),
                    AppliedCLR = reader["AppliedCLR"] == DBNull.Value ? null : Convert.ToDecimal(reader["AppliedCLR"]),
                    RatePerLiter = reader["RatePerLiter"] == DBNull.Value ? null : Convert.ToDecimal(reader["RatePerLiter"]),
                    Amount = reader["Amount"] == DBNull.Value ? null : Convert.ToDecimal(reader["Amount"]),
                    ReceiptNumber = reader["ReceiptNumber"]?.ToString()
                });
            }

            return list;
        }

        public int RejectMilkEntry(MilkRejectionViewModel model, int staffId)
        {
            using var con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand("Collection.usp_RejectMilkEntry", con);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@StaffId", staffId);
            cmd.Parameters.AddWithValue("@FarmerId", model.FarmerId);
            cmd.Parameters.AddWithValue("@MilkTypeId", model.MilkTypeId);

            cmd.Parameters.AddWithValue("@AppliedFat",
                model.AppliedFat.HasValue
                ? model.AppliedFat.Value
                : DBNull.Value);

            cmd.Parameters.AddWithValue("@AppliedCLR",
                model.AppliedCLR.HasValue
                ? model.AppliedCLR.Value
                : DBNull.Value);

            cmd.Parameters.AddWithValue("@Quantity", model.Quantity);

            // FIX: string directly — no .ToString() needed
            cmd.Parameters.AddWithValue("@RejectionReason", model.RejectionReason);

            cmd.Parameters.AddWithValue("@Remarks",
                string.IsNullOrWhiteSpace(model.Remarks)
                ? DBNull.Value
                : model.Remarks);

            try
            {
                con.Open();
                using var reader = cmd.ExecuteReader();

                if (reader.Read())
                    return Convert.ToInt32(reader["RejectionId"]);

                return 0;
            }
            catch (SqlException ex)
            {
                throw new Exception("Error while rejecting milk: " + ex.Message);
            }
        }


        public List<MilkRejectionViewModel> GetRejectionsByCenter(int centerId)
        {
            var list = new List<MilkRejectionViewModel>();

            using var con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand(@"
            SELECT
                mr.RejectionId,
                mr.BatchId,
                mr.RejectionDate,
                mr.Shift,
                mr.AppliedFat,
                mr.AppliedCLR,
                mr.Quantity,
                mr.RejectionReason,
                mr.Remarks,
                f.FarmerName,
                f.FarmerCode,
                mt.MilkTypeName,
                cc.CenterName
            FROM Collection.MilkRejections mr
            INNER JOIN Farmer.Farmers f  ON f.FarmerId    = mr.FarmerId
            INNER JOIN Finance.MilkTypes mt ON mt.MilkTypeId = mr.MilkTypeId
            INNER JOIN Collection.CollectionCenters cc ON cc.CenterId = mr.CenterId
            WHERE mr.CenterId = @CenterId
            ORDER BY mr.RejectionDate DESC, mr.RejectionId DESC
            ", con);

            cmd.Parameters.AddWithValue("@CenterId", centerId);

            con.Open();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new MilkRejectionViewModel
                {
                    RejectionId = Convert.ToInt32(reader["RejectionId"]),
                    BatchId = Convert.ToInt32(reader["BatchId"]),
                    RejectionDate = Convert.ToDateTime(reader["RejectionDate"]),
                    Shift = reader["Shift"]?.ToString(),

                    AppliedFat = reader["AppliedFat"] == DBNull.Value
                        ? (decimal?)null
                        : Convert.ToDecimal(reader["AppliedFat"]),

                    AppliedCLR = reader["AppliedCLR"] == DBNull.Value
                        ? (decimal?)null
                        : Convert.ToDecimal(reader["AppliedCLR"]),

                    Quantity = Convert.ToDecimal(reader["Quantity"]),
                    RejectionReason = reader["RejectionReason"]?.ToString(),
                    Remarks = reader["Remarks"]?.ToString(),
                    FarmerName = reader["FarmerName"]?.ToString(),
                    FarmerCode = reader["FarmerCode"]?.ToString(),
                    MilkTypeName = reader["MilkTypeName"]?.ToString(),
                    CenterName = reader["CenterName"]?.ToString()
                });
            }

            return list;
        }

        public List<MilkRejectionViewModel> GetRejectionsByFarmer(int farmerId)
        {
            var list = new List<MilkRejectionViewModel>();

            using var con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand(@"
        SELECT
            mr.RejectionId,
            mr.RejectionDate,
            mr.Shift,
            mr.AppliedFat,
            mr.AppliedCLR,
            mr.Quantity,
            mr.RejectionReason,
            mr.Remarks,
            f.FarmerName,
            f.FarmerCode,
            mt.MilkTypeName,
            cc.CenterName
        FROM Collection.MilkRejections mr
        INNER JOIN Farmer.Farmers               f  ON f.FarmerId    = mr.FarmerId
        INNER JOIN Finance.MilkTypes            mt ON mt.MilkTypeId = mr.MilkTypeId
        INNER JOIN Collection.CollectionCenters cc ON cc.CenterId   = mr.CenterId
        WHERE mr.FarmerId = @FarmerId
        ORDER BY mr.RejectionDate DESC
    ", con);

            cmd.Parameters.AddWithValue("@FarmerId", farmerId);

            con.Open();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new MilkRejectionViewModel
                {
                    RejectionId = Convert.ToInt32(reader["RejectionId"]),
                    RejectionDate = Convert.ToDateTime(reader["RejectionDate"]),
                    Shift = reader["Shift"]?.ToString(),

                    AppliedFat = reader["AppliedFat"] == DBNull.Value
                        ? (decimal?)null
                        : Convert.ToDecimal(reader["AppliedFat"]),

                    AppliedCLR = reader["AppliedCLR"] == DBNull.Value
                        ? (decimal?)null
                        : Convert.ToDecimal(reader["AppliedCLR"]),

                    Quantity = Convert.ToDecimal(reader["Quantity"]),

                    //  FIX: string directly — no Enum.TryParse needed
                    // DB stores "Low Quality", "Adulterated" etc as is
                    RejectionReason = reader["RejectionReason"]?.ToString(),

                    Remarks = reader["Remarks"]?.ToString(),
                    FarmerName = reader["FarmerName"]?.ToString(),
                    FarmerCode = reader["FarmerCode"]?.ToString(),
                    MilkTypeName = reader["MilkTypeName"]?.ToString(),
                    CenterName = reader["CenterName"]?.ToString()
                });
            }

            return list;
        }
        // ─────────────────────────────────────────────────────────────
        // GET BATCH ENTRIES — Collection.usp_GetTodayEntries (with shift filter)
        // Used by ViewCollectionBatch page. Same SP as GetTodayMilkEntries
        // but always called with a specific shift.
        // ─────────────────────────────────────────────────────────────
        public List<MilkCollectionViewModel> GetBatchEntries(int staffId, string shift)
        {
            return GetTodayMilkEntries(staffId, shift);
        }

        // ─────────────────────────────────────────────────────────────
        // GET MILK ENTRY BY ID — Collection.usp_GetMilkEntryById
        // FIX: Old code used wrong SP "SP_08_MilkEntry_GetById",
        //      was missing @StaffId param (SP enforces center ownership),
        //      and only mapped 5 fields.
        // ─────────────────────────────────────────────────────────────
        public MilkCollectionViewModel GetMilkEntryById(int staffId, int collectionId)
        {
            using var con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand("Collection.usp_GetMilkEntryById", con);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@StaffId", staffId);
            cmd.Parameters.AddWithValue("@CollectionId", collectionId);

            con.Open();
            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
                return null;

            return new MilkCollectionViewModel
            {
                CollectionId = Convert.ToInt32(reader["CollectionId"]),
                CollectionDate = Convert.ToDateTime(reader["CollectionDate"]),
                Shift = reader["Shift"]?.ToString(),
                ShiftWindow = reader["ShiftWindow"]?.ToString(),
                FarmerId = Convert.ToInt32(reader["FarmerId"]),
                FarmerCode = reader["FarmerCode"]?.ToString(),
                FarmerName = reader["FarmerName"]?.ToString(),
                MilkTypeId = Convert.ToInt32(reader["MilkTypeId"]),
                MilkTypeName = reader["MilkTypeName"]?.ToString(),
                Quantity = Convert.ToDecimal(reader["Quantity"]),
                AppliedFat = reader["AppliedFat"] == DBNull.Value ? null : Convert.ToDecimal(reader["AppliedFat"]),
                AppliedCLR = reader["AppliedCLR"] == DBNull.Value ? null : Convert.ToDecimal(reader["AppliedCLR"]),
                RatePerLiter = reader["RatePerLiter"] == DBNull.Value ? null : Convert.ToDecimal(reader["RatePerLiter"]),
                Amount = reader["Amount"] == DBNull.Value ? null : Convert.ToDecimal(reader["Amount"]),
                ReceiptNumber = reader["ReceiptNumber"]?.ToString(),
                BatchId = Convert.ToInt32(reader["BatchId"]),
                CenterId = Convert.ToInt32(reader["CenterId"])
            };
        }

        // ─────────────────────────────────────────────────────────────
        // BATCH STATUS — Collection.usp_GetTodayBatchStatus
        // Always returns exactly 2 rows: Morning + Evening.
        // Status is auto-computed by SP from server time and batch table.
        // Staff cannot manually open/close — that is done by SQL Agent.
        // ─────────────────────────────────────────────────────────────
        public List<BatchStatusViewModel> GetTodayBatchStatus(int staffId)
        {
            var list = new List<BatchStatusViewModel>();

            using var conn = _dbHelper.GetConnection();
            using var cmd = new SqlCommand("Collection.usp_GetTodayBatchStatus", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@StaffId", staffId);

            conn.Open();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new BatchStatusViewModel
                {
                    BatchId = reader["BatchId"] == DBNull.Value ? null : Convert.ToInt32(reader["BatchId"]),
                    Shift = reader["Shift"]?.ToString(),
                    ShiftWindow = reader["ShiftWindow"]?.ToString(),
                    BatchStatus = reader["BatchStatus"]?.ToString(),
                    TotalQuantity = reader["TotalQuantity"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["TotalQuantity"]),
                    AvgFat = reader["AvgFat"] == DBNull.Value ? null : Convert.ToDecimal(reader["AvgFat"]),
                    AvgCLR = reader["AvgCLR"] == DBNull.Value ? null : Convert.ToDecimal(reader["AvgCLR"]),
                    EntryCount = reader["EntryCount"] == DBNull.Value ? 0 : Convert.ToInt32(reader["EntryCount"]),
                    IsCurrentShift = reader["IsCurrentShift"] != DBNull.Value && Convert.ToBoolean(reader["IsCurrentShift"]),
                    BatchDate = Convert.ToDateTime(reader["BatchDate"])
                });
            }

            return list;
        }
        public List<FarmerViewModel> GetFarmers(int centerId)
        {
            var farmers = new List<FarmerViewModel>();

            using (var conn = _dbHelper.GetConnection())
            using (var cmd = new SqlCommand("Collection.usp_GetFarmersByCenter", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@CenterId", centerId);

                conn.Open();
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    farmers.Add(new FarmerViewModel
                    {
                        FarmerId = Convert.ToInt32(reader["FarmerId"]),
                        FarmerCode = reader["FarmerCode"].ToString(),
                        FarmerName = reader["FarmerName"].ToString()
                    });
                }
            }

            return farmers;
        }

        public int GetCenterIdByStaffId(int staffId)
        {
            using var con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand(@"
            SELECT TOP 1 CenterId 
            FROM Collection.StaffCenters 
            WHERE StaffId = @StaffId
            ", con);

            cmd.Parameters.AddWithValue("@StaffId", staffId);

            con.Open();

            var result = cmd.ExecuteScalar();

            if (result != null && result != DBNull.Value)
                return Convert.ToInt32(result);

            return 0; // fallback (no mapping found)
        }

        public List<MilkTypes> GetMilkTypes()
        {
            var list = new List<MilkTypes>();

            using var con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand("SELECT MilkTypeId, MilkTypeName FROM Finance.MilkTypes", con);

            con.Open();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new MilkTypes
                {
                    MilkTypeId = Convert.ToInt32(reader["MilkTypeId"]),
                    MilkTypeName = reader["MilkTypeName"].ToString()
                });
            }

            return list;
        }



        public List<DateWiseMilkEntryViewModel> GetAllEntries(int centerId)
        {
            var list = new List<DateWiseMilkEntryViewModel>();

            using (SqlConnection con = _dbHelper.GetConnection())
            using (SqlCommand cmd = new SqlCommand(@"
                SELECT
                    mc.CollectionId,
                    mc.CollectionDate,
                    mc.Shift,
                    mc.Quantity,
                    mc.AppliedFat,
                    mc.AppliedCLR,
                    mc.RatePerLiter,
                    mc.Amount,
                    f.FarmerName,
                    f.FarmerCode
                FROM Collection.MilkCollection mc
                INNER JOIN Farmer.Farmers f ON mc.FarmerId = f.FarmerId
                WHERE mc.CenterId = @CenterId
                ORDER BY mc.CollectionDate DESC, mc.CollectionId DESC
                ", con))
            {
                cmd.Parameters.AddWithValue("@CenterId", centerId);

                con.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new DateWiseMilkEntryViewModel
                        {
                            CollectionId = (int)reader["CollectionId"],
                            CollectionDate = (DateTime)reader["CollectionDate"],
                            Shift = reader["Shift"].ToString(),
                            Quantity = (decimal)reader["Quantity"],
                            AppliedFat = (decimal)reader["AppliedFat"],
                            AppliedCLR = (decimal)reader["AppliedCLR"],
                            RatePerLiter = (decimal)reader["RatePerLiter"],
                            Amount = (decimal)reader["Amount"],
                            FarmerName = reader["FarmerName"].ToString(),
                            FarmerCode = reader["FarmerCode"].ToString()
                        });
                    }
                }
            }

            return list;
        }
        //Inventory by center 
        public List<CenterInventoryViewModel> GetInventoryByCenter(int centerId)
        {
            var list = new List<CenterInventoryViewModel>();

            using var con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand("Collection.usp_Collection_GetCenterInventory", con);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@CenterId", centerId);

            con.Open();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new CenterInventoryViewModel
                {
                    CenterId = Convert.ToInt32(reader["CenterId"]),
                    CenterName = reader["CenterName"]?.ToString(),
                    MilkTypeName = reader["MilkTypeName"]?.ToString(),
                    AvailableQuantity = reader["AvailableQuantity"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["AvailableQuantity"]),
                    LastUpdated = Convert.ToDateTime(reader["LastUpdated"])
                });
            }

            return list;
        }


        //Farmer receipt
        public FarmerReceiptViewModel GetReceiptByCollectionId(int id)
        {
            using var con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand(@"
        SELECT 
            fr.ReceiptId,
            fr.ReceiptNumber,
            fr.ReceiptDate,
            mc.CollectionId,
            mc.CollectionDate,
            mc.Shift,
            mc.Quantity,
            mc.AppliedFat,
            mc.AppliedCLR,
            mc.RatePerLiter,
            mc.Amount,
            f.FarmerId,
            f.FarmerName,
            f.FarmerCode,
            mt.MilkTypeName,
            cc.CenterName
        FROM Collection.FarmerReceipts fr
        JOIN Collection.MilkCollection mc ON mc.CollectionId = fr.CollectionId
        JOIN Farmer.Farmers f ON f.FarmerId = mc.FarmerId
        JOIN Finance.MilkTypes mt ON mt.MilkTypeId = mc.MilkTypeId
        JOIN Collection.CollectionCenters cc ON cc.CenterId = mc.CenterId
        WHERE mc.CollectionId = @Id
    ", con);

            cmd.Parameters.AddWithValue("@Id", id);

            con.Open();
            using var reader = cmd.ExecuteReader();

            if (!reader.Read()) return null;

            return new FarmerReceiptViewModel
            {
                ReceiptId = Convert.ToInt32(reader["ReceiptId"]),
                ReceiptNumber = reader["ReceiptNumber"].ToString(),
                ReceiptDate = Convert.ToDateTime(reader["ReceiptDate"]),
                CollectionId = Convert.ToInt32(reader["CollectionId"]),
                CollectionDate = Convert.ToDateTime(reader["CollectionDate"]),
                Shift = reader["Shift"].ToString(),
                Quantity = Convert.ToDecimal(reader["Quantity"]),
                AppliedFat = reader["AppliedFat"] as decimal?,
                AppliedCLR = reader["AppliedCLR"] as decimal?,
                RatePerLiter = reader["RatePerLiter"] as decimal?,
                Amount = reader["Amount"] as decimal?,
                FarmerId = Convert.ToInt32(reader["FarmerId"]),
                FarmerName = reader["FarmerName"].ToString(),
                FarmerCode = reader["FarmerCode"].ToString(),
                MilkTypeName = reader["MilkTypeName"].ToString(),
                CenterName = reader["CenterName"].ToString()
            };
        }
        public List<RateChartModel> GetRateCharts()
        {
            var list = new List<RateChartModel>();

            using var con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand(@"
                SELECT rc.RateChartId, rc.MilkTypeId, mt.MilkTypeName,
                       rc.FatFrom, rc.FatTo,
                       rc.CLRFrom, rc.CLRTo,
                       rc.RatePerLiter, rc.EffectiveFrom
                FROM Finance.RateCharts rc
                INNER JOIN Finance.MilkTypes mt ON rc.MilkTypeId = mt.MilkTypeId
                ORDER BY mt.MilkTypeName, rc.FatFrom, rc.CLRFrom
            ", con);

            con.Open();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new RateChartModel
                {
                    RateChartId = (int)reader["RateChartId"],
                    MilkTypeId = (int)reader["MilkTypeId"],
                    MilkTypeName = reader["MilkTypeName"].ToString(),
                    FatFrom = (decimal)reader["FatFrom"],
                    FatTo = (decimal)reader["FatTo"],
                    CLRFrom = (decimal)reader["CLRFrom"],
                    CLRTo = (decimal)reader["CLRTo"],
                    RatePerLiter = (decimal)reader["RatePerLiter"],
                    EffectiveFrom = (DateTime)reader["EffectiveFrom"]
                });
            }

            return list;
        }

        //for self registration of farmer

        public List<CenterDropdownModel> GetCentersByVillage(int villageId)
        {
            var list = new List<CenterDropdownModel>();

            using var con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand("Collection.usp_GetCentersByVillage", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@VillageId", villageId);

            con.Open();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new CenterDropdownModel
                {
                    CenterId = Convert.ToInt32(reader["CenterId"]),
                    CenterName = reader["CenterName"].ToString(),
                    Location = reader["Location"] == DBNull.Value ? null : reader["Location"].ToString()
                });
            }

            return list;
        }

        // ─────────────────────────────────────────────────────────────
        // Closed/partially-dispatched batches that still have milk left.
        // SP: Collection.usp_GetClosedBatchesForDispatch  (FIXED)
        // Returns RemainingQty so dropdown shows how much is left.
        // ─────────────────────────────────────────────────────────────
        public List<ClosedBatchDropdownItem> GetClosedBatchesForDispatch(int centerId)
        {
            var list = new List<ClosedBatchDropdownItem>();

            using var con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand("Collection.usp_GetClosedBatchesForDispatch", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@CenterId", centerId);

            con.Open();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new ClosedBatchDropdownItem
                {
                    BatchId = Convert.ToInt32(reader["BatchId"]),
                    BatchDate = Convert.ToDateTime(reader["BatchDate"]),
                    Shift = reader["Shift"]?.ToString(),
                    TotalQuantity = reader["TotalQuantity"] == DBNull.Value ? null : Convert.ToDecimal(reader["TotalQuantity"]),
                    TotalDispatched = reader["TotalDispatched"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["TotalDispatched"]),
                    RemainingQty = reader["RemainingQty"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["RemainingQty"]),
                    AvgFat = reader["AvgFat"] == DBNull.Value ? null : Convert.ToDecimal(reader["AvgFat"]),
                    AvgCLR = reader["AvgCLR"] == DBNull.Value ? null : Convert.ToDecimal(reader["AvgCLR"])
                });
            }

            return list;
        }

        // ─────────────────────────────────────────────────────────────
        // Active vehicles with driver.
        // SP: Collection.usp_GetActiveVehiclesForDispatch
        // ─────────────────────────────────────────────────────────────
        public List<VehicleDropdownItem> GetActiveVehicles()
        {
            var list = new List<VehicleDropdownItem>();

            using var con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand("Collection.usp_GetActiveVehiclesForDispatch", con);
            cmd.CommandType = CommandType.StoredProcedure;

            con.Open();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new VehicleDropdownItem
                {
                    VehicleId = Convert.ToInt32(reader["VehicleId"]),
                    VehicleNumber = reader["VehicleNumber"]?.ToString(),
                    DriverName = reader["DriverName"]?.ToString()
                });
            }

            return list;
        }

        // ─────────────────────────────────────────────────────────────
        // All processing plants.
        // SP: Collection.usp_GetPlantsForDispatch
        // ─────────────────────────────────────────────────────────────
        public List<PlantDropdownItem> GetAllPlants()
        {
            var list = new List<PlantDropdownItem>();

            using var con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand("Collection.usp_GetPlantsForDispatch", con);
            cmd.CommandType = CommandType.StoredProcedure;

            con.Open();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new PlantDropdownItem
                {
                    PlantId = Convert.ToInt32(reader["PlantId"]),
                    PlantName = reader["PlantName"]?.ToString(),
                    Location = reader["Location"]?.ToString()
                });
            }

            return list;
        }
        public int DispatchMilkTransfer(int batchId, int milkTypeId, 
                                         int vehicleId, int plantId,
                                         decimal dispatchQty, DateTime dispatchDate)
        {
            using var con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand(
                "Production.usp_Production_DispatchMilkTransfer", con);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@BatchId", batchId);
            cmd.Parameters.AddWithValue("@MilkTypeId", milkTypeId);  
            cmd.Parameters.AddWithValue("@VehicleId", vehicleId);
            cmd.Parameters.AddWithValue("@PlantId", plantId);
            cmd.Parameters.AddWithValue("@DispatchQty", dispatchQty);
            cmd.Parameters.AddWithValue("@DispatchDate", dispatchDate.Date);

            try
            {
                con.Open();
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                    return Convert.ToInt32(reader["NewTransferId"]);
                return 0;
            }
            catch (SqlException ex)
            {
                throw new Exception(ex.Message);
            }
        }

        // GetDispatchHistory — add MilkTypeName
        public List<DispatchHistoryViewModel> GetDispatchHistory(int centerId)
        {
            var list = new List<DispatchHistoryViewModel>();

            using var con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand(
                "Collection.usp_GetDispatchHistoryByCenter", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@CenterId", centerId);

            con.Open();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new DispatchHistoryViewModel
                {
                    TransferId = Convert.ToInt32(reader["TransferId"]),
                    DispatchDate = Convert.ToDateTime(reader["DispatchDate"]),
                    DispatchQty = Convert.ToDecimal(reader["DispatchQty"]),
                    ReceivedQty = reader["ReceivedQty"] == DBNull.Value ? null : Convert.ToDecimal(reader["ReceivedQty"]),
                    LossQty = reader["LossQty"] == DBNull.Value ? null : Convert.ToDecimal(reader["LossQty"]),
                    ReceivedDate = reader["ReceivedDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["ReceivedDate"]),
                    BatchId = Convert.ToInt32(reader["BatchId"]),
                    BatchDate = Convert.ToDateTime(reader["BatchDate"]),
                    Shift = reader["Shift"]?.ToString(),
                    BatchTotalQty = reader["BatchTotalQty"] == DBNull.Value ? null : Convert.ToDecimal(reader["BatchTotalQty"]),
                    BatchRemainingQty = reader["BatchRemainingQty"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["BatchRemainingQty"]),
                    PlantName = reader["PlantName"]?.ToString(),
                    VehicleNumber = reader["VehicleNumber"]?.ToString(),
                    DriverName = reader["DriverName"]?.ToString(),
                    TransferStatus = reader["TransferStatus"]?.ToString(),

                    //  NEW
                    MilkTypeName = reader["MilkTypeName"]?.ToString()
                });
            }

            return list;
        }
    }
 }
