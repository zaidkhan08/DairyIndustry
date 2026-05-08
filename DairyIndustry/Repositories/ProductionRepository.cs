using DairyIndustry.Data;
using DairyIndustry.Models.Production;
using DairyIndustry.Models.Logistics;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DairyIndustry.Repositories
{
    public class ProductionRepository : IProductionRepository
    {
        private readonly DbHelper _db;

        public ProductionRepository(DbHelper db)
        {
            _db = db;
        }

        // ════════════════════════════════════════════════════════
        // DROPDOWN — Closed batches not yet dispatched
        // ════════════════════════════════════════════════════════

        public List<BatchDropdownModel> GetClosedBatches()
        {
            var list = new List<BatchDropdownModel>();

            string query = @"
                SELECT
                    cb.BatchId,
                    'B-' + CAST(cb.BatchId AS VARCHAR)
                        + ' | ' + cc.CenterName
                        + ' | ' + FORMAT(cb.BatchDate, 'dd-MMM-yyyy')
                        + ' | ' + cb.Shift AS DisplayText
                FROM Collection.CollectionBatches cb
                INNER JOIN Collection.CollectionCenters cc ON cc.CenterId = cb.CenterId
                WHERE cb.Status = 'Closed'
                  AND NOT EXISTS (
                      SELECT 1 FROM Production.MilkTransfers mt
                      WHERE mt.BatchId = cb.BatchId
                  )
                ORDER BY cb.BatchDate DESC";

            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.CommandType = CommandType.Text;
                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new BatchDropdownModel
                        {
                            BatchId = Convert.ToInt32(reader["BatchId"]),
                            DisplayText = reader["DisplayText"].ToString()
                        });
                    }
                }
            }

            return list;
        }

        // ════════════════════════════════════════════════════════
        // DROPDOWN — All vehicles (with driver info)
        // ════════════════════════════════════════════════════════

        public List<VehiclesModel> GetAllVehicles()
        {
            var list = new List<VehiclesModel>();

            string query = @"
                SELECT
                    v.VehicleId,
                    v.VehicleNumber,
                    v.Capacity,
                    v.DriverId,
                    d.DriverName,
                    d.Status AS DriverStatus
                FROM Logistics.VehiclesNew v
                LEFT JOIN Logistics.DriversNew d ON d.DriverId = v.DriverId
                ORDER BY v.VehicleNumber";

            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.CommandType = CommandType.Text;
                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new VehiclesModel
                        {
                            VehicleId = Convert.ToInt32(reader["VehicleId"]),
                            VehicleNumber = reader["VehicleNumber"].ToString(),
                            Capacity = reader["Capacity"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["Capacity"]),
                            DriverId = reader["DriverId"] == DBNull.Value ? 0 : Convert.ToInt32(reader["DriverId"]),
                            DriverName = reader["DriverName"] == DBNull.Value ? null : reader["DriverName"].ToString(),
                            DriverStatus = reader["DriverStatus"] == DBNull.Value ? null : reader["DriverStatus"].ToString()
                        });
                    }
                }
            }

            return list;
        }

        // ════════════════════════════════════════════════════════
        // DISPATCH — calls SP usp_Production_DispatchMilkTransfer
        // MilkTypeId is now a required parameter (no more guessing)
        // ════════════════════════════════════════════════════════

        public int DispatchMilkTransfer(int batchId, int milkTypeId, int vehicleId, int plantId,
                                        decimal dispatchQty, DateTime dispatchDate)
        {
            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand("Production.usp_Production_DispatchMilkTransfer", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@BatchId", batchId);
                cmd.Parameters.AddWithValue("@MilkTypeId", milkTypeId);   // ← FIXED: was missing
                cmd.Parameters.AddWithValue("@VehicleId", vehicleId);
                cmd.Parameters.AddWithValue("@PlantId", plantId);
                cmd.Parameters.AddWithValue("@DispatchQty", dispatchQty);
                cmd.Parameters.AddWithValue("@DispatchDate", dispatchDate);

                con.Open();
                var result = cmd.ExecuteScalar();
                return Convert.ToInt32(result);
            }
        }

        // ════════════════════════════════════════════════════════
        // RECEIVE — updates transfer, upserts RawMilkInventory
        // FIX: MilkTypeId now read directly from MilkTransfers
        //      (previously used ISNULL(mc.MilkTypeId, 1) which
        //       always fell back to type 1 = Cow milk)
        // ════════════════════════════════════════════════════════

        public void ReceiveMilkTransfer(int transferId, decimal receivedQty, DateTime receivedDate)
        {
            using (SqlConnection con = _db.GetConnection())
            {
                con.Open();

                int plantId = 0;
                int milkTypeId = 0;
                decimal lossQty = 0;

                // ── FIXED lookup: read MilkTypeId straight from MilkTransfers ──
                string lookupQuery = @"
                    SELECT
                        mt.PlantId,
                        mt.DispatchQty,
                        mt.MilkTypeId
                    FROM Production.MilkTransfers mt
                    WHERE mt.TransferId = @TransferId";

                using (SqlCommand cmd = new SqlCommand(lookupQuery, con))
                {
                    cmd.Parameters.AddWithValue("@TransferId", transferId);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            plantId = Convert.ToInt32(r["PlantId"]);
                            milkTypeId = Convert.ToInt32(r["MilkTypeId"]);
                            decimal dispatchQty = Convert.ToDecimal(r["DispatchQty"]);
                            lossQty = Math.Max(0, dispatchQty - receivedQty);
                        }
                        else
                        {
                            throw new InvalidOperationException($"Transfer {transferId} not found.");
                        }
                    }
                }

                // ── Update transfer record ──────────────────────
                string updateTransfer = @"
                    UPDATE Production.MilkTransfers
                    SET ReceivedQty  = @ReceivedQty,
                        LossQty      = @LossQty,
                        ReceivedDate = @ReceivedDate
                    WHERE TransferId = @TransferId";

                using (SqlCommand cmd = new SqlCommand(updateTransfer, con))
                {
                    cmd.Parameters.AddWithValue("@TransferId", transferId);
                    cmd.Parameters.AddWithValue("@ReceivedQty", receivedQty);
                    cmd.Parameters.AddWithValue("@LossQty", lossQty);
                    cmd.Parameters.AddWithValue("@ReceivedDate", receivedDate);
                    cmd.ExecuteNonQuery();
                }

                // ── Upsert RawMilkInventory with correct MilkTypeId ─
                string upsertInventory = @"
                    IF EXISTS (
                        SELECT 1 FROM Production.RawMilkInventory
                        WHERE PlantId = @PlantId AND MilkTypeId = @MilkTypeId
                    )
                        UPDATE Production.RawMilkInventory
                        SET Quantity    = Quantity + @ReceivedQty,
                            LastUpdated = GETDATE()
                        WHERE PlantId = @PlantId AND MilkTypeId = @MilkTypeId
                    ELSE
                        INSERT INTO Production.RawMilkInventory
                            (PlantId, MilkTypeId, Quantity, LastUpdated)
                        VALUES
                            (@PlantId, @MilkTypeId, @ReceivedQty, GETDATE())";

                using (SqlCommand cmd = new SqlCommand(upsertInventory, con))
                {
                    cmd.Parameters.AddWithValue("@PlantId", plantId);
                    cmd.Parameters.AddWithValue("@MilkTypeId", milkTypeId);
                    cmd.Parameters.AddWithValue("@ReceivedQty", receivedQty);
                    cmd.ExecuteNonQuery();
                }

                // ── Mark batch as Dispatched ────────────────────
                string updateBatch = @"
                    UPDATE Collection.CollectionBatches
                    SET Status = 'Dispatched'
                    FROM Collection.CollectionBatches cb
                    INNER JOIN Production.MilkTransfers mt ON mt.BatchId = cb.BatchId
                    WHERE mt.TransferId = @TransferId";

                using (SqlCommand cmd = new SqlCommand(updateBatch, con))
                {
                    cmd.Parameters.AddWithValue("@TransferId", transferId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ════════════════════════════════════════════════════════
        // GET ALL TRANSFERS
        // ════════════════════════════════════════════════════════

        public List<MilkTransferModel> GetAllTransfers(int? plantId = null)
        {
            var list = new List<MilkTransferModel>();

            string query = @"
                SELECT
                    mt.TransferId,
                    mt.BatchId,
                    mt.VehicleId,
                    mt.PlantId,
                    mt.MilkTypeId,
                    mt.DispatchQty,
                    mt.ReceivedQty,
                    mt.LossQty,
                    mt.DispatchDate,
                    mt.ReceivedDate,

                    'B-' + CAST(cb.BatchId AS VARCHAR) AS BatchRef,
                    cc.CenterName,
                    pp.PlantName,
                    v.VehicleNumber,
                    d.DriverName,
                    mtp.MilkTypeName,

                    CASE
                        WHEN qt.TransferId IS NOT NULL THEN 1
                        ELSE 0
                    END AS HasQualityTest

                FROM Production.MilkTransfers mt
                INNER JOIN Collection.CollectionBatches  cb  ON cb.BatchId   = mt.BatchId
                INNER JOIN Collection.CollectionCenters  cc  ON cc.CenterId  = cb.CenterId
                INNER JOIN Production.ProcessingPlants   pp  ON pp.PlantId   = mt.PlantId
                INNER JOIN Logistics.VehiclesNew         v   ON v.VehicleId  = mt.VehicleId
                LEFT  JOIN Logistics.DriversNew          d   ON d.DriverId   = v.DriverId
                LEFT  JOIN Finance.MilkTypes             mtp ON mtp.MilkTypeId = mt.MilkTypeId
                LEFT  JOIN Production.TransferQualityTests qt ON qt.TransferId = mt.TransferId

                WHERE (@PlantId IS NULL OR mt.PlantId = @PlantId)
                ORDER BY mt.DispatchDate DESC";

            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("@PlantId", (object?)plantId ?? DBNull.Value);
                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        list.Add(MapTransfer(reader));
                }
            }

            return list;
        }

        // ════════════════════════════════════════════════════════
        // GET SINGLE TRANSFER BY ID
        // ════════════════════════════════════════════════════════

        public MilkTransferModel GetTransferById(int transferId)
        {
            MilkTransferModel transfer = null;

            string query = @"
                SELECT
                    mt.TransferId,
                    mt.BatchId,
                    mt.VehicleId,
                    mt.PlantId,
                    mt.MilkTypeId,
                    mt.DispatchQty,
                    mt.ReceivedQty,
                    mt.LossQty,
                    mt.DispatchDate,
                    mt.ReceivedDate,
                    'B-' + CAST(cb.BatchId AS VARCHAR) AS BatchRef,
                    cc.CenterName,
                    pp.PlantName,
                    v.VehicleNumber,
                    d.DriverName,
                    mtp.MilkTypeName
                FROM Production.MilkTransfers mt
                INNER JOIN Collection.CollectionBatches  cb  ON cb.BatchId   = mt.BatchId
                INNER JOIN Collection.CollectionCenters  cc  ON cc.CenterId  = cb.CenterId
                INNER JOIN Production.ProcessingPlants   pp  ON pp.PlantId   = mt.PlantId
                INNER JOIN Logistics.VehiclesNew         v   ON v.VehicleId  = mt.VehicleId
                LEFT  JOIN Logistics.DriversNew          d   ON d.DriverId   = v.DriverId
                LEFT  JOIN Finance.MilkTypes             mtp ON mtp.MilkTypeId = mt.MilkTypeId
                WHERE mt.TransferId = @TransferId";

            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("@TransferId", transferId);
                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                        transfer = MapTransfer(reader);
                }
            }

            return transfer;
        }

        private MilkTransferModel MapTransfer(SqlDataReader reader)
        {
            bool HasColumn(string name)
            {
                for (int i = 0; i < reader.FieldCount; i++)
                    if (reader.GetName(i).Equals(name, StringComparison.OrdinalIgnoreCase))
                        return true;
                return false;
            }

            return new MilkTransferModel
            {
                TransferId = Convert.ToInt32(reader["TransferId"]),
                BatchId = Convert.ToInt32(reader["BatchId"]),
                VehicleId = Convert.ToInt32(reader["VehicleId"]),
                PlantId = Convert.ToInt32(reader["PlantId"]),
                MilkTypeId = reader["MilkTypeId"] == DBNull.Value ? 0 : Convert.ToInt32(reader["MilkTypeId"]),
                DispatchQty = Convert.ToDecimal(reader["DispatchQty"]),
                ReceivedQty = reader["ReceivedQty"] == DBNull.Value ? null : Convert.ToDecimal(reader["ReceivedQty"]),
                LossQty = reader["LossQty"] == DBNull.Value ? null : Convert.ToDecimal(reader["LossQty"]),
                DispatchDate = Convert.ToDateTime(reader["DispatchDate"]),
                ReceivedDate = reader["ReceivedDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["ReceivedDate"]),
                BatchRef = reader["BatchRef"].ToString(),
                CenterName = reader["CenterName"].ToString(),
                PlantName = reader["PlantName"].ToString(),
                VehicleNumber = reader["VehicleNumber"].ToString(),
                DriverName = reader["DriverName"] == DBNull.Value ? null : reader["DriverName"].ToString(),
                MilkTypeName = reader["MilkTypeName"] == DBNull.Value ? null : reader["MilkTypeName"].ToString(),
                HasQualityTest = HasColumn("HasQualityTest") && Convert.ToInt32(reader["HasQualityTest"]) == 1
            };
        }

        // ════════════════════════════════════════════════════════
        // PRODUCTS — CRUD
        // ════════════════════════════════════════════════════════

        public int AddProduct(string productName, string productType,
                              decimal mrp, string unit, int? shelfLifeDays)
        {
            string query = @"
                INSERT INTO Production.Products (ProductName, ProductType, MRP, Unit, ShelfLifeDays)
                VALUES (@ProductName, @ProductType, @MRP, @Unit, @ShelfLifeDays);
                SELECT SCOPE_IDENTITY();";

            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("@ProductName", productName);
                cmd.Parameters.AddWithValue("@ProductType", productType);
                cmd.Parameters.AddWithValue("@MRP", mrp);
                cmd.Parameters.AddWithValue("@Unit", unit);
                cmd.Parameters.AddWithValue("@ShelfLifeDays", (object?)shelfLifeDays ?? DBNull.Value);
                con.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public List<ProductModel> GetAllProducts()
        {
            var list = new List<ProductModel>();
            string query = "SELECT ProductId, ProductName, ProductType, MRP, Unit, ShelfLifeDays FROM Production.Products ORDER BY ProductName";

            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.CommandType = CommandType.Text;
                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        list.Add(MapProduct(reader));
                }
            }
            return list;
        }

        public ProductModel GetProductById(int productId)
        {
            ProductModel product = null;
            string query = "SELECT ProductId, ProductName, ProductType, MRP, Unit, ShelfLifeDays FROM Production.Products WHERE ProductId = @ProductId";

            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("@ProductId", productId);
                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                        product = MapProduct(reader);
                }
            }
            return product;
        }

        public void UpdateProduct(ProductModel product)
        {
            string query = @"
                UPDATE Production.Products
                SET ProductName   = @ProductName,
                    ProductType   = @ProductType,
                    MRP           = @MRP,
                    Unit          = @Unit,
                    ShelfLifeDays = @ShelfLifeDays
                WHERE ProductId = @ProductId";

            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("@ProductId", product.ProductId);
                cmd.Parameters.AddWithValue("@ProductName", product.ProductName);
                cmd.Parameters.AddWithValue("@ProductType", product.ProductType);
                cmd.Parameters.AddWithValue("@MRP", product.MRP);
                cmd.Parameters.AddWithValue("@Unit", product.Unit);
                cmd.Parameters.AddWithValue("@ShelfLifeDays", (object?)product.ShelfLifeDays ?? DBNull.Value);
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteProduct(int productId)
        {
            string query = "DELETE FROM Production.Products WHERE ProductId = @ProductId";

            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("@ProductId", productId);
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private ProductModel MapProduct(SqlDataReader reader)
        {
            return new ProductModel
            {
                ProductId = Convert.ToInt32(reader["ProductId"]),
                ProductName = reader["ProductName"].ToString(),
                ProductType = reader["ProductType"].ToString(),
                MRP = Convert.ToDecimal(reader["MRP"]),
                Unit = reader["Unit"].ToString(),
                ShelfLifeDays = reader["ShelfLifeDays"] == DBNull.Value ? null : Convert.ToInt32(reader["ShelfLifeDays"])
            };
        }

        // ════════════════════════════════════════════════════════
        // RAW MILK INVENTORY
        // ════════════════════════════════════════════════════════

        public List<RawMilkInventoryModel> GetRawMilkInventory(int? plantId = null)
        {
            var list = new List<RawMilkInventoryModel>();

            string query = @"
                SELECT
                    r.RawMilkInventoryId,
                    r.PlantId,
                    r.MilkTypeId,
                    r.Quantity,
                    r.LastUpdated,
                    pp.PlantName,
                    mt.MilkTypeName
                FROM Production.RawMilkInventory r
                INNER JOIN Production.ProcessingPlants pp ON pp.PlantId    = r.PlantId
                INNER JOIN Finance.MilkTypes           mt ON mt.MilkTypeId = r.MilkTypeId
                WHERE (@PlantId IS NULL OR r.PlantId = @PlantId)
                ORDER BY pp.PlantName, mt.MilkTypeName";

            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("@PlantId", (object?)plantId ?? DBNull.Value);
                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new RawMilkInventoryModel
                        {
                            RawMilkInventoryId = Convert.ToInt32(reader["RawMilkInventoryId"]),
                            PlantId = Convert.ToInt32(reader["PlantId"]),
                            MilkTypeId = Convert.ToInt32(reader["MilkTypeId"]),
                            Quantity = Convert.ToDecimal(reader["Quantity"]),
                            LastUpdated = Convert.ToDateTime(reader["LastUpdated"]),
                            PlantName = reader["PlantName"].ToString(),
                            MilkTypeName = reader["MilkTypeName"].ToString()
                        });
                    }
                }
            }

            return list;
        }

        // ════════════════════════════════════════════════════════
        // PRODUCTION BATCHES — START
        // MilkTypeId is now stored on ProductionBatches row
        // ════════════════════════════════════════════════════════

        public int StartProductionBatch(int plantId, int productId,
                                        decimal milkUsedQuantity, DateTime productionDate,
                                        int milkTypeId)
        {
            string checkQuery = @"
                SELECT ISNULL(Quantity, 0)
                FROM Production.RawMilkInventory
                WHERE PlantId = @PlantId AND MilkTypeId = @MilkTypeId";

            // MilkTypeId stored so UpdateBatchStatus can read it directly
            string insertQuery = @"
                INSERT INTO Production.ProductionBatches
                    (PlantId, ProductId, MilkUsedQuantity, ProductionDate, BatchStatus, MilkTypeId)
                VALUES (@PlantId, @ProductId, @MilkUsedQuantity, @ProductionDate, 'InProgress', @MilkTypeId);
                SELECT SCOPE_IDENTITY();";

            string deductQuery = @"
                UPDATE Production.RawMilkInventory
                SET Quantity = Quantity - @MilkUsedQuantity, LastUpdated = GETDATE()
                WHERE PlantId = @PlantId AND MilkTypeId = @MilkTypeId";

            using (SqlConnection con = _db.GetConnection())
            {
                con.Open();

                using (SqlCommand chk = new SqlCommand(checkQuery, con))
                {
                    chk.Parameters.AddWithValue("@PlantId", plantId);
                    chk.Parameters.AddWithValue("@MilkTypeId", milkTypeId);
                    decimal available = Convert.ToDecimal(chk.ExecuteScalar());
                    if (available < milkUsedQuantity)
                        throw new InvalidOperationException(
                            $"Insufficient milk. Available: {available} L, Required: {milkUsedQuantity} L");
                }

                int newId;
                using (SqlCommand ins = new SqlCommand(insertQuery, con))
                {
                    ins.Parameters.AddWithValue("@PlantId", plantId);
                    ins.Parameters.AddWithValue("@ProductId", productId);
                    ins.Parameters.AddWithValue("@MilkUsedQuantity", milkUsedQuantity);
                    ins.Parameters.AddWithValue("@ProductionDate", productionDate);
                    ins.Parameters.AddWithValue("@MilkTypeId", milkTypeId);
                    newId = Convert.ToInt32(ins.ExecuteScalar());
                }

                using (SqlCommand ded = new SqlCommand(deductQuery, con))
                {
                    ded.Parameters.AddWithValue("@PlantId", plantId);
                    ded.Parameters.AddWithValue("@MilkTypeId", milkTypeId);
                    ded.Parameters.AddWithValue("@MilkUsedQuantity", milkUsedQuantity);
                    ded.ExecuteNonQuery();
                }

                return newId;
            }
        }

        // ════════════════════════════════════════════════════════
        // UPDATE BATCH STATUS
        // FIX: MilkTypeId now read directly from ProductionBatches
        //      (previously guessed from inventory ORDER BY MilkTypeId
        //       which always picked type 1)
        // ════════════════════════════════════════════════════════

        public void UpdateBatchStatus(int productionBatchId, string batchStatus)
        {
            using (SqlConnection con = _db.GetConnection())
            {
                con.Open();

                int plantId = 0;
                int milkTypeId = 0;
                decimal milkUsedQty = 0;

                // ── FIXED: read MilkTypeId directly from the batch row ──
                string batchQuery = @"
                    SELECT pb.PlantId, pb.MilkUsedQuantity, pb.MilkTypeId
                    FROM Production.ProductionBatches pb
                    WHERE pb.ProductionBatchId = @ProductionBatchId
                      AND pb.BatchStatus = 'InProgress'";

                using (SqlCommand cmd = new SqlCommand(batchQuery, con))
                {
                    cmd.Parameters.AddWithValue("@ProductionBatchId", productionBatchId);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            plantId = Convert.ToInt32(r["PlantId"]);
                            milkUsedQty = Convert.ToDecimal(r["MilkUsedQuantity"]);
                            milkTypeId = Convert.ToInt32(r["MilkTypeId"]);
                        }
                    }
                }

                // ── Update the batch status ─────────────────────
                string updateQuery = @"
                    UPDATE Production.ProductionBatches
                    SET BatchStatus = @BatchStatus
                    WHERE ProductionBatchId = @ProductionBatchId
                      AND BatchStatus = 'InProgress'";

                using (SqlCommand cmd = new SqlCommand(updateQuery, con))
                {
                    cmd.Parameters.AddWithValue("@ProductionBatchId", productionBatchId);
                    cmd.Parameters.AddWithValue("@BatchStatus", batchStatus);
                    cmd.ExecuteNonQuery();
                }

                // ── If QCFailed, auto-log entire milk qty as wastage ─
                if (batchStatus == "QCFailed" && plantId > 0 && milkUsedQty > 0)
                {
                    string wastageQuery = @"
                        INSERT INTO Production.MilkProcessWastage
                            (ProductionBatchId, PlantId, MilkTypeId, WastageQuantity, WastageType, Reason, RecordedDate)
                        VALUES
                            (@ProductionBatchId, @PlantId, @MilkTypeId, @WastageQuantity, 'QCFailed',
                             'Batch failed QC — full milk quantity written off', GETDATE())";

                    using (SqlCommand cmd = new SqlCommand(wastageQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@ProductionBatchId", productionBatchId);
                        cmd.Parameters.AddWithValue("@PlantId", plantId);
                        cmd.Parameters.AddWithValue("@MilkTypeId", milkTypeId);
                        cmd.Parameters.AddWithValue("@WastageQuantity", milkUsedQty);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public List<ProductionBatchModel> GetAllProductionBatches(int? plantId = null)
        {
            var list = new List<ProductionBatchModel>();
            string query = @"
                SELECT
                    pb.ProductionBatchId,
                    pb.PlantId,
                    pb.ProductId,
                    pb.MilkUsedQuantity,
                    pb.ProductionDate,
                    pb.BatchStatus,
                    pb.MilkTypeId,
                    pp.PlantName,
                    p.ProductName,
                    p.ProductType,
                    p.Unit,
                    mt.MilkTypeName
                FROM Production.ProductionBatches pb
                INNER JOIN Production.ProcessingPlants pp ON pp.PlantId  = pb.PlantId
                INNER JOIN Production.Products         p  ON p.ProductId = pb.ProductId
                LEFT  JOIN Finance.MilkTypes           mt ON mt.MilkTypeId = pb.MilkTypeId
                WHERE (@PlantId IS NULL OR pb.PlantId = @PlantId)
                ORDER BY pb.ProductionDate DESC";

            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("@PlantId", (object?)plantId ?? DBNull.Value);
                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        list.Add(MapProductionBatch(reader));
                }
            }
            return list;
        }

        public ProductionBatchModel GetProductionBatchById(int productionBatchId)
        {
            ProductionBatchModel batch = null;
            string query = @"
                SELECT
                    pb.ProductionBatchId,
                    pb.PlantId,
                    pb.ProductId,
                    pb.MilkUsedQuantity,
                    pb.ProductionDate,
                    pb.BatchStatus,
                    pb.MilkTypeId,
                    pp.PlantName,
                    p.ProductName,
                    p.ProductType,
                    p.Unit,
                    mt.MilkTypeName
                FROM Production.ProductionBatches pb
                INNER JOIN Production.ProcessingPlants pp ON pp.PlantId  = pb.PlantId
                INNER JOIN Production.Products         p  ON p.ProductId = pb.ProductId
                LEFT  JOIN Finance.MilkTypes           mt ON mt.MilkTypeId = pb.MilkTypeId
                WHERE pb.ProductionBatchId = @ProductionBatchId";

            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("@ProductionBatchId", productionBatchId);
                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                        batch = MapProductionBatch(reader);
                }
            }
            return batch;
        }

        private ProductionBatchModel MapProductionBatch(SqlDataReader reader)
        {
            return new ProductionBatchModel
            {
                ProductionBatchId = Convert.ToInt32(reader["ProductionBatchId"]),
                PlantId = Convert.ToInt32(reader["PlantId"]),
                ProductId = Convert.ToInt32(reader["ProductId"]),
                MilkUsedQuantity = Convert.ToDecimal(reader["MilkUsedQuantity"]),
                ProductionDate = Convert.ToDateTime(reader["ProductionDate"]),
                BatchStatus = reader["BatchStatus"].ToString(),
                MilkTypeId = reader["MilkTypeId"] == DBNull.Value ? 0 : Convert.ToInt32(reader["MilkTypeId"]),
                PlantName = reader["PlantName"].ToString(),
                ProductName = reader["ProductName"].ToString(),
                ProductType = reader["ProductType"].ToString(),
                Unit = reader["Unit"].ToString(),
                MilkTypeName = reader["MilkTypeName"] == DBNull.Value ? null : reader["MilkTypeName"].ToString()
            };
        }

        // ════════════════════════════════════════════════════════
        // PRODUCT WASTAGE  (finished-goods wastage)
        // ════════════════════════════════════════════════════════

        public List<BatchForWastageModel> GetBatchesForWastage()
        {
            var list = new List<BatchForWastageModel>();

            string query = @"
                SELECT
                    pb.ProductionBatchId,
                    'PB-' + CAST(pb.ProductionBatchId AS VARCHAR)
                        + ' | ' + p.ProductName
                        + ' | ' + pp.PlantName
                        + ' | ' + pb.BatchStatus  AS DisplayText
                FROM Production.ProductionBatches pb
                INNER JOIN Production.Products         p  ON p.ProductId  = pb.ProductId
                INNER JOIN Production.ProcessingPlants pp ON pp.PlantId   = pb.PlantId
                WHERE pb.BatchStatus IN ('InProgress', 'Completed')
                ORDER BY pb.ProductionDate DESC";

            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.CommandType = CommandType.Text;
                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new BatchForWastageModel
                        {
                            BatchId = Convert.ToInt32(reader["ProductionBatchId"]),
                            DisplayText = reader["DisplayText"].ToString()
                        });
                    }
                }
            }

            return list;
        }

        public int AddProductWastage(int batchId, int productId, decimal quantity, string reason)
        {
            string query = @"
                INSERT INTO Production.ProductWastage (BatchId, ProductId, Quantity, Reason, RecordedDate)
                VALUES (@BatchId, @ProductId, @Quantity, @Reason, GETDATE());
                SELECT SCOPE_IDENTITY();";

            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("@BatchId", batchId);
                cmd.Parameters.AddWithValue("@ProductId", productId);
                cmd.Parameters.AddWithValue("@Quantity", quantity);
                cmd.Parameters.AddWithValue("@Reason", (object?)reason ?? DBNull.Value);
                con.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public List<ProductWastageModel> GetAllProductWastage(int? plantId = null)
        {
            var list = new List<ProductWastageModel>();

            string query = @"
                SELECT
                    pw.WastageId,
                    pw.BatchId,
                    pw.ProductId,
                    pw.Quantity,
                    pw.Reason,
                    pw.RecordedDate,
                    p.ProductName,
                    p.ProductType,
                    p.Unit,
                    pp.PlantName,
                    pb.BatchStatus
                FROM Production.ProductWastage pw
                INNER JOIN Production.ProductionBatches pb ON pb.ProductionBatchId = pw.BatchId
                INNER JOIN Production.Products          p  ON p.ProductId          = pw.ProductId
                INNER JOIN Production.ProcessingPlants  pp ON pp.PlantId           = pb.PlantId
                WHERE (@PlantId IS NULL OR pb.PlantId = @PlantId)
                ORDER BY pw.RecordedDate DESC";

            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("@PlantId", (object?)plantId ?? DBNull.Value);
                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        list.Add(MapWastage(reader));
                }
            }

            return list;
        }

        public List<ProductWastageModel> GetWastageByBatch(int batchId)
        {
            var list = new List<ProductWastageModel>();

            string query = @"
                SELECT
                    pw.WastageId,
                    pw.BatchId,
                    pw.ProductId,
                    pw.Quantity,
                    pw.Reason,
                    pw.RecordedDate,
                    p.ProductName,
                    p.ProductType,
                    p.Unit,
                    pp.PlantName,
                    pb.BatchStatus
                FROM Production.ProductWastage pw
                INNER JOIN Production.ProductionBatches pb ON pb.ProductionBatchId = pw.BatchId
                INNER JOIN Production.Products          p  ON p.ProductId          = pw.ProductId
                INNER JOIN Production.ProcessingPlants  pp ON pp.PlantId           = pb.PlantId
                WHERE pw.BatchId = @BatchId
                ORDER BY pw.RecordedDate DESC";

            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("@BatchId", batchId);
                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        list.Add(MapWastage(reader));
                }
            }

            return list;
        }

        private ProductWastageModel MapWastage(SqlDataReader reader)
        {
            return new ProductWastageModel
            {
                WastageId = Convert.ToInt32(reader["WastageId"]),
                BatchId = Convert.ToInt32(reader["BatchId"]),
                ProductId = Convert.ToInt32(reader["ProductId"]),
                Quantity = Convert.ToDecimal(reader["Quantity"]),
                Reason = reader["Reason"] == DBNull.Value ? null : reader["Reason"].ToString(),
                RecordedDate = Convert.ToDateTime(reader["RecordedDate"]),
                ProductName = reader["ProductName"].ToString(),
                ProductType = reader["ProductType"].ToString(),
                Unit = reader["Unit"].ToString(),
                PlantName = reader["PlantName"].ToString(),
                BatchStatus = reader["BatchStatus"].ToString()
            };
        }

        // ════════════════════════════════════════════════════════
        // MILK PROCESS WASTAGE  (raw-milk lost during production)
        // ════════════════════════════════════════════════════════

        public int AddMilkProcessWastage(int productionBatchId, int plantId, int milkTypeId,
                                          decimal wastageQuantity, string reason)
        {
            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand("Production.usp_Production_RecordMilkProcessWastage", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ProductionBatchId", productionBatchId);
                cmd.Parameters.AddWithValue("@PlantId", plantId);
                cmd.Parameters.AddWithValue("@MilkTypeId", milkTypeId);
                cmd.Parameters.AddWithValue("@WastageQuantity", wastageQuantity);
                cmd.Parameters.AddWithValue("@WastageType", "ProcessWastage");
                cmd.Parameters.AddWithValue("@Reason", (object?)reason ?? DBNull.Value);
                con.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public List<MilkProcessWastageModel> GetAllMilkProcessWastage(int? plantId = null)
        {
            var list = new List<MilkProcessWastageModel>();

            string query = @"
                SELECT
                    mpw.WastageId,
                    mpw.ProductionBatchId,
                    mpw.PlantId,
                    mpw.MilkTypeId,
                    mpw.WastageQuantity,
                    mpw.WastageType,
                    mpw.Reason,
                    mpw.RecordedDate,
                    pp.PlantName,
                    mt.MilkTypeName,
                    pb.BatchStatus,
                    pb.ProductionDate
                FROM Production.MilkProcessWastage mpw
                INNER JOIN Production.ProcessingPlants  pp ON pp.PlantId           = mpw.PlantId
                INNER JOIN Finance.MilkTypes            mt ON mt.MilkTypeId        = mpw.MilkTypeId
                INNER JOIN Production.ProductionBatches pb ON pb.ProductionBatchId = mpw.ProductionBatchId
                WHERE (@PlantId IS NULL OR mpw.PlantId = @PlantId)
                ORDER BY mpw.RecordedDate DESC";

            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("@PlantId", (object?)plantId ?? DBNull.Value);
                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new MilkProcessWastageModel
                        {
                            WastageId = Convert.ToInt32(reader["WastageId"]),
                            ProductionBatchId = Convert.ToInt32(reader["ProductionBatchId"]),
                            PlantId = Convert.ToInt32(reader["PlantId"]),
                            MilkTypeId = Convert.ToInt32(reader["MilkTypeId"]),
                            WastageQuantity = Convert.ToDecimal(reader["WastageQuantity"]),
                            WastageType = reader["WastageType"].ToString(),
                            Reason = reader["Reason"] == DBNull.Value ? null : reader["Reason"].ToString(),
                            RecordedDate = Convert.ToDateTime(reader["RecordedDate"]),
                            PlantName = reader["PlantName"].ToString(),
                            MilkTypeName = reader["MilkTypeName"].ToString(),
                            BatchStatus = reader["BatchStatus"].ToString(),
                            ProductionDate = Convert.ToDateTime(reader["ProductionDate"])
                        });
                    }
                }
            }

            return list;
        }

        // ════════════════════════════════════════════════════════
        // TRANSFER QUALITY TESTS
        // ════════════════════════════════════════════════════════

        public List<QualityTestModel> GetAllQualityTests(int? plantId = null)
        {
            var list = new List<QualityTestModel>();

            string query = @"
                SELECT
                    tqt.TestId,
                    tqt.TransferId,
                    tqt.TestedFat,
                    tqt.TestedCLR,
                    tqt.TestDate,
                    mt.DispatchQty,
                    mt.ReceivedQty,
                    mt.DispatchDate,
                    pp.PlantName,
                    cc.CenterName,
                    v.VehicleNumber,
                    d.DriverName,
                    cb.AvgFat   AS BatchAvgFat,
                    cb.AvgCLR   AS BatchAvgCLR
                FROM Production.TransferQualityTests tqt
                INNER JOIN Production.MilkTransfers         mt  ON mt.TransferId  = tqt.TransferId
                INNER JOIN Production.ProcessingPlants       pp  ON pp.PlantId     = mt.PlantId
                INNER JOIN Logistics.VehiclesNew             v   ON v.VehicleId    = mt.VehicleId
                INNER JOIN Logistics.DriversNew              d   ON d.DriverId     = v.DriverId
                INNER JOIN Collection.CollectionBatches      cb  ON cb.BatchId     = mt.BatchId
                INNER JOIN Collection.CollectionCenters      cc  ON cc.CenterId    = cb.CenterId
                WHERE (@PlantId IS NULL OR mt.PlantId = @PlantId)
                ORDER BY tqt.TestDate DESC";

            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("@PlantId", (object?)plantId ?? DBNull.Value);
                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        list.Add(MapQualityTest(reader));
                }
            }

            return list;
        }

        public QualityTestModel GetQualityTestByTransfer(int transferId)
        {
            string query = @"
                SELECT
                    tqt.TestId,
                    tqt.TransferId,
                    tqt.TestedFat,
                    tqt.TestedCLR,
                    tqt.TestDate,
                    mt.DispatchQty,
                    mt.ReceivedQty,
                    mt.DispatchDate,
                    pp.PlantName,
                    cc.CenterName,
                    v.VehicleNumber,
                    d.DriverName,
                    cb.AvgFat   AS BatchAvgFat,
                    cb.AvgCLR   AS BatchAvgCLR
                FROM Production.TransferQualityTests tqt
                INNER JOIN Production.MilkTransfers         mt  ON mt.TransferId  = tqt.TransferId
                INNER JOIN Production.ProcessingPlants       pp  ON pp.PlantId     = mt.PlantId
                INNER JOIN Logistics.VehiclesNew             v   ON v.VehicleId    = mt.VehicleId
                INNER JOIN Logistics.DriversNew              d   ON d.DriverId     = v.DriverId
                INNER JOIN Collection.CollectionBatches      cb  ON cb.BatchId     = mt.BatchId
                INNER JOIN Collection.CollectionCenters      cc  ON cc.CenterId    = cb.CenterId
                WHERE tqt.TransferId = @TransferId";

            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("@TransferId", transferId);
                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                        return MapQualityTest(reader);
                }
            }

            return null;
        }

        public int AddQualityTest(int transferId, decimal testedFat, decimal testedCLR, DateTime testDate)
        {
            string checkQuery = @"
                SELECT COUNT(1) FROM Production.TransferQualityTests
                WHERE TransferId = @TransferId";

            string insertQuery = @"
                INSERT INTO Production.TransferQualityTests
                    (TransferId, TestedFat, TestedCLR, TestDate)
                VALUES
                    (@TransferId, @TestedFat, @TestedCLR, @TestDate);
                SELECT SCOPE_IDENTITY();";

            using (SqlConnection con = _db.GetConnection())
            {
                con.Open();

                using (SqlCommand chk = new SqlCommand(checkQuery, con))
                {
                    chk.Parameters.AddWithValue("@TransferId", transferId);
                    int count = (int)chk.ExecuteScalar();
                    if (count > 0)
                        throw new InvalidOperationException(
                            $"A quality test already exists for Transfer #{transferId}.");
                }

                using (SqlCommand cmd = new SqlCommand(insertQuery, con))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@TransferId", transferId);
                    cmd.Parameters.AddWithValue("@TestedFat", testedFat);
                    cmd.Parameters.AddWithValue("@TestedCLR", testedCLR);
                    cmd.Parameters.AddWithValue("@TestDate", testDate.Date);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        private QualityTestModel MapQualityTest(SqlDataReader reader)
        {
            return new QualityTestModel
            {
                TestId = Convert.ToInt32(reader["TestId"]),
                TransferId = Convert.ToInt32(reader["TransferId"]),
                TestedFat = reader["TestedFat"] == DBNull.Value ? null : Convert.ToDecimal(reader["TestedFat"]),
                TestedCLR = reader["TestedCLR"] == DBNull.Value ? null : Convert.ToDecimal(reader["TestedCLR"]),
                TestDate = Convert.ToDateTime(reader["TestDate"]),
                DispatchQty = Convert.ToDecimal(reader["DispatchQty"]),
                ReceivedQty = reader["ReceivedQty"] == DBNull.Value ? null : Convert.ToDecimal(reader["ReceivedQty"]),
                DispatchDate = Convert.ToDateTime(reader["DispatchDate"]),
                PlantName = reader["PlantName"].ToString(),
                CenterName = reader["CenterName"].ToString(),
                VehicleNumber = reader["VehicleNumber"].ToString(),
                DriverName = reader["DriverName"].ToString(),
                BatchAvgFat = reader["BatchAvgFat"] == DBNull.Value ? null : Convert.ToDecimal(reader["BatchAvgFat"]),
                BatchAvgCLR = reader["BatchAvgCLR"] == DBNull.Value ? null : Convert.ToDecimal(reader["BatchAvgCLR"]),
            };
        }

        // ════════════════════════════════════════════════════════
        // TRANSFER LOSS LOG
        // ════════════════════════════════════════════════════════

        public List<TransferLossLogModel> GetTransferLossLog(int? plantId = null)
        {
            var list = new List<TransferLossLogModel>();

            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand("Production.usp_Production_GetTransferLossLog", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@PlantId", (object?)plantId ?? DBNull.Value);
                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        list.Add(MapLossLog(reader));
                }
            }

            return list;
        }

        public List<LossSummaryModel> GetLossSummary(int? plantId = null)
        {
            var list = new List<LossSummaryModel>();

            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand("Production.usp_Production_GetLossSummary", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@PlantId", (object?)plantId ?? DBNull.Value);
                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new LossSummaryModel
                        {
                            PlantName = reader["PlantName"].ToString(),
                            TotalLossEvents = Convert.ToInt32(reader["TotalLossEvents"]),
                            TotalLossLitres = Convert.ToDecimal(reader["TotalLossLitres"]),
                            AvgLossPct = Convert.ToDecimal(reader["AvgLossPct"]),
                            MaxSingleLoss = Convert.ToDecimal(reader["MaxSingleLoss"]),
                            SevereCount = Convert.ToInt32(reader["SevereCount"]),
                            ModerateCount = Convert.ToInt32(reader["ModerateCount"]),
                            MinorCount = Convert.ToInt32(reader["MinorCount"])
                        });
                    }
                }
            }

            return list;
        }

        private TransferLossLogModel MapLossLog(SqlDataReader reader)
        {
            return new TransferLossLogModel
            {
                LossLogId = Convert.ToInt32(reader["LossLogId"]),
                TransferId = Convert.ToInt32(reader["TransferId"]),
                DispatchQty = Convert.ToDecimal(reader["DispatchQty"]),
                ReceivedQty = Convert.ToDecimal(reader["ReceivedQty"]),
                LossQty = Convert.ToDecimal(reader["LossQty"]),
                LossPct = Convert.ToDecimal(reader["LossPct"]),
                LossCategory = reader["LossCategory"].ToString(),
                RecordedAt = Convert.ToDateTime(reader["RecordedAt"]),
                PlantName = reader["PlantName"].ToString(),
                CenterName = reader["CenterName"].ToString(),
                Shift = reader["Shift"].ToString(),
                BatchDate = Convert.ToDateTime(reader["BatchDate"]),
                VehicleNumber = reader["VehicleNumber"].ToString(),
                DriverName = reader["DriverName"].ToString(),
                DispatchDate = Convert.ToDateTime(reader["DispatchDate"]),
                ReceivedDate = reader["ReceivedDate"] == DBNull.Value
                                    ? null
                                    : Convert.ToDateTime(reader["ReceivedDate"])
            };
        }
    }
}