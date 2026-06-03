using DairyIndustry.Data;
using DairyIndustry.Models.ChillingStorage;
using Microsoft.Data.SqlClient;

namespace DairyIndustry.Repositories
{
    public class ChillingCenterRepository : IChillingCenterRepository
    {
        private readonly DbHelper _db;

        public ChillingCenterRepository(DbHelper db)
        {
            _db = db;
        }


        // ═══════════════════════════════════════════════════════════
        //  GET PLANT BY USER ID — uses SP usp_GetPlantByUserId
        //  Queries Admin.UserPlants to find which plant the
        //  logged-in user belongs to. Returns null if not assigned.
        // ═══════════════════════════════════════════════════════════
        public PlantDropdownModel? GetPlantByUserId(int userId)
        {
            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand("Admin.usp_GetPlantByUserId", con);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@UserId", userId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new PlantDropdownModel
                {
                    PlantId = Convert.ToInt32(reader["PlantId"]),
                    PlantName = reader["PlantName"].ToString(),
                    Location = reader["Location"] == DBNull.Value
                                    ? null : reader["Location"].ToString()
                };
            }
            return null;
        }


        // ═══════════════════════════════════════════════════════════
        //  1. STORE ITEM — uses SP usp_Chilling_StoreItem
        //     Inserts a new chilling storage entry
        //     Returns new StorageId
        // ═══════════════════════════════════════════════════════════
        public int StoreItem(ChillingStoreItemModel model)
        {
            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand("Chilling.usp_Chilling_StoreItem", con);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@PlantId", model.PlantId);
            cmd.Parameters.AddWithValue("@ProductId", (object?)model.ProductId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@MilkQuantity", model.MilkQuantity);
            cmd.Parameters.AddWithValue("@Temperature", (object?)model.Temperature ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@StoredDate", model.StoredDate);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
                return Convert.ToInt32(reader["NewStorageId"]);

            return 0;
        }


        // ═══════════════════════════════════════════════════════════
        //  2. GET BY PLANT — uses SP usp_Chilling_GetStorageByPlant
        //     Returns all entries for a specific plant
        //     with optional date filter
        // ═══════════════════════════════════════════════════════════
        public List<ChillingStorageModel> GetByPlant(int plantId,
                                                      DateTime? fromDate,
                                                      DateTime? toDate)
        {
            var list = new List<ChillingStorageModel>();

            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand("Chilling.usp_Chilling_GetStorageByPlant", con);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@PlantId", plantId);
            cmd.Parameters.AddWithValue("@FromDate", (object?)fromDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ToDate", (object?)toDate ?? DBNull.Value);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var temp = reader["Temperature"] == DBNull.Value
                                 ? (decimal?)null : Convert.ToDecimal(reader["Temperature"]);
                list.Add(new ChillingStorageModel
                {
                    StorageId = Convert.ToInt32(reader["StorageId"]),
                    PlantId = plantId,
                    PlantName = reader["PlantName"].ToString(),
                    ProductId = null,
                    ItemName = reader["ItemName"].ToString(),
                    ProductType = reader["ProductType"] == DBNull.Value ? null : reader["ProductType"].ToString(),
                    Unit = reader["Unit"] == DBNull.Value ? null : reader["Unit"].ToString(),
                    MilkQuantity = Convert.ToDecimal(reader["MilkQuantity"]),
                    Temperature = temp,
                    StoredDate = Convert.ToDateTime(reader["StoredDate"]),
                    TempStatus = GetTempStatus(temp)
                });
            }
            return list;
        }


        // ═══════════════════════════════════════════════════════════
        //  3. GET ALL — inline query
        //     Returns all entries across all plants
        //     with optional date filter
        // ═══════════════════════════════════════════════════════════
        public List<ChillingStorageModel> GetAll(DateTime? fromDate, DateTime? toDate)
        {
            var list = new List<ChillingStorageModel>();

            string query = @"
                SELECT
                    cs.StorageId, cs.PlantId, cs.ProductId, cs.Shift,
                    cs.MilkQuantity, cs.Temperature, cs.StoredDate,
                    pp.PlantName,
                    ISNULL(p.ProductName, 'Raw Milk') AS ItemName,
                    p.ProductType, p.Unit
                FROM Chilling.ChillingStorage cs
                INNER JOIN Production.ProcessingPlants pp ON pp.PlantId  = cs.PlantId
                LEFT  JOIN Production.Products         p  ON p.ProductId = cs.ProductId
                WHERE (@FromDate IS NULL OR cs.StoredDate >= @FromDate)
                  AND (@ToDate   IS NULL OR cs.StoredDate <= @ToDate)
                ORDER BY cs.StoredDate DESC";

            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@FromDate", (object?)fromDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ToDate", (object?)toDate ?? DBNull.Value);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(MapStorageModel(reader));

            return list;
        }


        // ═══════════════════════════════════════════════════════════
        //  4. GET BY ID — inline query
        //     Returns single entry for Details / Edit pages
        // ═══════════════════════════════════════════════════════════
        public ChillingStorageModel? GetById(int storageId)
        {
            string query = @"
                SELECT
                    cs.StorageId, cs.PlantId, cs.ProductId, cs.Shift,
                    cs.MilkQuantity, cs.Temperature, cs.StoredDate,
                    pp.PlantName,
                    ISNULL(p.ProductName, 'Raw Milk') AS ItemName,
                    p.ProductType, p.Unit
                FROM Chilling.ChillingStorage cs
                INNER JOIN Production.ProcessingPlants pp ON pp.PlantId  = cs.PlantId
                LEFT  JOIN Production.Products         p  ON p.ProductId = cs.ProductId
                WHERE cs.StorageId = @StorageId";

            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@StorageId", storageId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
                return MapStorageModel(reader);

            return null;
        }


        // ═══════════════════════════════════════════════════════════
        //  5. UPDATE ENTRY — inline query
        //     Updates quantity and temperature of an existing entry
        // ═══════════════════════════════════════════════════════════
        public bool UpdateEntry(ChillingStoreItemModel model)
        {
            string query = @"
                UPDATE Chilling.ChillingStorage
                SET ProductId    = @ProductId,
                    MilkQuantity = @MilkQuantity,
                    Temperature  = @Temperature,
                    Shift        = @Shift
                WHERE StorageId  = @StorageId";

            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@StorageId", model.StorageId);
            cmd.Parameters.AddWithValue("@ProductId", (object?)model.ProductId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@MilkQuantity", model.MilkQuantity);
            cmd.Parameters.AddWithValue("@Temperature", (object?)model.Temperature ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Shift", (object?)model.Shift ?? DBNull.Value);

            return cmd.ExecuteNonQuery() > 0;
        }


        // ═══════════════════════════════════════════════════════════
        //  6. DELETE ENTRY — inline query
        // ═══════════════════════════════════════════════════════════
        public bool DeleteEntry(int storageId)
        {
            string query = "DELETE FROM Chilling.ChillingStorage WHERE StorageId = @StorageId";

            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@StorageId", storageId);

            return cmd.ExecuteNonQuery() > 0;
        }


        // ═══════════════════════════════════════════════════════════
        //  7. GET TEMPERATURE ALERTS — inline query
        //     Returns all entries where Temperature > 5°C
        // ═══════════════════════════════════════════════════════════
        public List<ChillingStorageModel> GetTemperatureAlerts(DateTime? fromDate,
                                                                DateTime? toDate)
        {
            var list = new List<ChillingStorageModel>();

            string query = @"
                SELECT
                    cs.StorageId, cs.PlantId, cs.ProductId, cs.Shift,
                    cs.MilkQuantity, cs.Temperature, cs.StoredDate,
                    pp.PlantName,
                    ISNULL(p.ProductName, 'Raw Milk') AS ItemName,
                    p.ProductType, p.Unit
                FROM Chilling.ChillingStorage cs
                INNER JOIN Production.ProcessingPlants pp ON pp.PlantId  = cs.PlantId
                LEFT  JOIN Production.Products         p  ON p.ProductId = cs.ProductId
                WHERE cs.Temperature > 5
                  AND (@FromDate IS NULL OR cs.StoredDate >= @FromDate)
                  AND (@ToDate   IS NULL OR cs.StoredDate <= @ToDate)
                ORDER BY cs.Temperature DESC";

            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@FromDate", (object?)fromDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ToDate", (object?)toDate ?? DBNull.Value);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(MapStorageModel(reader));

            return list;
        }


        // ═══════════════════════════════════════════════════════════
        //  8. DASHBOARD SUMMARY — inline query
        // ═══════════════════════════════════════════════════════════
        public ChillingDashboardSummaryModel GetDashboardSummary()
        {
            string query = @"
                SELECT
                    COUNT(*)                                                        AS TotalEntries,
                    ISNULL(SUM(MilkQuantity), 0)                                   AS TotalQuantity,
                    SUM(CASE WHEN Temperature > 8  THEN 1 ELSE 0 END)              AS CriticalAlerts,
                    SUM(CASE WHEN Temperature BETWEEN 5 AND 8 THEN 1 ELSE 0 END)   AS Warnings,
                    SUM(CASE WHEN Temperature BETWEEN 0 AND 5 THEN 1 ELSE 0 END)   AS SafeCount,
                    SUM(CASE WHEN Temperature IS NULL THEN 1 ELSE 0 END)            AS UnknownCount
                FROM Chilling.ChillingStorage
                WHERE StoredDate = CAST(GETDATE() AS DATE)";

            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(query, con);
            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                return new ChillingDashboardSummaryModel
                {
                    TotalEntries = reader["TotalEntries"] == DBNull.Value ? 0 : Convert.ToInt32(reader["TotalEntries"]),
                    TotalQuantity = reader["TotalQuantity"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["TotalQuantity"]),
                    CriticalAlerts = reader["CriticalAlerts"] == DBNull.Value ? 0 : Convert.ToInt32(reader["CriticalAlerts"]),
                    Warnings = reader["Warnings"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Warnings"]),
                    SafeCount = reader["SafeCount"] == DBNull.Value ? 0 : Convert.ToInt32(reader["SafeCount"]),
                    UnknownCount = reader["UnknownCount"] == DBNull.Value ? 0 : Convert.ToInt32(reader["UnknownCount"]),
                    ReportDate = DateTime.Today
                };
            }

            return new ChillingDashboardSummaryModel { ReportDate = DateTime.Today };
        }


        // ═══════════════════════════════════════════════════════════
        //  9. PLANT CAPACITY SUMMARY — inline query
        // ═══════════════════════════════════════════════════════════
        public List<ChillingPlantCapacityModel> GetPlantCapacitySummary()
        {
            var list = new List<ChillingPlantCapacityModel>();

            string query = @"
                SELECT
                    pp.PlantId,
                    pp.PlantName,
                    ISNULL(pp.Location, '')                                           AS Location,
                    ISNULL(SUM(cs.MilkQuantity), 0)                                   AS TotalStoredAllTime,
                    ISNULL(SUM(CASE WHEN cs.StoredDate = CAST(GETDATE() AS DATE)
                                    THEN cs.MilkQuantity ELSE 0 END), 0)              AS StoredToday,
                    COUNT(cs.StorageId)                                                AS TotalEntries,
                    SUM(CASE WHEN cs.Temperature > 5 THEN 1 ELSE 0 END)               AS AlertCount
                FROM Production.ProcessingPlants pp
                LEFT JOIN Chilling.ChillingStorage cs ON cs.PlantId = pp.PlantId
                GROUP BY pp.PlantId, pp.PlantName, pp.Location
                ORDER BY pp.PlantName";

            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(query, con);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new ChillingPlantCapacityModel
                {
                    PlantId = Convert.ToInt32(reader["PlantId"]),
                    PlantName = reader["PlantName"].ToString(),
                    Location = reader["Location"].ToString(),
                    TotalStoredAllTime = Convert.ToDecimal(reader["TotalStoredAllTime"]),
                    StoredToday = Convert.ToDecimal(reader["StoredToday"]),
                    TotalEntries = Convert.ToInt32(reader["TotalEntries"]),
                    AlertCount = reader["AlertCount"] == DBNull.Value ? 0 : Convert.ToInt32(reader["AlertCount"])
                });
            }
            return list;
        }


