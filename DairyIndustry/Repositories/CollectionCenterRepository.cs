using DairyIndustry.Data;
using DairyIndustry.Models.Admin;
using DairyIndustry.Models.Collection;
using DairyIndustry.Models.FarmerModel;
using DairyIndustry.Repositories;
using Microsoft.Data.SqlClient;
using System.Data;
using CenterDropdownModel = DairyIndustry.Models.FarmerModel.CenterDropdownModel;

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

            if (now >= new TimeSpan(10, 0, 0) && now < new TimeSpan(12, 0
                , 0))
                return "Morning";

            if (now >= new TimeSpan(15, 0, 0) && now < new TimeSpan(16, 0, 0))
                return "Evening";

            return "No Active Shift";
        }


        /* ============================================================
           1. STAFF + CENTER INFO
           SP: Collection.usp_Dashboard_StaffCenter
           ============================================================ */
        public StaffCenterModel GetStaffCenter(int staffId)
        {
            using SqlConnection con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand("Collection.usp_Dashboard_StaffCenter", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@StaffId", staffId);

            con.Open();
            using var r = cmd.ExecuteReader();

            if (!r.Read())
                return null;

            return new StaffCenterModel
            {
                StaffId = Convert.ToInt32(r["StaffId"]),
                StaffName = r["StaffName"].ToString(),
                Email = r["Email"] == DBNull.Value ? null : r["Email"].ToString(),
                StaffPhone = r["StaffPhone"] == DBNull.Value ? null : r["StaffPhone"].ToString(),
                StaffPhoto = r["StaffPhoto"] == DBNull.Value ? null : r["StaffPhoto"].ToString(),
                DOJ = r["DOJ"] == DBNull.Value ? null : Convert.ToDateTime(r["DOJ"]),
                RoleId = Convert.ToInt32(r["RoleId"]),
                RoleName = r["RoleName"].ToString(),

                CenterId = Convert.ToInt32(r["CenterId"]),
                CenterName = r["CenterName"].ToString(),
                Capacity = r["Capacity"] == DBNull.Value ? null : Convert.ToDecimal(r["Capacity"]),

                VillageId = Convert.ToInt32(r["VillageId"]),
                VillageName = r["VillageName"].ToString(),
                CityId = Convert.ToInt32(r["CityId"]),
                CityName = r["CityName"].ToString(),
                StateId = Convert.ToInt32(r["StateId"]),
                StateName = r["StateName"].ToString()
            };
        }

        /* ============================================================
           2. TODAY'S SUMMARY
           SP: Collection.usp_Dashboard_TodaySummary
           ============================================================ */
        public TodaySummaryModel GetTodaySummary(int staffId)
        {

            using SqlConnection con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand("Collection.usp_Dashboard_TodaySummary", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@StaffId", staffId);

            con.Open();
            using var r = cmd.ExecuteReader();

            if (!r.Read())
                return new TodaySummaryModel();

            return new TodaySummaryModel
            {
                TotalMilkToday = r["TotalMilkToday"] == DBNull.Value ? 0 : Convert.ToDecimal(r["TotalMilkToday"]),
                MorningQty = r["MorningQty"] == DBNull.Value ? 0 : Convert.ToDecimal(r["MorningQty"]),
                EveningQty = r["EveningQty"] == DBNull.Value ? 0 : Convert.ToDecimal(r["EveningQty"]),

                TotalAmountToday = r["TotalAmountToday"] == DBNull.Value ? 0 : Convert.ToDecimal(r["TotalAmountToday"]),
                MorningAmount = r["MorningAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(r["MorningAmount"]),
                EveningAmount = r["EveningAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(r["EveningAmount"]),

                TotalEntriesToday = r["TotalEntriesToday"] == DBNull.Value ? 0 : Convert.ToInt32(r["TotalEntriesToday"]),
                MorningEntries = r["MorningEntries"] == DBNull.Value ? 0 : Convert.ToInt32(r["MorningEntries"]),
                EveningEntries = r["EveningEntries"] == DBNull.Value ? 0 : Convert.ToInt32(r["EveningEntries"]),

                AvgFatToday = r["AvgFatToday"] == DBNull.Value ? null : Convert.ToDecimal(r["AvgFatToday"]),
                AvgCLRToday = r["AvgCLRToday"] == DBNull.Value ? null : Convert.ToDecimal(r["AvgCLRToday"]),

                PendingPaymentAmount = r["PendingPaymentAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(r["PendingPaymentAmount"]),

                RejectionsToday = r["RejectionsToday"] == DBNull.Value ? 0 : Convert.ToInt32(r["RejectionsToday"]),
                RejectedQtyToday = r["RejectedQtyToday"] == DBNull.Value ? 0 : Convert.ToDecimal(r["RejectedQtyToday"]),
                MorningRejections =r["MorningRejections"] == DBNull.Value ? 0 :Convert.ToInt32(r["MorningRejections"]),

                EveningRejections =r["EveningRejections"] == DBNull.Value ? 0 :Convert.ToInt32(r["EveningRejections"]),
                MorningRejectedQty =r["MorningRejectedQty"] == DBNull.Value ? 0 :Convert.ToDecimal(r["MorningRejectedQty"]),
                EveningRejectedQty =r["EveningRejectedQty"] == DBNull.Value ? 0 :Convert.ToDecimal(r["EveningRejectedQty"]),
            };
        }


        /* ============================================================
           3. SHIFT STATUS (Morning + Evening)
           SP: Collection.usp_Dashboard_ShiftStatus
           ============================================================ */
        public List<ShiftStatusModel> GetShiftStatus(int staffId)
        {
            var list = new List<ShiftStatusModel>();


            using SqlConnection con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand("Collection.usp_Dashboard_ShiftStatus", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@StaffId", staffId);

            con.Open();
            using var r = cmd.ExecuteReader();

            while (r.Read())
            {
                list.Add(new ShiftStatusModel
                {
                    Shift = r["Shift"].ToString(),
                    ShiftWindow = r["ShiftWindow"].ToString(),
                    BatchId = r["BatchId"] == DBNull.Value ? null : Convert.ToInt32(r["BatchId"]),
                    BatchStatus = r["BatchStatus"].ToString(),
                    TotalQuantity = r["TotalQuantity"] == DBNull.Value ? 0 : Convert.ToDecimal(r["TotalQuantity"]),
                    AvgFat = r["AvgFat"] == DBNull.Value ? null : Convert.ToDecimal(r["AvgFat"]),
                    AvgCLR = r["AvgCLR"] == DBNull.Value ? null : Convert.ToDecimal(r["AvgCLR"]),
                    EntryCount = r["EntryCount"] == DBNull.Value ? 0 : Convert.ToInt32(r["EntryCount"]),
                    RejectionCount = r["RejectionCount"] == DBNull.Value ? 0 : Convert.ToInt32(r["RejectionCount"]),
                    IsCurrentShift = r["IsCurrentShift"] != DBNull.Value && Convert.ToInt32(r["IsCurrentShift"]) == 1,
                    BatchDate = Convert.ToDateTime(r["BatchDate"])
                });
            }

            return list;
        }


        /* ============================================================
           4. INVENTORY
           SP: Collection.usp_Dashboard_Inventory
           ============================================================ */
        public List<InventoryModel> GetInventory(int staffId)
        {
            var list = new List<InventoryModel>();

            using SqlConnection con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand("Collection.usp_Dashboard_Inventory", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@StaffId", staffId);

            con.Open();
            using var r = cmd.ExecuteReader();

            while (r.Read())
            {
                list.Add(new InventoryModel
                {
                    InventoryId = Convert.ToInt32(r["InventoryId"]),
                    MilkTypeId = Convert.ToInt32(r["MilkTypeId"]),
                    MilkTypeName = r["MilkTypeName"].ToString(),
                    AvailableQuantity = r["AvailableQuantity"] == DBNull.Value ? 0 : Convert.ToDecimal(r["AvailableQuantity"]),
                    CenterCapacity = r["CenterCapacity"] == DBNull.Value ? null : Convert.ToDecimal(r["CenterCapacity"]),
                    CapacityUsedPct = r["CapacityUsedPct"] == DBNull.Value ? 0 : Convert.ToDecimal(r["CapacityUsedPct"]),
                    CollectedTodayQty = r["CollectedTodayQty"] == DBNull.Value ? 0 : Convert.ToDecimal(r["CollectedTodayQty"]),
                    LastUpdated = Convert.ToDateTime(r["LastUpdated"])
                });
            }

            return list;
        }
        /* ============================================================
           5. FARMER STATS
           SP: Collection.usp_Dashboard_FarmerStats
           ============================================================ */
        public FarmerStatsModel GetFarmerStats(int staffId)
        {

            using SqlConnection con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand("Collection.usp_Dashboard_FarmerStats", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@StaffId", staffId);

            con.Open();
            using var r = cmd.ExecuteReader();

            if (!r.Read())
                return new FarmerStatsModel();

            return new FarmerStatsModel
            {
                TotalFarmers = r["TotalFarmers"] == DBNull.Value ? 0 : Convert.ToInt32(r["TotalFarmers"]),
                ActiveFarmers = r["ActiveFarmers"] == DBNull.Value ? 0 : Convert.ToInt32(r["ActiveFarmers"]),
                InactiveFarmers = r["InactiveFarmers"] == DBNull.Value ? 0 : Convert.ToInt32(r["InactiveFarmers"]),
                PendingApprovals = r["PendingApprovals"] == DBNull.Value ? 0 : Convert.ToInt32(r["PendingApprovals"]),
                RejectedFarmers = r["RejectedFarmers"] == DBNull.Value ? 0 : Convert.ToInt32(r["RejectedFarmers"]),
                FarmersDeliveredToday = r["FarmersDeliveredToday"] == DBNull.Value ? 0 : Convert.ToInt32(r["FarmersDeliveredToday"]),
                FarmersPendingPayment = r["FarmersPendingPayment"] == DBNull.Value ? 0 : Convert.ToInt32(r["FarmersPendingPayment"]),
                TotalPendingPaymentAmount = r["TotalPendingPaymentAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(r["TotalPendingPaymentAmount"])
            };
        }

        public List<TopFarmerModel> GetTopFarmersThisMonth(int staffId)
        {
            var list = new List<TopFarmerModel>();

            using SqlConnection con = _dbHelper.GetConnection();

            string sql = @"
                DECLARE @CenterId INT;
                SELECT @CenterId = CenterId FROM HR.Staffs WHERE StaffId = @StaffId;

                SELECT TOP 5
                    f.FarmerId,
                    f.FarmerName,
                    f.FarmerCode,
                    ISNULL(SUM(mc.Quantity), 0)                  AS TotalQty,
                    ISNULL(SUM(mc.Amount), 0)                    AS TotalAmount,
                    COUNT(DISTINCT mc.CollectionDate)             AS DaysDelivered
                FROM Collection.MilkCollection mc
                INNER JOIN Farmer.Farmers f ON f.FarmerId = mc.FarmerId
                WHERE mc.CenterId = @CenterId
                  AND mc.CollectionDate >= DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1)
                  AND mc.CollectionDate <= CAST(GETDATE() AS DATE)
                GROUP BY f.FarmerId, f.FarmerName, f.FarmerCode
                ORDER BY SUM(mc.Quantity) DESC";

            using SqlCommand cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@StaffId", staffId);

            con.Open();
            using SqlDataReader r = cmd.ExecuteReader();

            while (r.Read())
            {
                list.Add(new TopFarmerModel
                {
                    FarmerId = Convert.ToInt32(r["FarmerId"]),
                    FarmerName = r["FarmerName"].ToString(),
                    FarmerCode = r["FarmerCode"].ToString(),
                    TotalQty = Convert.ToDecimal(r["TotalQty"]),
                    TotalAmount = Convert.ToDecimal(r["TotalAmount"]),
                    DaysDelivered = Convert.ToInt32(r["DaysDelivered"])
                });
            }

            return list;
        }

        //payment kpi
        public PaymentStatsModel GetPaymentStats(int staffId)
        {
            using SqlConnection con = _dbHelper.GetConnection();

            string sql = @"
                DECLARE @CenterId INT;
                SELECT @CenterId = CenterId FROM HR.Staffs WHERE StaffId = @StaffId;

                SELECT
                    SUM(CASE WHEN fp.PaymentStatus = 'Pending'
                        THEN 1 ELSE 0 END)                      AS PendingCount,
                    ISNULL(SUM(CASE WHEN fp.PaymentStatus = 'Pending'
                        THEN fp.TotalAmount ELSE 0 END), 0)     AS PendingAmount,

                    SUM(CASE WHEN fp.PaymentStatus = 'Processed'
                        THEN 1 ELSE 0 END)                      AS ProcessedCount,
                    ISNULL(SUM(CASE WHEN fp.PaymentStatus = 'Processed'
                        THEN fp.TotalAmount ELSE 0 END), 0)     AS ProcessedAmount,

                    SUM(CASE WHEN fp.PaymentStatus = 'Failed'
                        THEN 1 ELSE 0 END)                      AS FailedCount,
                    ISNULL(SUM(CASE WHEN fp.PaymentStatus = 'Failed'
                        THEN fp.TotalAmount ELSE 0 END), 0)     AS FailedAmount

                FROM Finance.FarmerPayments fp
                WHERE fp.CenterId = @CenterId";

            using SqlCommand cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@StaffId", staffId);

            con.Open();
            using var r = cmd.ExecuteReader();

            if (!r.Read())
                return new PaymentStatsModel();

            return new PaymentStatsModel
            {
                PendingCount = r["PendingCount"] == DBNull.Value ? 0 : Convert.ToInt32(r["PendingCount"]),
                PendingAmount = r["PendingAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(r["PendingAmount"]),
                ProcessedCount = r["ProcessedCount"] == DBNull.Value ? 0 : Convert.ToInt32(r["ProcessedCount"]),
                ProcessedAmount = r["ProcessedAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(r["ProcessedAmount"]),
                FailedCount = r["FailedCount"] == DBNull.Value ? 0 : Convert.ToInt32(r["FailedCount"]),
                FailedAmount = r["FailedAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(r["FailedAmount"])
            };
        }
        public List<CollectionTrendModel> GetCollectionTrend(int staffId)
        {
            var list = new List<CollectionTrendModel>();

            using SqlConnection con = _dbHelper.GetConnection();

            string query = @"
                SELECT
                    mc.CollectionDate,
                    SUM(mc.Quantity) AS TotalQuantity
                FROM Collection.MilkCollection mc
                INNER JOIN HR.Staffs s
                    ON s.CenterId = mc.CenterId
                WHERE s.StaffId = @StaffId
                  AND mc.CollectionDate >= DATEADD(DAY, -6, CAST(GETDATE() AS DATE))
                GROUP BY mc.CollectionDate
                ORDER BY mc.CollectionDate";

            using SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@StaffId", staffId);

            con.Open();

            using SqlDataReader r = cmd.ExecuteReader();

            while (r.Read())
            {
                list.Add(new CollectionTrendModel
                {
                    CollectionDate = Convert.ToDateTime(r["CollectionDate"]),
                    TotalQuantity = Convert.ToDecimal(r["TotalQuantity"])
                });
            }

            return list;
        }
        public List<CollectionRejectionTrendModel> GetCollectionVsRejectionTrend(int staffId)
        {
            var list = new List<CollectionRejectionTrendModel>();

            using SqlConnection con = _dbHelper.GetConnection();

            string sql = @"
                DECLARE @CenterId INT;

                SELECT @CenterId = CenterId
                FROM HR.Staffs
                WHERE StaffId = @StaffId;

                ;WITH Last7Days AS
                (
                    SELECT CAST(DATEADD(DAY,-6,GETDATE()) AS DATE) AS Dt
                    UNION ALL
                    SELECT DATEADD(DAY,1,Dt)
                    FROM Last7Days
                    WHERE Dt < CAST(GETDATE() AS DATE)
                )
                SELECT
                    d.Dt AS TrendDate,

                    ISNULL(
                    (
                        SELECT SUM(mc.Quantity)
                        FROM Collection.MilkCollection mc
                        WHERE mc.CenterId = @CenterId
                          AND mc.CollectionDate = d.Dt
                    ),0) AS CollectedQty,

                    ISNULL(
                    (
                        SELECT SUM(mr.Quantity)
                        FROM Collection.MilkRejections mr
                        WHERE mr.CenterId = @CenterId
                          AND mr.RejectionDate = d.Dt
                    ),0) AS RejectedQty

                FROM Last7Days d
                OPTION (MAXRECURSION 7);";

            using SqlCommand cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@StaffId", staffId);

            con.Open();

            using SqlDataReader r = cmd.ExecuteReader();

            while (r.Read())
            {
                list.Add(new CollectionRejectionTrendModel
                {
                    Date = Convert.ToDateTime(r["TrendDate"]),
                    CollectedQty = Convert.ToDecimal(r["CollectedQty"]),
                    RejectedQty = Convert.ToDecimal(r["RejectedQty"])
                });
            }

            return list;
        }
        public List<RejectionReasonModel> GetRejectionReasons(int staffId)
        {
            var list = new List<RejectionReasonModel>();

            using SqlConnection con = _dbHelper.GetConnection();

            string sql = @"
                SELECT
                    mr.RejectionReason,
                    COUNT(*) AS RejectionCount
                FROM Collection.MilkRejections mr
                INNER JOIN HR.Staffs s
                    ON s.CenterId = mr.CenterId
                WHERE s.StaffId = @StaffId
                  AND mr.RejectionDate >= DATEADD(DAY,-30,CAST(GETDATE() AS DATE))
                GROUP BY mr.RejectionReason
                ORDER BY COUNT(*) DESC";

            using SqlCommand cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@StaffId", staffId);

            con.Open();

            using SqlDataReader r = cmd.ExecuteReader();

            while (r.Read())
            {
                list.Add(new RejectionReasonModel
                {
                    RejectionReason = r["RejectionReason"].ToString(),
                    RejectionCount = Convert.ToInt32(r["RejectionCount"])
                });
            }

            return list;
        }
        public int AddMilkCollection(int staffId, int farmerId, int milkTypeId,decimal quantity, decimal appliedFat, decimal appliedCLR)
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
        // ─────────────────────────────────────────────────────────────
        public List<MilkCollectionModel> GetTodayMilkEntries(int staffId, string shift = null)
        {
            var list = new List<MilkCollectionModel>();

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
                list.Add(new MilkCollectionModel
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

        public int RejectMilkEntry(MilkRejectionModel model, int staffId)
        {
            using var con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand("Collection.usp_RejectMilkEntryCenter", con);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@StaffId", staffId);
            cmd.Parameters.AddWithValue("@FarmerId", model.FarmerId);
            cmd.Parameters.AddWithValue("@MilkTypeId", model.MilkTypeId);
            cmd.Parameters.AddWithValue("@AppliedFat", model.AppliedFat.HasValue? model.AppliedFat.Value: DBNull.Value);
            cmd.Parameters.AddWithValue("@AppliedCLR",model.AppliedCLR.HasValue? model.AppliedCLR.Value: DBNull.Value);
            cmd.Parameters.AddWithValue("@Quantity", model.Quantity);
            cmd.Parameters.AddWithValue("@RejectionReason", model.RejectionReason);
            cmd.Parameters.AddWithValue("@Remarks",string.IsNullOrWhiteSpace(model.Remarks)? DBNull.Value: model.Remarks);

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



        public List<MilkRejectionModel> GetRejectionsByFarmer(int farmerId)
        {
            var list = new List<MilkRejectionModel>();

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
                list.Add(new MilkRejectionModel
                {
                    RejectionId = Convert.ToInt32(reader["RejectionId"]),
                    RejectionDate = Convert.ToDateTime(reader["RejectionDate"]),
                    Shift = reader["Shift"]?.ToString(),
                    AppliedFat = reader["AppliedFat"] == DBNull.Value? (decimal?)null: Convert.ToDecimal(reader["AppliedFat"]),
                    AppliedCLR = reader["AppliedCLR"] == DBNull.Value? (decimal?)null: Convert.ToDecimal(reader["AppliedCLR"]),
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
       

        // ─────────────────────────────────────────────────────────────
        // BATCH STATUS — Collection.usp_GetTodayBatchStatus
        // ─────────────────────────────────────────────────────────────
        public List<BatchStatusViewModel> GetTodayBatchStatus(int staffId)
        {
            List<BatchStatusViewModel> batches =new List<BatchStatusViewModel>();

            using (SqlConnection con = _dbHelper.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Collection.usp_GetTodayBatchStatus", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 60;

                    cmd.Parameters.AddWithValue("@StaffId", staffId);

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        //------------------------------------------------
                        // SHIFT CARDS
                        //------------------------------------------------

                        while (reader.Read())
                        {
                            batches.Add(new BatchStatusViewModel
                            {
                                BatchId = reader["BatchId"] == DBNull.Value? null: Convert.ToInt32(reader["BatchId"]),
                                Shift = reader["Shift"]?.ToString(),
                                ShiftWindow = reader["ShiftWindow"]?.ToString(),
                                BatchStatus = reader["BatchStatus"]?.ToString(),
                                TotalQuantity = reader["TotalQuantity"] == DBNull.Value? 0: Convert.ToDecimal(reader["TotalQuantity"]),
                                AvgFat = reader["AvgFat"] == DBNull.Value? null: Convert.ToDecimal(reader["AvgFat"]),
                                AvgCLR = reader["AvgCLR"] == DBNull.Value? null: Convert.ToDecimal(reader["AvgCLR"]),
                                EntryCount = reader["EntryCount"] == DBNull.Value? 0: Convert.ToInt32(reader["EntryCount"]),
                                RejectionCount = reader["RejectionCount"] != DBNull.Value? Convert.ToInt32(reader["RejectionCount"]): 0,
                                TotalAmount = reader["TotalAmount"] == DBNull.Value? 0: Convert.ToDecimal(reader["TotalAmount"]),
                                IsCurrentShift = reader["IsCurrentShift"] != DBNull.Value && Convert.ToBoolean(reader["IsCurrentShift"]),
                                BatchDate = reader["BatchDate"] == DBNull.Value? DateTime.Today: Convert.ToDateTime(reader["BatchDate"])
                            });
                        }

                        //------------------------------------------------
                        // TODAY ENTRIES
                        //------------------------------------------------

                        reader.NextResult();

                        List<TodayMilkEntryModel> todayEntries =new List<TodayMilkEntryModel>();

                        while (reader.Read())
                        {
                            todayEntries.Add(new TodayMilkEntryModel
                            {
                                FarmerName = reader["FarmerName"].ToString(),
                                FarmerCode = reader["FarmerCode"].ToString(),
                                Shift = reader["Shift"].ToString(),
                                MilkType = reader["MilkType"]?.ToString(),
                                Quantity = reader["Quantity"] != DBNull.Value? Convert.ToDecimal(reader["Quantity"]): null,
                                Fat = reader["Fat"] != DBNull.Value? Convert.ToDecimal(reader["Fat"]): null,
                                CLR = reader["CLR"] != DBNull.Value? Convert.ToDecimal(reader["CLR"]): null,
                                Amount = reader["Amount"] != DBNull.Value? Convert.ToDecimal(reader["Amount"]): null,
                                Status = reader["Status"].ToString(),
                                RejectionReason =reader["RejectionReason"] != DBNull.Value? reader["RejectionReason"].ToString(): null,
                                EntryTime =Convert.ToDateTime(reader["EntryTime"])
                            });
                        }

                        //------------------------------------------------
                        // ASSIGN SAME TODAY ENTRIES
                        //------------------------------------------------

                        foreach (var batch in batches)
                        {
                            batch.TodayEntries = todayEntries;
                        }
                    }
                }
            }

            return batches;
        }

        public BatchDetailModel GetBatchDetail(int batchId)
        {
            BatchDetailModel detail = new BatchDetailModel();

            using (SqlConnection con = _dbHelper.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Collection.usp_GetBatchDetail", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 60;
                    cmd.Parameters.AddWithValue("@BatchId", batchId);

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        //--------------------------------------------
                        // RESULT SET 1: Batch Header + KPIs
                        //--------------------------------------------
                        if (reader.Read())
                        {
                            detail.BatchId = Convert.ToInt32(reader["BatchId"]);
                            detail.Shift = reader["Shift"]?.ToString();
                            detail.BatchDate = Convert.ToDateTime(reader["BatchDate"]);
                            detail.Status = reader["Status"]?.ToString();
                            detail.CenterName = reader["CenterName"]?.ToString();
                            detail.TotalQuantity = reader["TotalQuantity"] != DBNull.Value ? Convert.ToDecimal(reader["TotalQuantity"]) : 0;
                            detail.TotalAmount = reader["TotalAmount"] != DBNull.Value ? Convert.ToDecimal(reader["TotalAmount"]) : 0;
                            detail.AvgFat = reader["AvgFat"] != DBNull.Value ? Convert.ToDecimal(reader["AvgFat"]) : null;
                            detail.AvgCLR = reader["AvgCLR"] != DBNull.Value ? Convert.ToDecimal(reader["AvgCLR"]) : null;
                            detail.AcceptedCount = reader["AcceptedCount"] != DBNull.Value ? Convert.ToInt32(reader["AcceptedCount"]) : 0;
                            detail.RejectedCount = reader["RejectedCount"] != DBNull.Value ? Convert.ToInt32(reader["RejectedCount"]) : 0;
                        }

                        //--------------------------------------------
                        // RESULT SET 2: Entries
                        //--------------------------------------------
                        reader.NextResult();

                        while (reader.Read())
                        {
                            detail.Entries.Add(new BatchEntryModel
                            {
                                FarmerName = reader["FarmerName"]?.ToString(),
                                FarmerCode = reader["FarmerCode"]?.ToString(),
                                MilkType = reader["MilkType"]?.ToString(),
                                Quantity = reader["Quantity"] != DBNull.Value ? Convert.ToDecimal(reader["Quantity"]) : null,
                                Fat = reader["Fat"] != DBNull.Value ? Convert.ToDecimal(reader["Fat"]) : null,
                                CLR = reader["CLR"] != DBNull.Value ? Convert.ToDecimal(reader["CLR"]) : null,
                                Rate = reader["Rate"] != DBNull.Value ? Convert.ToDecimal(reader["Rate"]) : null,
                                Amount = reader["Amount"] != DBNull.Value ? Convert.ToDecimal(reader["Amount"]) : null,
                                Status = reader["Status"]?.ToString(),
                                RejectionReason = reader["RejectionReason"] != DBNull.Value ? reader["RejectionReason"]?.ToString() : null,
                                Remarks = reader["Remarks"] != DBNull.Value ? reader["Remarks"]?.ToString() : null
                            });
                        }
                    }
                }
            }

            return detail;
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
            SELECT CenterId
            FROM HR.Staffs
            WHERE StaffId = @StaffId
            ", con);

            cmd.Parameters.AddWithValue("@StaffId", staffId);

            con.Open();

            var result = cmd.ExecuteScalar();

            return result != null && result != DBNull.Value
                ? Convert.ToInt32(result)
                : 0;
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

        //All Milk Entries for center
        public List<AllMilkEntriesModel> GetAllEntries(int centerId)
        {
            List<AllMilkEntriesModel> list = new List<AllMilkEntriesModel>();

            using (SqlConnection con = _dbHelper.GetConnection())
            {
                string query = @"
                    SELECT
                        mc.CollectionId,
                        mc.CollectionDate,
                        f.FarmerName,
                        f.FarmerCode,
                        mc.Shift,
                        mt.MilkTypeName AS MilkType,
                        mc.Quantity,
                        mc.AppliedFat,
                        mc.AppliedCLR,
                        mc.RatePerLiter,
                        mc.Amount,
                        mc.BatchId,
                        'Accepted' AS Status,
                        NULL AS RejectionReason,
                        NULL AS Remarks
                    FROM Collection.MilkCollection mc
                    INNER JOIN Farmer.Farmers f ON mc.FarmerId = f.FarmerId
                    INNER JOIN Finance.MilkTypes mt ON mc.MilkTypeId = mt.MilkTypeId
                    WHERE mc.CenterId = @CenterId

                    UNION ALL

                    SELECT
                        0 AS CollectionId,
                        mr.RejectionDate AS CollectionDate,
                        f.FarmerName,
                        f.FarmerCode,
                        mr.Shift,
                        mt.MilkTypeName AS MilkType,
                        mr.Quantity,
                        mr.AppliedFat,
                        mr.AppliedCLR,
                        0 AS RatePerLiter,
                        0 AS Amount,
                        mr.BatchId,
                        'Rejected' AS Status,
                        mr.RejectionReason,
                        mr.Remarks
                    FROM Collection.MilkRejections mr
                    INNER JOIN Farmer.Farmers f ON mr.FarmerId = f.FarmerId
                    INNER JOIN Finance.MilkTypes mt ON mr.MilkTypeId = mt.MilkTypeId
                    WHERE mr.CenterId = @CenterId

                    ORDER BY CollectionDate DESC";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@CenterId", centerId);

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new AllMilkEntriesModel
                            {
                                CollectionId = Convert.ToInt32(reader["CollectionId"]),
                                CollectionDate = Convert.ToDateTime(reader["CollectionDate"]),
                                FarmerName = reader["FarmerName"].ToString(),
                                FarmerCode = reader["FarmerCode"].ToString(),
                                MilkType = reader["MilkType"]?.ToString(),
                                Shift = reader["Shift"].ToString(),
                                Quantity = reader["Quantity"] as decimal?,
                                AppliedFat = reader["AppliedFat"] as decimal?,
                                AppliedCLR = reader["AppliedCLR"] as decimal?,
                                RatePerLiter = reader["RatePerLiter"] as decimal?,
                                Amount = reader["Amount"] as decimal?,
                                BatchId = reader["BatchId"] != DBNull.Value? Convert.ToInt32(reader["BatchId"]): 0,
                                Status = reader["Status"].ToString(),
                                RejectionReason = reader["RejectionReason"]?.ToString(),
                                Remarks = reader["Remarks"]?.ToString()
                            });
                        }
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
        public FarmerMilkReceiptModel GetReceiptByCollectionId(int id)
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

            return new FarmerMilkReceiptModel
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
        // SP: Collection.usp_GetClosedBatchesForDispatch 
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

        public (decimal totalQty, decimal availableQty)GetMilkTypeBatchDetails(int batchId, int milkTypeId)
        {
            using var con = _dbHelper.GetConnection();

            using var cmd = new SqlCommand(
                "Collection.usp_GetMilkTypeBatchDetails",
                con);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@BatchId", batchId);
            cmd.Parameters.AddWithValue("@MilkTypeId", milkTypeId);

            con.Open();

            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                return (
                    Convert.ToDecimal(reader["TotalMilkTypeQty"]),
                    Convert.ToDecimal(reader["AvailableQty"])
                );
            }

            return (0, 0);
        }
        public int DispatchMilkTransfer(int batchId, int milkTypeId, int vehicleId, int plantId,decimal dispatchQty, DateTime dispatchDate)
        {
            using var con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand("Production.usp_Production_DispatchMilkTransferNew", con);
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

                   
                    MilkTypeName = reader["MilkTypeName"]?.ToString()
                });
            }

            return list;
        }

        public List<AllBatchsModel> GetAllBatchDetails(int staffId)
        {
            List<AllBatchsModel> list = new();

            using var con = _dbHelper.GetConnection();

            using SqlCommand cmd =
                new SqlCommand("Collection.usp_GetAllBatchDetails", con);

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@StaffId", staffId);

            con.Open();

            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                list.Add(new AllBatchsModel
                {
                    BatchId = Convert.ToInt32(dr["BatchId"]),

                    CenterName = dr["CenterName"].ToString(),

                    Shift = dr["Shift"].ToString(),

                    BatchDate = Convert.ToDateTime(dr["BatchDate"]),

                    TotalQuantity = dr["TotalQuantity"] == DBNull.Value
                        ? 0
                        : Convert.ToDecimal(dr["TotalQuantity"]),

                    AvgFat = dr["AvgFat"] == DBNull.Value
                        ? 0
                        : Convert.ToDecimal(dr["AvgFat"]),

                    AvgCLR = dr["AvgCLR"] == DBNull.Value
                        ? 0
                        : Convert.ToDecimal(dr["AvgCLR"]),

                    Status = dr["Status"].ToString(),

                    TotalFarmers = dr["TotalFarmers"] == DBNull.Value
                        ? 0
                        : Convert.ToInt32(dr["TotalFarmers"]),

                    TotalMilkCollected = dr["TotalMilkCollected"] == DBNull.Value
                        ? 0
                        : Convert.ToDecimal(dr["TotalMilkCollected"]),

                    TotalAmount = dr["TotalAmount"] == DBNull.Value
                        ? 0
                        : Convert.ToDecimal(dr["TotalAmount"])
                });
            }

            return list;
        }


      
    }
}
