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

            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Logistics.usp_Logistics_GetVehicles", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new VehiclesModel
                            {
                                VehicleId = Convert.ToInt32(reader["VehicleId"]),
                                VehicleNumber = reader["VehicleNumber"].ToString(),
                                Capacity = reader["Capacity"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Capacity"]),
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
    }
}