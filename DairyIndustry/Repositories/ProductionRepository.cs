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
            {
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
                FROM Logistics.Vehicles v
                LEFT JOIN Logistics.Drivers d ON d.DriverId = v.DriverId
                ORDER BY v.VehicleNumber";

            using (SqlConnection con = _db.GetConnection())
            {
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
            }

            return list;
        }

        // ════════════════════════════════════════════════════════
        // DISPATCH — calls SP 6.2
        // ════════════════════════════════════════════════════════

        public int DispatchMilkTransfer(int batchId, int vehicleId, int plantId,
                                        decimal dispatchQty, DateTime dispatchDate)
        {
            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Production.usp_Production_DispatchMilkTransfer", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@BatchId", batchId);
                    cmd.Parameters.AddWithValue("@VehicleId", vehicleId);
                    cmd.Parameters.AddWithValue("@PlantId", plantId);
                    cmd.Parameters.AddWithValue("@DispatchQty", dispatchQty);
                    cmd.Parameters.AddWithValue("@DispatchDate", dispatchDate);

                    con.Open();
                    var result = cmd.ExecuteScalar();
                    return Convert.ToInt32(result);
                }
            }
        }

        // ════════════════════════════════════════════════════════
        // RECEIVE — calls SP 6.3
        // ════════════════════════════════════════════════════════

        public void ReceiveMilkTransfer(int transferId, decimal receivedQty, DateTime receivedDate)
        {
            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Production.usp_Production_ReceiveMilkTransfer", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@TransferId", transferId);
                    cmd.Parameters.AddWithValue("@ReceivedQty", receivedQty);
                    cmd.Parameters.AddWithValue("@ReceivedDate", receivedDate);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ════════════════════════════════════════════════════════
        // GET ALL TRANSFERS
        // ════════════════════════════════════════════════════════

        public List<MilkTransferModel> GetAllTransfers()
        {
            var list = new List<MilkTransferModel>();

            string query = @"
                SELECT
                    mt.TransferId,
                    mt.BatchId,
                    mt.VehicleId,
                    mt.PlantId,
                    mt.DispatchQty,
                    mt.ReceivedQty,
                    mt.LossQty,
                    mt.DispatchDate,
                    mt.ReceivedDate,
                    'B-' + CAST(cb.BatchId AS VARCHAR) AS BatchRef,
                    cc.CenterName,
                    pp.PlantName,
                    v.VehicleNumber,
                    d.DriverName
                FROM Production.MilkTransfers mt
                INNER JOIN Collection.CollectionBatches    cb ON cb.BatchId  = mt.BatchId
                INNER JOIN Collection.CollectionCenters    cc ON cc.CenterId = cb.CenterId
                INNER JOIN Production.ProcessingPlants     pp ON pp.PlantId  = mt.PlantId
                INNER JOIN Logistics.Vehicles               v  ON v.VehicleId = mt.VehicleId
                LEFT  JOIN Logistics.Drivers                d  ON d.DriverId  = v.DriverId
                ORDER BY mt.DispatchDate DESC";

            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.CommandType = CommandType.Text;
                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            list.Add(MapTransfer(reader));
                    }
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
                    mt.DispatchQty,
                    mt.ReceivedQty,
                    mt.LossQty,
                    mt.DispatchDate,
                    mt.ReceivedDate,
                    'B-' + CAST(cb.BatchId AS VARCHAR) AS BatchRef,
                    cc.CenterName,
                    pp.PlantName,
                    v.VehicleNumber,
                    d.DriverName
                FROM Production.MilkTransfers mt
                INNER JOIN Collection.CollectionBatches    cb ON cb.BatchId  = mt.BatchId
                INNER JOIN Collection.CollectionCenters    cc ON cc.CenterId = cb.CenterId
                INNER JOIN Production.ProcessingPlants     pp ON pp.PlantId  = mt.PlantId
                INNER JOIN Logistics.Vehicles               v  ON v.VehicleId = mt.VehicleId
                LEFT  JOIN Logistics.Drivers                d  ON d.DriverId  = v.DriverId
                WHERE mt.TransferId = @TransferId";

            using (SqlConnection con = _db.GetConnection())
            {
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
            }

            return transfer;
        }

        // ════════════════════════════════════════════════════════
        // PRIVATE HELPER
        // ════════════════════════════════════════════════════════

        private MilkTransferModel MapTransfer(SqlDataReader reader)
        {
            return new MilkTransferModel
            {
                TransferId = Convert.ToInt32(reader["TransferId"]),
                BatchId = Convert.ToInt32(reader["BatchId"]),
                VehicleId = Convert.ToInt32(reader["VehicleId"]),
                PlantId = Convert.ToInt32(reader["PlantId"]),
                DispatchQty = Convert.ToDecimal(reader["DispatchQty"]),
                ReceivedQty = reader["ReceivedQty"] == DBNull.Value ? null : Convert.ToDecimal(reader["ReceivedQty"]),
                LossQty = reader["LossQty"] == DBNull.Value ? null : Convert.ToDecimal(reader["LossQty"]),
                DispatchDate = Convert.ToDateTime(reader["DispatchDate"]),
                ReceivedDate = reader["ReceivedDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["ReceivedDate"]),
                BatchRef = reader["BatchRef"].ToString(),
                CenterName = reader["CenterName"].ToString(),
                PlantName = reader["PlantName"].ToString(),
                VehicleNumber = reader["VehicleNumber"].ToString(),
                DriverName = reader["DriverName"] == DBNull.Value ? null : reader["DriverName"].ToString()
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
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@ProductName", productName);
                    cmd.Parameters.AddWithValue("@ProductType", productType);
                    cmd.Parameters.AddWithValue("@MRP", mrp);
                    cmd.Parameters.AddWithValue("@Unit", unit);
                    cmd.Parameters.AddWithValue("@ShelfLifeDays", (object?)shelfLifeDays ?? DBNull.Value);
                    con.Open();
                    var result = cmd.ExecuteScalar();
                    return Convert.ToInt32(result);
                }
            }
        }

        public List<ProductModel> GetAllProducts()
        {
            var list = new List<ProductModel>();
            string query = "SELECT ProductId, ProductName, ProductType, MRP, Unit, ShelfLifeDays FROM Production.Products ORDER BY ProductName";

            using (SqlConnection con = _db.GetConnection())
            {
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
            }
            return list;
        }

        public ProductModel GetProductById(int productId)
        {
            ProductModel product = null;
            string query = "SELECT ProductId, ProductName, ProductType, MRP, Unit, ShelfLifeDays FROM Production.Products WHERE ProductId = @ProductId";

            using (SqlConnection con = _db.GetConnection())
            {
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
            {
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
        }

        public void DeleteProduct(int productId)
        {
            string query = "DELETE FROM Production.Products WHERE ProductId = @ProductId";

            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@ProductId", productId);
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
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
        // PRODUCTION BATCHES
        // ════════════════════════════════════════════════════════

        public int StartProductionBatch(int plantId, int productId,
                                        decimal milkUsedQuantity, DateTime productionDate)
        {
            // Check available raw milk at plant first
            string checkQuery = "SELECT ISNULL(SUM(Quantity), 0) FROM Production.RawMilkInventory WHERE PlantId = @PlantId";
            string insertQuery = @"
                INSERT INTO Production.ProductionBatches (PlantId, ProductId, MilkUsedQuantity, ProductionDate, BatchStatus)
                VALUES (@PlantId, @ProductId, @MilkUsedQuantity, @ProductionDate, 'InProgress');
                SELECT SCOPE_IDENTITY();";
            string deductQuery = @"
                UPDATE TOP(1) Production.RawMilkInventory
                SET Quantity = Quantity - @MilkUsedQuantity, LastUpdated = GETDATE()
                WHERE RawMilkInventoryId = (
                    SELECT TOP 1 RawMilkInventoryId FROM Production.RawMilkInventory
                    WHERE PlantId = @PlantId ORDER BY LastUpdated ASC
                )";

            using (SqlConnection con = _db.GetConnection())
            {
                con.Open();

                // Validate inventory
                using (SqlCommand chk = new SqlCommand(checkQuery, con))
                {
                    chk.Parameters.AddWithValue("@PlantId", plantId);
                    decimal available = Convert.ToDecimal(chk.ExecuteScalar());
                    if (available < milkUsedQuantity)
                        throw new InvalidOperationException($"Insufficient raw milk at plant. Available: {available} L, Required: {milkUsedQuantity} L");
                }

                // Insert batch
                int newId;
                using (SqlCommand ins = new SqlCommand(insertQuery, con))
                {
                    ins.Parameters.AddWithValue("@PlantId", plantId);
                    ins.Parameters.AddWithValue("@ProductId", productId);
                    ins.Parameters.AddWithValue("@MilkUsedQuantity", milkUsedQuantity);
                    ins.Parameters.AddWithValue("@ProductionDate", productionDate);
                    newId = Convert.ToInt32(ins.ExecuteScalar());
                }

                // Deduct from inventory
                using (SqlCommand ded = new SqlCommand(deductQuery, con))
                {
                    ded.Parameters.AddWithValue("@PlantId", plantId);
                    ded.Parameters.AddWithValue("@MilkUsedQuantity", milkUsedQuantity);
                    ded.ExecuteNonQuery();
                }

                return newId;
            }
        }

        public void UpdateBatchStatus(int productionBatchId, string batchStatus)
        {
            string query = @"
                UPDATE Production.ProductionBatches
                SET BatchStatus = @BatchStatus
                WHERE ProductionBatchId = @ProductionBatchId
                  AND BatchStatus = 'InProgress'";

            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@ProductionBatchId", productionBatchId);
                    cmd.Parameters.AddWithValue("@BatchStatus", batchStatus);
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<ProductionBatchModel> GetAllProductionBatches()
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
                    pp.PlantName,
                    p.ProductName,
                    p.ProductType,
                    p.Unit
                FROM Production.ProductionBatches pb
                INNER JOIN Production.ProcessingPlants pp ON pp.PlantId  = pb.PlantId
                INNER JOIN Production.Products         p  ON p.ProductId = pb.ProductId
                ORDER BY pb.ProductionDate DESC";

            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.CommandType = CommandType.Text;
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            list.Add(MapProductionBatch(reader));
                    }
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
                    pp.PlantName,
                    p.ProductName,
                    p.ProductType,
                    p.Unit
                FROM Production.ProductionBatches pb
                INNER JOIN Production.ProcessingPlants pp ON pp.PlantId  = pb.PlantId
                INNER JOIN Production.Products         p  ON p.ProductId = pb.ProductId
                WHERE pb.ProductionBatchId = @ProductionBatchId";

            using (SqlConnection con = _db.GetConnection())
            {
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
                PlantName = reader["PlantName"].ToString(),
                ProductName = reader["ProductName"].ToString(),
                ProductType = reader["ProductType"].ToString(),
                Unit = reader["Unit"].ToString()
            };
        }
    }
}