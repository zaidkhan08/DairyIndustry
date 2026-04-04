using DairyIndustry.Data;
using DairyIndustry.Models.Admin;
using DairyIndustry.Models.Collection;
using DairyIndustry.Models.FarmerModel;
using DairyIndustry.Repositories;
using Microsoft.Data.SqlClient;
using System.Data;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DairyIndustry.Repository
{
    public class CollectionCenterRepository : ICollectionCenterRepository
    {
        private readonly DbHelper _dbHelper;

        public CollectionCenterRepository(DbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        // ─────────────────────────────────────────────────────────────
        // CURRENT SHIFT (UI DISPLAY ONLY — actual enforcement is in SP)
        // Matches SP windows: Morning 08:00–11:00, Evening 16:00–19:00
        // ─────────────────────────────────────────────────────────────
        public string GetCurrentShift()
        {
            var now = DateTime.Now.TimeOfDay;

            // Morning: 10:30 AM – 11:00 AM
            if (now >= new TimeSpan(10, 30, 0) && now < new TimeSpan(11, 0, 0))
                return "Morning";

            //  Evening: 4:00 PM – 7:00 PM
            if (now >= new TimeSpan(16, 0, 0) && now < new TimeSpan(19, 0, 0))
                return "Evening";

            return "No Active Shift";
        }

        //public string GetCurrentShift()
        //{
        //    var now = DateTime.Now.TimeOfDay;

        //    if (now >= TimeSpan.FromHours(8) && now < TimeSpan.FromHours(11))
        //        return "Morning";

        //    if (now >= TimeSpan.FromHours(16) && now < TimeSpan.FromHours(19))
        //        return "Evening";

        //    return "No Active Shift"; //  important
        //}
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
        // ─────────────────────────────────────────────────────────────
        // ADD MILK ENTRY — Collection.usp_AddMilkEntry
        // FIX: Old code had wrong SP name ("SP_06A_MilkEntry_Add") and
        //      passed ~10 wrong params. SP only needs these 6.
        //      SP auto-detects: CenterId (from StaffId via StaffCenters),
        //      Shift (from server time), BatchId (open batch), Rate, Amount.
        // ─────────────────────────────────────────────────────────────
        //public int AddMilkCollection(int staffId, int farmerId, int milkTypeId,
        //                             decimal quantity, decimal appliedFat, decimal appliedCLR)
        //{
        //    using var con = _dbHelper.GetConnection();
        //    using var cmd = new SqlCommand("Collection.usp_AddMilkEntry", con);
        //    cmd.CommandType = CommandType.StoredProcedure;

        //    cmd.Parameters.AddWithValue("@StaffId", staffId);
        //    cmd.Parameters.AddWithValue("@FarmerId", farmerId);
        //    cmd.Parameters.AddWithValue("@MilkTypeId", milkTypeId);
        //    cmd.Parameters.AddWithValue("@Quantity", quantity);
        //    cmd.Parameters.AddWithValue("@AppliedFat", appliedFat);
        //    cmd.Parameters.AddWithValue("@AppliedCLR", appliedCLR);

        //    con.Open();
        //    return Convert.ToInt32(cmd.ExecuteScalar());
        //}
        //public List<RateChartModel> GetRateChart(int milkTypeId)
        //{
        //    var list = new List<RateChartModel>();

        //    using var con = _dbHelper.GetConnection();

        //    using var cmd = new SqlCommand(@"
        //        SELECT 
        //            rc.*,
        //            mt.MilkTypeName
        //        FROM Finance.RateCharts rc
        //        INNER JOIN Finance.MilkTypes mt 
        //            ON rc.MilkTypeId = mt.MilkTypeId
        //        WHERE rc.MilkTypeId = @MilkTypeId
        //        AND rc.EffectiveFrom = (
        //            SELECT MAX(EffectiveFrom)
        //            FROM Finance.RateCharts
        //            WHERE MilkTypeId = @MilkTypeId
        //        )
        //        ORDER BY rc.FatFrom
        //    ", con);

        //    cmd.Parameters.AddWithValue("@MilkTypeId", milkTypeId);

        //    con.Open();
        //    using var reader = cmd.ExecuteReader();

        //    while (reader.Read())
        //    {
        //        list.Add(new RateChartModel
        //        {
        //            RateChartId = Convert.ToInt32(reader["RateChartId"]),
        //            MilkTypeId = Convert.ToInt32(reader["MilkTypeId"]),
        //            MilkTypeName = reader["MilkTypeName"]?.ToString()
        //            FatFrom = Convert.ToDecimal(reader["FatFrom"]),
        //            FatTo = Convert.ToDecimal(reader["FatTo"]),
        //            CLRFrom = Convert.ToDecimal(reader["CLRFrom"]),
        //            CLRTo = Convert.ToDecimal(reader["CLRTo"]),
        //            RatePerLiter = Convert.ToDecimal(reader["RatePerLiter"]),
        //            EffectiveFrom = Convert.ToDateTime(reader["EffectiveFrom"])
        //        });
        //    }

        //    return list;
        //}

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

                // SP does INSERT only — no return value
                cmd.ExecuteNonQuery();

                return 1; // success
            }
            catch (SqlException ex)
            {
                // VERY IMPORTANT → pass exact DB error to controller
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
                        FarmerName = reader["FarmerName"].ToString()
                    });
                }
            }

            return farmers;
        }
        //public int GetCenterIdByStaffId(int staffId)
        //{
        //    using var con = _dbHelper.GetConnection();
        //    using var cmd = new SqlCommand("SELECT CenterId FROM Collection.StaffCenters WHERE StaffId = @StaffId", con);

        //    cmd.Parameters.AddWithValue("@StaffId", staffId);

        //    con.Open();
        //    return Convert.ToInt32(cmd.ExecuteScalar());
        //}
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

        public List<DateWiseMilkEntryViewModel> GetEntriesByDate(DateTime date, int centerId)
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
                WHERE CAST(mc.CollectionDate AS DATE) = CAST(@Date AS DATE)
                AND mc.CenterId = @CenterId
                ORDER BY mc.Shift, mc.CollectionId
            ", con))
            {
                cmd.Parameters.AddWithValue("@Date", date);
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

        //inventory by center 
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

        //farmer receipt
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
    }
}