        // ═══════════════════════════════════════════════════════════
        //  10. GET PLANTS — inline query for dropdowns
        // ═══════════════════════════════════════════════════════════
        public List<PlantDropdownModel> GetPlants()
        {
            var list = new List<PlantDropdownModel>();

            string query = "SELECT PlantId, PlantName, Location FROM Production.ProcessingPlants ORDER BY PlantName";

            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(query, con);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new PlantDropdownModel
                {
                    PlantId = Convert.ToInt32(reader["PlantId"]),
                    PlantName = reader["PlantName"].ToString(),
                    Location = reader["Location"] == DBNull.Value ? null : reader["Location"].ToString()
                });
            }
            return list;
        }


        // ═══════════════════════════════════════════════════════════
        //  11. GET PRODUCTS — inline query for dropdowns
        // ═══════════════════════════════════════════════════════════
        public List<ProductDropdownModel> GetProducts()
        {
            var list = new List<ProductDropdownModel>();

            string query = @"
                SELECT ProductId, ProductName, ProductType, Unit
                FROM Production.Products
                ORDER BY ProductName";

            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(query, con);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new ProductDropdownModel
                {
                    ProductId = Convert.ToInt32(reader["ProductId"]),
                    ProductName = reader["ProductName"].ToString(),
                    ProductType = reader["ProductType"].ToString(),
                    Unit = reader["Unit"].ToString()
                });
            }
            return list;
        }


        // ═══════════════════════════════════════════════════════════
        //  12. NEW — GET BY PLANT FILTERED — inline query
        //      Same as GetByPlant SP but supports item name search.
        //      Kept separate so the existing SP call is never touched.
        // ═══════════════════════════════════════════════════════════
        public List<ChillingStorageModel> GetByPlantFiltered(int plantId,
                                                              DateTime? fromDate,
                                                              DateTime? toDate,
                                                              string? search)
        {
            var list = new List<ChillingStorageModel>();

            string query = @"
                SELECT
                    cs.StorageId, cs.PlantId, cs.ProductId, cs.Shift,
                    cs.MilkQuantity, cs.Temperature, cs.StoredDate,
                    pp.PlantName,
                    ISNULL(p.ProductName, 'Raw Milk') AS ItemName,
                    p.ProductType, p.Unit
                FROM Chilling.ChillingStorage cs
                INNER JOIN Production.ProcessingPlants pp ON pp.PlantId  = cs.PlantId
                LEFT  JOIN Production.Products         p  ON p.ProductId = cs.ProductId
                WHERE cs.PlantId = @PlantId
                  AND (@FromDate IS NULL OR cs.StoredDate >= @FromDate)
                  AND (@ToDate   IS NULL OR cs.StoredDate <= @ToDate)
                  AND (@Search   IS NULL OR ISNULL(p.ProductName, 'Raw Milk') LIKE '%' + @Search + '%')
                ORDER BY cs.StoredDate DESC";

            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@PlantId", plantId);
            cmd.Parameters.AddWithValue("@FromDate", (object?)fromDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ToDate", (object?)toDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Search", (object?)search ?? DBNull.Value);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(MapStorageModel(reader));

            return list;
        }


        // ═══════════════════════════════════════════════════════════
        //  13. NEW — GET ALL FILTERED — inline query
        //      Same as GetAll but supports item name search.
        // ═══════════════════════════════════════════════════════════
        public List<ChillingStorageModel> GetAllFiltered(DateTime? fromDate,
                                                          DateTime? toDate,
                                                          string? search)
        {
            var list = new List<ChillingStorageModel>();

            string query = @"
                SELECT
                    cs.StorageId, cs.PlantId, cs.ProductId, cs.Shift,
                    cs.MilkQuantity, cs.Temperature, cs.StoredDate,
                    pp.PlantName,
                    ISNULL(p.ProductName, 'Raw Milk') AS ItemName,
                    p.ProductType, p.Unit
                FROM Chilling.ChillingStorage cs
                INNER JOIN Production.ProcessingPlants pp ON pp.PlantId  = cs.PlantId
                LEFT  JOIN Production.Products         p  ON p.ProductId = cs.ProductId
                WHERE (@FromDate IS NULL OR cs.StoredDate >= @FromDate)
                  AND (@ToDate   IS NULL OR cs.StoredDate <= @ToDate)
                  AND (@Search   IS NULL OR ISNULL(p.ProductName, 'Raw Milk') LIKE '%' + @Search + '%')
                ORDER BY cs.StoredDate DESC";

            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@FromDate", (object?)fromDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ToDate", (object?)toDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Search", (object?)search ?? DBNull.Value);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(MapStorageModel(reader));

            return list;
        }


        // ═══════════════════════════════════════════════════════════
        //  14. NEW — GET DAILY REPORT — inline query
        //      Groups entries by StoredDate for the Report page.
        //      Filters by PlantId if provided (plant manager view).
        // ═══════════════════════════════════════════════════════════
        public List<ChillingDailyReportModel> GetDailyReport(int? plantId,
                                                               DateTime? fromDate,
                                                               DateTime? toDate)
        {
            var list = new List<ChillingDailyReportModel>();

            string query = @"
                SELECT
                    cs.StoredDate,
                    COUNT(*)                                                              AS TotalEntries,
                    ISNULL(SUM(cs.MilkQuantity), 0)                                      AS TotalQuantity,
                    SUM(CASE WHEN cs.Temperature <= 5              THEN 1 ELSE 0 END)    AS SafeCount,
                    SUM(CASE WHEN cs.Temperature BETWEEN 5 AND 8   THEN 1 ELSE 0 END)    AS WarningCount,
                    SUM(CASE WHEN cs.Temperature > 8               THEN 1 ELSE 0 END)    AS CriticalCount,
                    SUM(CASE WHEN cs.Temperature IS NULL            THEN 1 ELSE 0 END)    AS UnknownCount
                FROM Chilling.ChillingStorage cs
                WHERE (@PlantId  IS NULL OR cs.PlantId    = @PlantId)
                  AND (@FromDate IS NULL OR cs.StoredDate >= @FromDate)
                  AND (@ToDate   IS NULL OR cs.StoredDate <= @ToDate)
                GROUP BY cs.StoredDate
                ORDER BY cs.StoredDate DESC";

            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@PlantId", (object?)plantId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FromDate", (object?)fromDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ToDate", (object?)toDate ?? DBNull.Value);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new ChillingDailyReportModel
                {
                    StoredDate = Convert.ToDateTime(reader["StoredDate"]),
                    TotalEntries = Convert.ToInt32(reader["TotalEntries"]),
                    TotalQuantity = Convert.ToDecimal(reader["TotalQuantity"]),
                    SafeCount = Convert.ToInt32(reader["SafeCount"]),
                    WarningCount = Convert.ToInt32(reader["WarningCount"]),
                    CriticalCount = Convert.ToInt32(reader["CriticalCount"]),
                    UnknownCount = Convert.ToInt32(reader["UnknownCount"])
                });
            }
            return list;
        }


        // ═══════════════════════════════════════════════════════════
        //  NEW — GET PRODUCT REPORT (Feature 2)
        //  Groups entries by ItemName for the By Product tab.
        //  Returns one row per distinct product/raw milk.
        // ═══════════════════════════════════════════════════════════
        public List<ChillingProductReportModel> GetProductReport(int? plantId,
                                                                   DateTime? fromDate,
                                                                   DateTime? toDate)
        {
            var list = new List<ChillingProductReportModel>();

            string query = @"
                SELECT
                    ISNULL(p.ProductName, 'Raw Milk')                             AS ItemName,
                    p.ProductType,
                    p.Unit,
                    COUNT(*)                                                       AS TotalEntries,
                    ISNULL(SUM(cs.MilkQuantity), 0)                               AS TotalQuantity,
                    ISNULL(AVG(CAST(cs.Temperature AS FLOAT)), 0)                  AS AvgTemperature,
                    SUM(CASE WHEN cs.Temperature > 5 THEN 1 ELSE 0 END)           AS AlertCount,
                    ISNULL(SUM(cs.MilkQuantity) / NULLIF(COUNT(*), 0), 0)         AS AvgQuantityPerEntry
                FROM Chilling.ChillingStorage cs
                INNER JOIN Production.ProcessingPlants pp ON pp.PlantId  = cs.PlantId
                LEFT  JOIN Production.Products         p  ON p.ProductId = cs.ProductId
                WHERE (@PlantId  IS NULL OR cs.PlantId    = @PlantId)
                  AND (@FromDate IS NULL OR cs.StoredDate >= @FromDate)
                  AND (@ToDate   IS NULL OR cs.StoredDate <= @ToDate)
                GROUP BY p.ProductName, p.ProductType, p.Unit
                ORDER BY TotalQuantity DESC";

            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@PlantId", (object?)plantId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FromDate", (object?)fromDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ToDate", (object?)toDate ?? DBNull.Value);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new ChillingProductReportModel
                {
                    ItemName = reader["ItemName"].ToString(),
                    ProductType = reader["ProductType"] == DBNull.Value ? null : reader["ProductType"].ToString(),
                    Unit = reader["Unit"] == DBNull.Value ? null : reader["Unit"].ToString(),
                    TotalEntries = Convert.ToInt32(reader["TotalEntries"]),
                    TotalQuantity = Convert.ToDecimal(reader["TotalQuantity"]),
                    AvgTemperature = Convert.ToDecimal(reader["AvgTemperature"]),
                    AlertCount = Convert.ToInt32(reader["AlertCount"]),
                    AvgQuantityPerEntry = Convert.ToDecimal(reader["AvgQuantityPerEntry"])
                });
            }
            return list;
        }


        // ═══════════════════════════════════════════════════════════
        //  NEW — GET WEEKLY TREND (Feature 3)
        //  Returns last 7 days of daily quantity per plant.
        //  Fills missing days with 0 so sparklines always have 7 points.
        // ═══════════════════════════════════════════════════════════
        public List<ChillingWeeklyTrendModel> GetWeeklyTrend(int? plantId)
        {
            var result = new List<ChillingWeeklyTrendModel>();

            // Build the 7-day date spine
            var dates = Enumerable.Range(0, 7)
                .Select(i => DateTime.Today.AddDays(-6 + i))
                .ToList();

            string query = @"
                SELECT
                    cs.PlantId,
                    pp.PlantName,
                    cs.StoredDate,
                    ISNULL(SUM(cs.MilkQuantity), 0)  AS DayQty,
                    COUNT(*)                          AS DayEntries
                FROM Chilling.ChillingStorage cs
                INNER JOIN Production.ProcessingPlants pp ON pp.PlantId = cs.PlantId
                WHERE cs.StoredDate >= @FromDate
                  AND cs.StoredDate <= @ToDate
                  AND (@PlantId IS NULL OR cs.PlantId = @PlantId)
                GROUP BY cs.PlantId, pp.PlantName, cs.StoredDate
                ORDER BY cs.PlantId, cs.StoredDate";

            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@FromDate", DateTime.Today.AddDays(-6));
            cmd.Parameters.AddWithValue("@ToDate", DateTime.Today);
            cmd.Parameters.AddWithValue("@PlantId", (object?)plantId ?? DBNull.Value);

            // Read raw rows into a lookup
            var raw = new Dictionary<int, (string Name, List<(DateTime Date, decimal Qty, int Cnt)> Rows)>();

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                int pid = Convert.ToInt32(reader["PlantId"]);
                string pn = reader["PlantName"].ToString();
                var dt = Convert.ToDateTime(reader["StoredDate"]);
                var qty = Convert.ToDecimal(reader["DayQty"]);
                var cnt = Convert.ToInt32(reader["DayEntries"]);

                if (!raw.ContainsKey(pid))
                    raw[pid] = (pn, new List<(DateTime, decimal, int)>());
                raw[pid].Rows.Add((dt, qty, cnt));
            }

            // Build WeeklyTrendModel — fill all 7 days including zeros
            foreach (var kvp in raw)
            {
                var trend = new ChillingWeeklyTrendModel
                {
                    PlantId = kvp.Key,
                    PlantName = kvp.Value.Name
                };

                foreach (var date in dates)
                {
                    var match = kvp.Value.Rows.FirstOrDefault(r => r.Date.Date == date.Date);
                    trend.Days.Add(new ChillingDayPointModel
                    {
                        Date = date,
                        Quantity = match.Date == default ? 0 : match.Qty,
                        EntryCount = match.Date == default ? 0 : match.Cnt
                    });
                }

                trend.TotalWeekQty = trend.Days.Sum(d => d.Quantity);
                trend.TotalWeekEntries = trend.Days.Sum(d => d.EntryCount);
                result.Add(trend);
            }

            return result;
        }


        // ═══════════════════════════════════════════════════════════
        //  PRIVATE HELPERS
        // ═══════════════════════════════════════════════════════════
        //  NEW — INSERT WITH SHIFT — inline query
        //  Called instead of StoreItem SP when Shift is selected.
        //  Identical to SP behaviour + writes Shift column.
        //  SP is left completely untouched.
        // ═══════════════════════════════════════════════════════════
        public int InsertWithShift(ChillingStoreItemModel model)
        {
            string query = @"
                INSERT INTO Chilling.ChillingStorage
                    (PlantId, ProductId, MilkQuantity, Temperature, StoredDate, Shift)
                VALUES
                    (@PlantId, @ProductId, @MilkQuantity, @Temperature,
                     ISNULL(@StoredDate, CAST(GETDATE() AS DATE)), @Shift);
                SELECT SCOPE_IDENTITY() AS NewStorageId;";

            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@PlantId", model.PlantId);
            cmd.Parameters.AddWithValue("@ProductId", (object?)model.ProductId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@MilkQuantity", model.MilkQuantity);
            cmd.Parameters.AddWithValue("@Temperature", (object?)model.Temperature ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@StoredDate", model.StoredDate);
            cmd.Parameters.AddWithValue("@Shift", (object?)model.Shift ?? DBNull.Value);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
                return Convert.ToInt32(reader["NewStorageId"]);
            return 0;
        }


        // ═══════════════════════════════════════════════════════════
        //  PRIVATE HELPERS
        // ═══════════════════════════════════════════════════════════

        private ChillingStorageModel MapStorageModel(SqlDataReader reader)
        {
            var temp = reader["Temperature"] == DBNull.Value
                           ? (decimal?)null : Convert.ToDecimal(reader["Temperature"]);
            return new ChillingStorageModel
            {
                StorageId = Convert.ToInt32(reader["StorageId"]),
                PlantId = Convert.ToInt32(reader["PlantId"]),
                PlantName = reader["PlantName"].ToString(),
                ProductId = reader["ProductId"] == DBNull.Value ? null : Convert.ToInt32(reader["ProductId"]),
                ItemName = reader["ItemName"].ToString(),
                ProductType = reader["ProductType"] == DBNull.Value ? null : reader["ProductType"].ToString(),
                Unit = reader["Unit"] == DBNull.Value ? null : reader["Unit"].ToString(),
                MilkQuantity = Convert.ToDecimal(reader["MilkQuantity"]),
                Temperature = temp,
                StoredDate = Convert.ToDateTime(reader["StoredDate"]),
                Shift = HasColumn(reader, "Shift") && reader["Shift"] != DBNull.Value
                                   ? reader["Shift"].ToString() : null,
                TempStatus = GetTempStatus(temp)
            };
        }

        private string GetTempStatus(decimal? temp)
        {
            if (temp == null) return "Unknown";
            if (temp <= 5) return "Safe";
            if (temp <= 8) return "Warning";
            return "Critical";
        }

        // Safe column existence check — prevents InvalidOperationException
        // when SP results don't include all columns (e.g. Shift not in GetByPlant SP)
        private bool HasColumn(SqlDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
                if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }
    }
}