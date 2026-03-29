using DairyIndustry.Data;
using DairyIndustry.Models;
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
        //  1. GET ALL — inline query
        //     Returns all entries across all plants with TempStatus
        // ═══════════════════════════════════════════════════════════
        public List<ChillingStorageModel> GetAll(DateTime? fromDate, DateTime? toDate)
        {
            var list = new List<ChillingStorageModel>();

            string query = @"
                SELECT
                    cs.StorageId,
                    cs.PlantId,
                    pp.PlantName,
                    cs.ProductId,
                    ISNULL(p.ProductName, 'Raw Milk') AS ItemName,
                    p.ProductType,
                    p.Unit,
                    cs.MilkQuantity,
                    cs.Temperature,
                    cs.StoredDate,
                    CASE
                        WHEN cs.Temperature IS NULL                          THEN 'Unknown'
                        WHEN cs.Temperature > 7.0                            THEN 'Critical'
                        WHEN cs.Temperature BETWEEN 5.0 AND 7.0             THEN 'Warning'
                        ELSE 'Safe'
                    END AS TempStatus
                FROM Chilling.ChillingStorage cs
                INNER JOIN Production.ProcessingPlants pp ON pp.PlantId  = cs.PlantId
                LEFT  JOIN Production.Products         p  ON p.ProductId = cs.ProductId
                WHERE (@FromDate IS NULL OR cs.StoredDate >= @FromDate)
                  AND (@ToDate   IS NULL OR cs.StoredDate <= @ToDate)
                ORDER BY cs.StoredDate DESC, pp.PlantName";

            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@FromDate", (object?)fromDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ToDate", (object?)toDate ?? DBNull.Value);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(MapStorageModel(reader));
            }
            return list;
        }


        // ═══════════════════════════════════════════════════════════
        //  2. GET BY ID — inline query
        //     Returns one entry for Details / Edit page
        // ═══════════════════════════════════════════════════════════
        public ChillingStorageModel GetById(int storageId)
        {
            string query = @"
                SELECT
                    cs.StorageId,
                    cs.PlantId,
                    pp.PlantName,
                    cs.ProductId,
                    ISNULL(p.ProductName, 'Raw Milk') AS ItemName,
                    p.ProductType,
                    p.Unit,
                    cs.MilkQuantity,
                    cs.Temperature,
                    cs.StoredDate,
                    CASE
                        WHEN cs.Temperature IS NULL              THEN 'Unknown'
                        WHEN cs.Temperature > 7.0               THEN 'Critical'
                        WHEN cs.Temperature BETWEEN 5.0 AND 7.0 THEN 'Warning'
                        ELSE 'Safe'
                    END AS TempStatus
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
        //  3. STORE ITEM — uses SP usp_Chilling_StoreItem
        //     Inserts new entry, returns new StorageId
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
        //  4. UPDATE ENTRY — inline query
        //     Updates quantity, temperature and product of an entry
        // ═══════════════════════════════════════════════════════════
        public bool UpdateEntry(ChillingStoreItemModel model)
        {
            string query = @"
                UPDATE Chilling.ChillingStorage
                SET
                    MilkQuantity = @MilkQuantity,
                    Temperature  = @Temperature,
                    ProductId    = @ProductId
                WHERE StorageId  = @StorageId";

            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@StorageId", model.StorageId);
            cmd.Parameters.AddWithValue("@MilkQuantity", model.MilkQuantity);
            cmd.Parameters.AddWithValue("@Temperature", (object?)model.Temperature ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ProductId", (object?)model.ProductId ?? DBNull.Value);

            int rows = cmd.ExecuteNonQuery();
            return rows > 0;
        }


        // ═══════════════════════════════════════════════════════════
        //  5. DELETE ENTRY — inline query
        // ═══════════════════════════════════════════════════════════
        public bool DeleteEntry(int storageId)
        {
            string query = "DELETE FROM Chilling.ChillingStorage WHERE StorageId = @StorageId";

            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@StorageId", storageId);

            int rows = cmd.ExecuteNonQuery();
            return rows > 0;
        }


        // ═══════════════════════════════════════════════════════════
        //  6. GET BY PLANT — uses SP usp_Chilling_GetStorageByPlant
        //     Filtered list for one specific plant
        // ═══════════════════════════════════════════════════════════
        public List<ChillingStorageModel> GetByPlant(int plantId, DateTime? fromDate, DateTime? toDate)
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
                list.Add(new ChillingStorageModel
                {
                    StorageId = Convert.ToInt32(reader["StorageId"]),
                    PlantId = plantId,
                    ProductId = null,      // SP does not return ProductId column
                    StoredDate = Convert.ToDateTime(reader["StoredDate"]),
                    MilkQuantity = Convert.ToDecimal(reader["MilkQuantity"]),
                    Temperature = reader["Temperature"] == DBNull.Value
                                        ? null
                                        : Convert.ToDecimal(reader["Temperature"]),
                    PlantName = reader["PlantName"].ToString(),
                    ItemName = reader["ItemName"].ToString(),
                    ProductType = reader["ProductType"] == DBNull.Value
                                        ? null
                                        : reader["ProductType"].ToString(),
                    Unit = reader["Unit"] == DBNull.Value
                                        ? null
                                        : reader["Unit"].ToString(),
                    TempStatus = GetTempStatus(reader["Temperature"])
                });
            }
            return list;
        }


        // ═══════════════════════════════════════════════════════════
        //  7. GET TEMPERATURE ALERTS — inline query
        //     Only entries where temperature > 5°C
        // ═══════════════════════════════════════════════════════════
        public List<ChillingStorageModel> GetTemperatureAlerts(DateTime? fromDate, DateTime? toDate)
        {
            var list = new List<ChillingStorageModel>();

            string query = @"
                SELECT
                    cs.StorageId,
                    cs.PlantId,
                    pp.PlantName,
                    cs.ProductId,
                    ISNULL(p.ProductName, 'Raw Milk') AS ItemName,
                    p.ProductType,
                    p.Unit,
                    cs.MilkQuantity,
                    cs.Temperature,
                    cs.StoredDate,
                    CASE
                        WHEN cs.Temperature > 7.0               THEN 'Critical'
                        WHEN cs.Temperature BETWEEN 5.0 AND 7.0 THEN 'Warning'
                        ELSE 'Safe'
                    END AS TempStatus
                FROM Chilling.ChillingStorage cs
                INNER JOIN Production.ProcessingPlants pp ON pp.PlantId  = cs.PlantId
                LEFT  JOIN Production.Products         p  ON p.ProductId = cs.ProductId
                WHERE cs.Temperature > 5.0
                  AND (@FromDate IS NULL OR cs.StoredDate >= @FromDate)
                  AND (@ToDate   IS NULL OR cs.StoredDate <= @ToDate)
                ORDER BY cs.Temperature DESC, cs.StoredDate DESC";

            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@FromDate", (object?)fromDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ToDate", (object?)toDate ?? DBNull.Value);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(MapStorageModel(reader));
            }
            return list;
        }


        // ═══════════════════════════════════════════════════════════
        //  8. GET DASHBOARD SUMMARY — inline query
        //     Today's totals: entries, quantity, alert counts
        // ═══════════════════════════════════════════════════════════
        public ChillingDashboardSummaryModel GetDashboardSummary()
        {
            string query = @"
                SELECT
                    COUNT(*)                                                         AS TotalEntries,
                    ISNULL(SUM(MilkQuantity), 0)                                     AS TotalQuantity,
                    SUM(CASE WHEN Temperature > 7.0  THEN 1 ELSE 0 END)             AS CriticalAlerts,
                    SUM(CASE WHEN Temperature BETWEEN 5.0 AND 7.0 THEN 1 ELSE 0 END) AS Warnings,
                    SUM(CASE WHEN Temperature <= 5.0 AND Temperature IS NOT NULL THEN 1 ELSE 0 END) AS SafeCount,
                    SUM(CASE WHEN Temperature IS NULL THEN 1 ELSE 0 END)             AS UnknownCount,
                    CAST(GETDATE() AS DATE)                                           AS ReportDate
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
                    TotalEntries = Convert.ToInt32(reader["TotalEntries"]),
                    TotalQuantity = Convert.ToDecimal(reader["TotalQuantity"]),
                    CriticalAlerts = Convert.ToInt32(reader["CriticalAlerts"]),
                    Warnings = Convert.ToInt32(reader["Warnings"]),
                    SafeCount = Convert.ToInt32(reader["SafeCount"]),
                    UnknownCount = Convert.ToInt32(reader["UnknownCount"]),
                    ReportDate = Convert.ToDateTime(reader["ReportDate"])
                };
            }

            return new ChillingDashboardSummaryModel
            {
                ReportDate = DateTime.Today
            };
        }


        // ═══════════════════════════════════════════════════════════
        //  9. GET PLANT CAPACITY SUMMARY — inline query
        //     Per-plant breakdown for capacity monitoring page
        // ═══════════════════════════════════════════════════════════
        public List<ChillingPlantCapacityModel> GetPlantCapacitySummary()
        {
            var list = new List<ChillingPlantCapacityModel>();

            string query = @"
                SELECT
                    pp.PlantId,
                    pp.PlantName,
                    pp.Location,
                    ISNULL(SUM(cs.MilkQuantity), 0) AS TotalStoredAllTime,
                    ISNULL(SUM(CASE WHEN cs.StoredDate = CAST(GETDATE() AS DATE)
                                    THEN cs.MilkQuantity ELSE 0 END), 0) AS StoredToday,
                    COUNT(cs.StorageId)              AS TotalEntries,
                    SUM(CASE WHEN cs.Temperature > 7.0 THEN 1 ELSE 0 END) AS AlertCount
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
                    Location = reader["Location"] == DBNull.Value
                                            ? null
                                            : reader["Location"].ToString(),
                    TotalStoredAllTime = Convert.ToDecimal(reader["TotalStoredAllTime"]),
                    StoredToday = Convert.ToDecimal(reader["StoredToday"]),
                    TotalEntries = Convert.ToInt32(reader["TotalEntries"]),
                    AlertCount = Convert.ToInt32(reader["AlertCount"])
                });
            }
            return list;
        }


        // ═══════════════════════════════════════════════════════════
        //  10. GET PLANTS — inline query (dropdown helper)
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
                    Location = reader["Location"] == DBNull.Value
                                    ? null
                                    : reader["Location"].ToString()
                });
            }
            return list;
        }


        // ═══════════════════════════════════════════════════════════
        //  11. GET PRODUCTS — inline query (dropdown helper)
        // ═══════════════════════════════════════════════════════════
        public List<ProductDropdownModel> GetProducts()
        {
            var list = new List<ProductDropdownModel>();

            string query = "SELECT ProductId, ProductName, ProductType, Unit FROM Production.Products ORDER BY ProductName";

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
        //  PRIVATE HELPERS
        // ═══════════════════════════════════════════════════════════

        // Maps a SqlDataReader row to ChillingStorageModel
        // Used by GetAll, GetById, GetTemperatureAlerts
        private ChillingStorageModel MapStorageModel(SqlDataReader reader)
        {
            return new ChillingStorageModel
            {
                StorageId = Convert.ToInt32(reader["StorageId"]),
                PlantId = Convert.ToInt32(reader["PlantId"]),
                PlantName = reader["PlantName"].ToString(),
                ProductId = reader["ProductId"] == DBNull.Value
                                    ? null
                                    : Convert.ToInt32(reader["ProductId"]),
                ItemName = reader["ItemName"].ToString(),
                ProductType = reader["ProductType"] == DBNull.Value
                                    ? null
                                    : reader["ProductType"].ToString(),
                Unit = reader["Unit"] == DBNull.Value
                                    ? null
                                    : reader["Unit"].ToString(),
                MilkQuantity = Convert.ToDecimal(reader["MilkQuantity"]),
                Temperature = reader["Temperature"] == DBNull.Value
                                    ? null
                                    : Convert.ToDecimal(reader["Temperature"]),
                StoredDate = Convert.ToDateTime(reader["StoredDate"]),
                TempStatus = reader["TempStatus"].ToString()
            };
        }

        // Calculates TempStatus from a raw DB value
        // Used by GetByPlant (SP does not return TempStatus column)
        private string GetTempStatus(object tempValue)
        {
            if (tempValue == DBNull.Value || tempValue == null)
                return "Unknown";

            decimal temp = Convert.ToDecimal(tempValue);
            if (temp > 7.0m) return "Critical";
            if (temp >= 5.0m) return "Warning";
            return "Safe";
        }
    }
}
