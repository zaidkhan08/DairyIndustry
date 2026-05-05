using DairyIndustry.Data;
using DairyIndustry.Models.Reports;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DairyIndustry.Repositories
{
    public class ReportRepository : IReportRepository
    {
        private readonly DbHelper _db;

        public ReportRepository(DbHelper db)
        {
            _db = db;
        }

        // ── kept for backwards compat ────────────────────────────────

        public List<DailySummaryModel> GetDailySummaryByCenter(int? centerId, DateTime? fromDate, DateTime? toDate)
        {
            var list = new List<DailySummaryModel>();
            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand("Reports.usp_Report_DailySummaryByCenter", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@CenterId", (object?)centerId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@FromDate", (object?)fromDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ToDate", (object?)toDate ?? DBNull.Value);
                con.Open();
                using var dr = cmd.ExecuteReader();
                while (dr.Read())
                    list.Add(new DailySummaryModel
                    {
                        CenterId = Convert.ToInt32(dr["CenterId"]),
                        CenterName = dr["CenterName"].ToString(),
                        CollectionDate = Convert.ToDateTime(dr["CollectionDate"]),
                        Shift = dr["Shift"].ToString(),
                        MilkTypeName = dr["MilkTypeName"].ToString(),
                        TotalEntries = Convert.ToInt32(dr["TotalEntries"]),
                        TotalQuantityLtr = Convert.ToDecimal(dr["TotalQuantityLtr"]),
                        AvgFat = Convert.ToDecimal(dr["AvgFat"]),
                        AvgCLR = Convert.ToDecimal(dr["AvgCLR"]),
                        TotalAmount = Convert.ToDecimal(dr["TotalAmount"])
                    });
            }
            return list;
        }

        public List<FarmerCollectionModel> GetCollectionByFarmer(int? centerId, DateTime? fromDate, DateTime? toDate)
        {
            var list = new List<FarmerCollectionModel>();
            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand("Reports.usp_Report_MilkCollectionByFarmer", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@CenterId", (object?)centerId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@FromDate", (object?)fromDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ToDate", (object?)toDate ?? DBNull.Value);
                con.Open();
                using var dr = cmd.ExecuteReader();
                while (dr.Read())
                    list.Add(new FarmerCollectionModel
                    {
                        FarmerId = Convert.ToInt32(dr["FarmerId"]),
                        FarmerName = dr["FarmerName"].ToString(),
                        CenterName = dr["CenterName"].ToString(),
                        MilkTypeName = dr["MilkTypeName"].ToString(),
                        TotalEntries = Convert.ToInt32(dr["TotalEntries"]),
                        TotalQtyLtr = Convert.ToDecimal(dr["TotalQtyLtr"]),
                        AvgFat = Convert.ToDecimal(dr["AvgFat"]),
                        AvgCLR = Convert.ToDecimal(dr["AvgCLR"]),
                        TotalPayable = Convert.ToDecimal(dr["TotalPayable"])
                    });
            }
            return list;
        }

        // ── admin-facing reports ─────────────────────────────────────

        public List<DashboardMetricModel> GetDashboardOverview()
        {
            var list = new List<DashboardMetricModel>();
            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand("Reports.usp_Report_Dashboard_Overview", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                con.Open();
                using var dr = cmd.ExecuteReader();
                while (dr.Read())
                    list.Add(new DashboardMetricModel
                    {
                        MetricName = dr["MetricName"].ToString(),
                        Value = Convert.ToDecimal(dr["Value"]),
                        Unit = dr["Unit"].ToString()
                    });
            }
            return list;
        }

        public List<SalesReportModel> GetSalesReport(int? distributorId, DateTime? fromDate, DateTime? toDate)
        {
            var list = new List<SalesReportModel>();
            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand("Reports.usp_Report_SalesReport", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@DistributorId", (object?)distributorId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@FromDate", (object?)fromDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ToDate", (object?)toDate ?? DBNull.Value);
                con.Open();
                using var dr = cmd.ExecuteReader();
                while (dr.Read())
                    list.Add(new SalesReportModel
                    {
                        DistributorName = dr["DistributorName"].ToString(),
                        OrderDate = Convert.ToDateTime(dr["OrderDate"]),
                        OrderStatus = dr["OrderStatus"].ToString(),
                        ProductName = dr["ProductName"].ToString(),
                        ProductType = dr["ProductType"].ToString(),
                        Quantity = Convert.ToDecimal(dr["Quantity"]),
                        UnitPrice = Convert.ToDecimal(dr["UnitPrice"]),
                        LineTotal = Convert.ToDecimal(dr["LineTotal"])
                    });
            }
            return list;
        }

        public List<ProductionSummaryModel> GetPlantProductionSummary(int? plantId, DateTime? fromDate, DateTime? toDate)
        {
            var list = new List<ProductionSummaryModel>();
            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand("Reports.usp_Report_PlantProductionSummary", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@PlantId", (object?)plantId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@FromDate", (object?)fromDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ToDate", (object?)toDate ?? DBNull.Value);
                con.Open();
                using var dr = cmd.ExecuteReader();
                while (dr.Read())
                    list.Add(new ProductionSummaryModel
                    {
                        PlantName = dr["PlantName"].ToString(),
                        ProductName = dr["ProductName"].ToString(),
                        ProductType = dr["ProductType"].ToString(),
                        TotalBatches = Convert.ToInt32(dr["TotalBatches"]),
                        TotalMilkUsedLtr = Convert.ToDecimal(dr["TotalMilkUsedLtr"]),
                        BatchStatus = dr["BatchStatus"].ToString()
                    });
            }
            return list;
        }

        public List<WastageModel> GetWastageSummary(int? plantId, DateTime? fromDate, DateTime? toDate)
        {
            var list = new List<WastageModel>();
            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand("Reports.usp_Report_WastageSummary", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@PlantId", (object?)plantId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@FromDate", (object?)fromDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ToDate", (object?)toDate ?? DBNull.Value);
                con.Open();
                using var dr = cmd.ExecuteReader();
                while (dr.Read())
                    list.Add(new WastageModel
                    {
                        WastageCategory = dr["WastageCategory"].ToString(),
                        PlantName = dr["PlantName"].ToString(),
                        Item = dr["Item"].ToString(),
                        TotalWastage = Convert.ToDecimal(dr["TotalWastage"]),
                        Unit = dr["Unit"].ToString(),
                        WastageType = dr["WastageType"] == DBNull.Value ? null : dr["WastageType"].ToString()
                    });
            }
            return list;
        }

        public List<MilkTransferReportModel> GetMilkTransfers(int? plantId, DateTime? fromDate, DateTime? toDate)
        {
            var list = new List<MilkTransferReportModel>();
            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand("Production.usp_Production_GetMilkTransfers", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@PlantId", (object?)plantId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CenterId", DBNull.Value);
                cmd.Parameters.AddWithValue("@FromDate", (object?)fromDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ToDate", (object?)toDate ?? DBNull.Value);
                con.Open();
                using var dr = cmd.ExecuteReader();
                while (dr.Read())
                    list.Add(new MilkTransferReportModel
                    {
                        TransferId = Convert.ToInt32(dr["TransferId"]),
                        DispatchDate = Convert.ToDateTime(dr["DispatchDate"]),
                        ReceivedDate = dr["ReceivedDate"] == DBNull.Value ? null : Convert.ToDateTime(dr["ReceivedDate"]),
                        DispatchQty = Convert.ToDecimal(dr["DispatchQty"]),
                        ReceivedQty = dr["ReceivedQty"] == DBNull.Value ? null : Convert.ToDecimal(dr["ReceivedQty"]),
                        LossQty = dr["LossQty"] == DBNull.Value ? null : Convert.ToDecimal(dr["LossQty"]),
                        LossPercent = dr["LossPercent"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["LossPercent"]),
                        TransferStatus = dr["TransferStatus"].ToString(),
                        CenterName = dr["CenterName"].ToString(),
                        PlantName = dr["PlantName"].ToString(),
                        VehicleNumber = dr["VehicleNumber"].ToString(),
                        DriverName = dr["DriverName"].ToString(),
                        TestedFat = dr["TestedFat"] == DBNull.Value ? null : Convert.ToDecimal(dr["TestedFat"]),
                        TestedCLR = dr["TestedCLR"] == DBNull.Value ? null : Convert.ToDecimal(dr["TestedCLR"])
                    });
            }
            return list;
        }

        public List<StaffPaymentReportModel> GetStaffPayments(DateTime? fromDate, DateTime? toDate)
        {
            var list = new List<StaffPaymentReportModel>();
            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(@"
                SELECT
                    sp.PaymentId, s.FirstName, s.LastName,
                    r.RoleName, pp.PlantName,
                    sp.FromDate, sp.ToDate,
                    sp.TotalAmount, sp.PaymentDate, sp.PaymentStatus
                FROM Finance.StaffPayments sp
                INNER JOIN HR.Staffs                  s  ON s.StaffId  = sp.StaffId
                INNER JOIN Admin.Roles                r  ON r.RoleId   = s.RoleId
                INNER JOIN Production.ProcessingPlants pp ON pp.PlantId = sp.PlantId
                WHERE (@FromDate IS NULL OR sp.PaymentDate >= @FromDate)
                  AND (@ToDate   IS NULL OR sp.PaymentDate <= @ToDate)
                ORDER BY sp.PaymentDate DESC", con))
            {
                cmd.Parameters.AddWithValue("@FromDate", (object?)fromDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ToDate", (object?)toDate ?? DBNull.Value);
                con.Open();
                using var dr = cmd.ExecuteReader();
                while (dr.Read())
                    list.Add(new StaffPaymentReportModel
                    {
                        PaymentId = Convert.ToInt32(dr["PaymentId"]),
                        FirstName = dr["FirstName"].ToString(),
                        LastName = dr["LastName"].ToString(),
                        RoleName = dr["RoleName"].ToString(),
                        PlantName = dr["PlantName"].ToString(),
                        FromDate = Convert.ToDateTime(dr["FromDate"]),
                        ToDate = Convert.ToDateTime(dr["ToDate"]),
                        TotalAmount = Convert.ToDecimal(dr["TotalAmount"]),
                        PaymentDate = Convert.ToDateTime(dr["PaymentDate"]),
                        PaymentStatus = dr["PaymentStatus"].ToString()
                    });
            }
            return list;
        }

        public List<CenterPaymentReportModel> GetCenterPayments(DateTime? fromDate, DateTime? toDate)
        {
            var list = new List<CenterPaymentReportModel>();
            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(@"
                SELECT
                    cp.CenterPaymentId, cc.CenterName, pp.PlantName,
                    cp.ReceivedQty, cp.RatePerLiter,
                    cp.TestedFat, cp.TestedCLR,
                    cp.TotalAmount, cp.PaymentDate, cp.PaymentStatus
                FROM Finance.CenterPayments cp
                INNER JOIN Collection.CollectionCenters  cc ON cc.CenterId = cp.CenterId
                INNER JOIN Production.ProcessingPlants   pp ON pp.PlantId  = cp.PlantId
                WHERE (@FromDate IS NULL OR cp.PaymentDate >= @FromDate)
                  AND (@ToDate   IS NULL OR cp.PaymentDate <= @ToDate)
                ORDER BY cp.PaymentDate DESC", con))
            {
                cmd.Parameters.AddWithValue("@FromDate", (object?)fromDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ToDate", (object?)toDate ?? DBNull.Value);
                con.Open();
                using var dr = cmd.ExecuteReader();
                while (dr.Read())
                    list.Add(new CenterPaymentReportModel
                    {
                        CenterPaymentId = Convert.ToInt32(dr["CenterPaymentId"]),
                        CenterName = dr["CenterName"].ToString(),
                        PlantName = dr["PlantName"].ToString(),
                        ReceivedQty = Convert.ToDecimal(dr["ReceivedQty"]),
                        RatePerLiter = Convert.ToDecimal(dr["RatePerLiter"]),
                        TestedFat = dr["TestedFat"] == DBNull.Value ? null : Convert.ToDecimal(dr["TestedFat"]),
                        TestedCLR = dr["TestedCLR"] == DBNull.Value ? null : Convert.ToDecimal(dr["TestedCLR"]),
                        TotalAmount = Convert.ToDecimal(dr["TotalAmount"]),
                        PaymentDate = Convert.ToDateTime(dr["PaymentDate"]),
                        PaymentStatus = dr["PaymentStatus"].ToString()
                    });
            }
            return list;
        }

        public List<PendingPaymentModel> GetPendingPayments()
        {
            var list = new List<PendingPaymentModel>();
            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand("Finance.usp_Finance_GetPendingPayments", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                con.Open();
                using var dr = cmd.ExecuteReader();
                while (dr.Read())
                    list.Add(new PendingPaymentModel
                    {
                        PaymentType = dr["PaymentType"].ToString(),
                        PaymentId = Convert.ToInt32(dr["PaymentId"]),
                        Payee = dr["Payee"].ToString(),
                        TotalAmount = Convert.ToDecimal(dr["TotalAmount"]),
                        PaymentDate = Convert.ToDateTime(dr["PaymentDate"]),
                        PaymentStatus = dr["PaymentStatus"].ToString()
                    });
            }
            return list;
        }
    }
}