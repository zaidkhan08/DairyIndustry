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
        //  GET PLANT BY USER ID
        // ═══════════════════════════════════════════════════════════
        public PlantDropdownModel? GetPlantByUserId(int userId)
        {
            try
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChillingRepo] GetPlantByUserId: {ex.Message}");
                return null;
            }
        }


        // ═══════════════════════════════════════════════════════════
        //  1. STORE ITEM — SP usp_Chilling_StoreItem
        // ═══════════════════════════════════════════════════════════
        public int StoreItem(ChillingStoreItemModel model)
        {
            try
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
                if (reader.Read()) return Convert.ToInt32(reader["NewStorageId"]);
                return 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChillingRepo] StoreItem: {ex.Message}");
                return 0;
            }
        }


        // ═══════════════════════════════════════════════════════════
        //  2. GET BY PLANT — SP usp_Chilling_GetStorageByPlant
        // ═══════════════════════════════════════════════════════════
        public List<ChillingStorageModel> GetByPlant(int plantId,
                                                      DateTime? fromDate,
                                                      DateTime? toDate)
        {
            try
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChillingRepo] GetByPlant: {ex.Message}");
                return new List<ChillingStorageModel>();
            }
        }


        // ═══════════════════════════════════════════════════════════
        //  3. GET ALL — inline query
        // ═══════════════════════════════════════════════════════════
        public List<ChillingStorageModel> GetAll(DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                var list = new List<ChillingStorageModel>();
                string query = @"
                    SELECT cs.StorageId, cs.PlantId, cs.ProductId, cs.Shift,
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
                while (reader.Read()) list.Add(MapStorageModel(reader));
                return list;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChillingRepo] GetAll: {ex.Message}");
                return new List<ChillingStorageModel>();
            }
        }


        // ═══════════════════════════════════════════════════════════
        //  4. GET BY ID — inline query
        // ═══════════════════════════════════════════════════════════
        public ChillingStorageModel? GetById(int storageId)
        {
            try
            {
                string query = @"
                    SELECT cs.StorageId, cs.PlantId, cs.ProductId, cs.Shift,
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
                if (reader.Read()) return MapStorageModel(reader);
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChillingRepo] GetById: {ex.Message}");
                return null;
            }
        }


        // ═══════════════════════════════════════════════════════════
        //  5. UPDATE ENTRY — inline query
        // ═══════════════════════════════════════════════════════════
        public bool UpdateEntry(ChillingStoreItemModel model)
        {
            try
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChillingRepo] UpdateEntry: {ex.Message}");
                return false;
            }
        }


        // ═══════════════════════════════════════════════════════════
        //  6. DELETE ENTRY — inline query
        // ═══════════════════════════════════════════════════════════
        public bool DeleteEntry(int storageId)
        {
            try
            {
                string query = "DELETE FROM Chilling.ChillingStorage WHERE StorageId = @StorageId";
                using var con = _db.GetConnection();
                con.Open();
                using var cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@StorageId", storageId);
                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChillingRepo] DeleteEntry: {ex.Message}");
                return false;
            }
        }


        // ═══════════════════════════════════════════════════════════
        //  7. GET TEMPERATURE ALERTS — inline query
        //  Fix #2 — plantId pushed to SQL
        // ═══════════════════════════════════════════════════════════
        public List<ChillingStorageModel> GetTemperatureAlerts(int? plantId,
                                                                DateTime? fromDate,
                                                                DateTime? toDate)
        {
            try
            {
                var list = new List<ChillingStorageModel>();
                string query = @"
                    SELECT cs.StorageId, cs.PlantId, cs.ProductId, cs.Shift,
                           cs.MilkQuantity, cs.Temperature, cs.StoredDate,
                           pp.PlantName,
                           ISNULL(p.ProductName, 'Raw Milk') AS ItemName,
                           p.ProductType, p.Unit
                    FROM Chilling.ChillingStorage cs
                    INNER JOIN Production.ProcessingPlants pp ON pp.PlantId  = cs.PlantId
                    LEFT  JOIN Production.Products         p  ON p.ProductId = cs.ProductId
                    WHERE cs.Temperature > 5
                      AND (@PlantId  IS NULL OR cs.PlantId    = @PlantId)
                      AND (@FromDate IS NULL OR cs.StoredDate >= @FromDate)
                      AND (@ToDate   IS NULL OR cs.StoredDate <= @ToDate)
                    ORDER BY cs.Temperature DESC";

                using var con = _db.GetConnection();
                con.Open();
                using var cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@PlantId", (object?)plantId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@FromDate", (object?)fromDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ToDate", (object?)toDate ?? DBNull.Value);

                using var reader = cmd.ExecuteReader();
                while (reader.Read()) list.Add(MapStorageModel(reader));
                return list;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChillingRepo] GetTemperatureAlerts: {ex.Message}");
                return new List<ChillingStorageModel>();
            }
        }


        // ═══════════════════════════════════════════════════════════
        //  8. DASHBOARD SUMMARY — inline query
        //  Fix #3 — Safe = Temperature < 5 (not <= 5)
        //  Fix #3 — scoped to plantId
        // ═══════════════════════════════════════════════════════════
        public ChillingDashboardSummaryModel GetDashboardSummary(int? plantId)
        {
            try
            {
                string query = @"
                    SELECT
                        COUNT(*)                                                        AS TotalEntries,
                        ISNULL(SUM(MilkQuantity), 0)                                   AS TotalQuantity,
                        SUM(CASE WHEN Temperature > 8   THEN 1 ELSE 0 END)             AS CriticalAlerts,
                        SUM(CASE WHEN Temperature > 5
                                  AND Temperature <= 8  THEN 1 ELSE 0 END)             AS Warnings,
                        SUM(CASE WHEN Temperature IS NOT NULL
                                  AND Temperature < 5   THEN 1 ELSE 0 END)             AS SafeCount,
                        SUM(CASE WHEN Temperature IS NULL THEN 1 ELSE 0 END)            AS UnknownCount
                    FROM Chilling.ChillingStorage
                    WHERE StoredDate = CAST(GETDATE() AS DATE)
                      AND (@PlantId IS NULL OR PlantId = @PlantId)";

                using var con = _db.GetConnection();
                con.Open();
                using var cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@PlantId", (object?)plantId ?? DBNull.Value);
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChillingRepo] GetDashboardSummary: {ex.Message}");
                return new ChillingDashboardSummaryModel { ReportDate = DateTime.Today };
            }
        }


        // ═══════════════════════════════════════════════════════════
        //  9. PLANT CAPACITY SUMMARY — inline query
        // ═══════════════════════════════════════════════════════════
        public List<ChillingPlantCapacityModel> GetPlantCapacitySummary()
        {
            try
            {
                var list = new List<ChillingPlantCapacityModel>();
                string query = @"
                    SELECT pp.PlantId, pp.PlantName,
                           ISNULL(pp.Location, '')                                         AS Location,
                           ISNULL(SUM(cs.MilkQuantity), 0)                                 AS TotalStoredAllTime,
                           ISNULL(SUM(CASE WHEN cs.StoredDate = CAST(GETDATE() AS DATE)
                                          THEN cs.MilkQuantity ELSE 0 END), 0)             AS StoredToday,
                           COUNT(cs.StorageId)                                              AS TotalEntries,
                           SUM(CASE WHEN cs.Temperature > 5 THEN 1 ELSE 0 END)             AS AlertCount
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChillingRepo] GetPlantCapacitySummary: {ex.Message}");
                return new List<ChillingPlantCapacityModel>();
            }
        }


        // ═══════════════════════════════════════════════════════════
        //  10. GET PLANTS — inline query
        // ═══════════════════════════════════════════════════════════
        public List<PlantDropdownModel> GetPlants()
        {
            try
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChillingRepo] GetPlants: {ex.Message}");
                return new List<PlantDropdownModel>();
            }
        }


        // ═══════════════════════════════════════════════════════════
        //  11. GET PRODUCTS — inline query
        // ═══════════════════════════════════════════════════════════
        public List<ProductDropdownModel> GetProducts()
        {
            try
            {
                var list = new List<ProductDropdownModel>();
                string query = @"SELECT ProductId, ProductName, ProductType, Unit
                                 FROM Production.Products ORDER BY ProductName";
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChillingRepo] GetProducts: {ex.Message}");
                return new List<ProductDropdownModel>();
            }
        }


        // ═══════════════════════════════════════════════════════════
        //  12. GET BY PLANT FILTERED — inline query
        // ═══════════════════════════════════════════════════════════
        public List<ChillingStorageModel> GetByPlantFiltered(int plantId,
                                                              DateTime? fromDate,
                                                              DateTime? toDate,
                                                              string? search)
        {
            try
            {
                var list = new List<ChillingStorageModel>();
                string query = @"
                    SELECT cs.StorageId, cs.PlantId, cs.ProductId, cs.Shift,
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
                while (reader.Read()) list.Add(MapStorageModel(reader));
                return list;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChillingRepo] GetByPlantFiltered: {ex.Message}");
                return new List<ChillingStorageModel>();
            }
        }


        // ═══════════════════════════════════════════════════════════
        //  13. GET ALL FILTERED — inline query
        // ═══════════════════════════════════════════════════════════
        public List<ChillingStorageModel> GetAllFiltered(DateTime? fromDate,
                                                          DateTime? toDate,
                                                          string? search)
        {
            try
            {
                var list = new List<ChillingStorageModel>();
                string query = @"
                    SELECT cs.StorageId, cs.PlantId, cs.ProductId, cs.Shift,
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
                while (reader.Read()) list.Add(MapStorageModel(reader));
                return list;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChillingRepo] GetAllFiltered: {ex.Message}");
                return new List<ChillingStorageModel>();
            }
        }


        // ═══════════════════════════════════════════════════════════
        //  14. QUICK UPDATE ENTRY — inline query
        // ═══════════════════════════════════════════════════════════
        public bool QuickUpdateEntry(int storageId, decimal milkQuantity, decimal? temperature)
        {
            try
            {
                string query = @"
                    UPDATE Chilling.ChillingStorage
                    SET MilkQuantity = @MilkQuantity,
                        Temperature  = @Temperature
                    WHERE StorageId  = @StorageId";

                using var con = _db.GetConnection();
                con.Open();
                using var cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@StorageId", storageId);
                cmd.Parameters.AddWithValue("@MilkQuantity", milkQuantity);
                cmd.Parameters.AddWithValue("@Temperature", (object?)temperature ?? DBNull.Value);
                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChillingRepo] QuickUpdateEntry: {ex.Message}");
                return false;
            }
        }


        // ═══════════════════════════════════════════════════════════
        //  15. INSERT WITH SHIFT — inline query
        // ═══════════════════════════════════════════════════════════
        public int InsertWithShift(ChillingStoreItemModel model)
        {
            try
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
                if (reader.Read()) return Convert.ToInt32(reader["NewStorageId"]);
                return 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChillingRepo] InsertWithShift: {ex.Message}");
                return 0;
            }
        }


        // ═══════════════════════════════════════════════════════════
        //  16. GET DAILY REPORT — inline query
        // ═══════════════════════════════════════════════════════════
        public List<ChillingDailyReportModel> GetDailyReport(int? plantId,
                                                               DateTime? fromDate,
                                                               DateTime? toDate)
        {
            try
            {
                var list = new List<ChillingDailyReportModel>();
                string query = @"
                    SELECT cs.StoredDate,
                           COUNT(*)                                                          AS TotalEntries,
                           ISNULL(SUM(cs.MilkQuantity), 0)                                  AS TotalQuantity,
                           SUM(CASE WHEN cs.Temperature IS NOT NULL
                                     AND cs.Temperature < 5   THEN 1 ELSE 0 END)            AS SafeCount,
                           SUM(CASE WHEN cs.Temperature > 5
                                     AND cs.Temperature <= 8  THEN 1 ELSE 0 END)            AS WarningCount,
                           SUM(CASE WHEN cs.Temperature > 8   THEN 1 ELSE 0 END)            AS CriticalCount,
                           SUM(CASE WHEN cs.Temperature IS NULL THEN 1 ELSE 0 END)           AS UnknownCount
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChillingRepo] GetDailyReport: {ex.Message}");
                return new List<ChillingDailyReportModel>();
            }
        }


        // ═══════════════════════════════════════════════════════════
        //  17. GET PRODUCT REPORT — inline query
        // ═══════════════════════════════════════════════════════════
        public List<ChillingProductReportModel> GetProductReport(int? plantId,
                                                                   DateTime? fromDate,
                                                                   DateTime? toDate)
        {
            try
            {
                var list = new List<ChillingProductReportModel>();
                string query = @"
                    SELECT ISNULL(p.ProductName, 'Raw Milk')                        AS ItemName,
                           p.ProductType, p.Unit,
                           COUNT(*)                                                  AS TotalEntries,
                           ISNULL(SUM(cs.MilkQuantity), 0)                          AS TotalQuantity,
                           ISNULL(AVG(CAST(cs.Temperature AS FLOAT)), 0)             AS AvgTemperature,
                           SUM(CASE WHEN cs.Temperature > 5 THEN 1 ELSE 0 END)      AS AlertCount,
                           ISNULL(SUM(cs.MilkQuantity)/NULLIF(COUNT(*),0), 0)        AS AvgQuantityPerEntry
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChillingRepo] GetProductReport: {ex.Message}");
                return new List<ChillingProductReportModel>();
            }
        }


        // ═══════════════════════════════════════════════════════════
        //  18. GET WEEKLY TREND — inline query
        //  Fix #8 — LEFT JOIN so plants with no entries appear
        // ═══════════════════════════════════════════════════════════
        public List<ChillingWeeklyTrendModel> GetWeeklyTrend(int? plantId)
        {
            try
            {
                var result = new List<ChillingWeeklyTrendModel>();
                var dates = Enumerable.Range(0, 7)
                    .Select(i => DateTime.Today.AddDays(-6 + i))
                    .ToList();

                string query = @"
                    SELECT pp.PlantId, pp.PlantName,
                           cs.StoredDate,
                           ISNULL(SUM(cs.MilkQuantity), 0) AS DayQty,
                           COUNT(cs.StorageId)              AS DayEntries
                    FROM Production.ProcessingPlants pp
                    LEFT JOIN Chilling.ChillingStorage cs
                        ON cs.PlantId    = pp.PlantId
                       AND cs.StoredDate >= @FromDate
                       AND cs.StoredDate <= @ToDate
                    WHERE (@PlantId IS NULL OR pp.PlantId = @PlantId)
                    GROUP BY pp.PlantId, pp.PlantName, cs.StoredDate
                    ORDER BY pp.PlantId, cs.StoredDate";

                using var con = _db.GetConnection();
                con.Open();
                using var cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@FromDate", DateTime.Today.AddDays(-6));
                cmd.Parameters.AddWithValue("@ToDate", DateTime.Today);
                cmd.Parameters.AddWithValue("@PlantId", (object?)plantId ?? DBNull.Value);

                var raw = new Dictionary<int, (string Name, List<(DateTime Date, decimal Qty, int Cnt)> Rows)>();

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    int pid = Convert.ToInt32(reader["PlantId"]);
                    string pn = reader["PlantName"].ToString();

                    if (!raw.ContainsKey(pid))
                        raw[pid] = (pn, new List<(DateTime, decimal, int)>());

                    if (reader["StoredDate"] != DBNull.Value)
                    {
                        raw[pid].Rows.Add((
                            Convert.ToDateTime(reader["StoredDate"]),
                            Convert.ToDecimal(reader["DayQty"]),
                            Convert.ToInt32(reader["DayEntries"])
                        ));
                    }
                }

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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChillingRepo] GetWeeklyTrend: {ex.Message}");
                return new List<ChillingWeeklyTrendModel>();
            }
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

        // Fix #3 — Safe = temp < 5 (not <= 5)
        private string GetTempStatus(decimal? temp)
        {
            if (temp == null) return "Unknown";
            if (temp < 5) return "Safe";
            if (temp <= 8) return "Warning";
            return "Critical";
        }

        private bool HasColumn(SqlDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
                if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }
    }
}