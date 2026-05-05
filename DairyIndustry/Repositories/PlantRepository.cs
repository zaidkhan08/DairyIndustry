using DairyIndustry.Data;
using DairyIndustry.Models.Production;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DairyIndustry.Repositories
{
    public class PlantRepository:IPlantRepository
    {
        private readonly DbHelper _dbHelper;

        public PlantRepository(DbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }
        // ─────────────────────────────────────────────
        // GET PLANT ID — returns INT
        // Called by AdminController Login
        // ─────────────────────────────────────────────
        public int GetPlantIdByStaffId(int? staffId)
        {
            if (staffId == null || staffId == 0) return 0;

            using var con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand(@"
                SELECT ISNULL(PlantId, 3)
                FROM HR.Staffs
                WHERE StaffId  = @StaffId
                  AND IsActive = 1
            ", con);

            cmd.Parameters.AddWithValue("@StaffId", staffId.Value);

            con.Open();
            var result = cmd.ExecuteScalar();

            return result == null || result == DBNull.Value
                ? 0
                : Convert.ToInt32(result);
        }

        // ─────────────────────────────────────────────
        // GET PLANT DETAILS — returns PlantStaffModel
        // Called by PlantController Dashboard
        // FIX: Plant.Plants → Production.ProcessingPlants
        // FIX: StaffName    → FirstName + LastName
        // ─────────────────────────────────────────────
        public PlantStaffModel GetPlantByStaffId(int staffId)
        {
            var result = new PlantStaffModel();

            using var con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand(@"
                SELECT
                    s.StaffId,
                    s.FirstName + ' ' + s.LastName AS StaffName,
                    s.ProfilePhoto,
                    p.PlantId,
                    p.PlantName,
                    p.Location AS PlantLocation
                FROM HR.Staffs s
                LEFT JOIN Production.ProcessingPlants p
                    ON p.PlantId = s.PlantId
                WHERE s.StaffId  = @StaffId
                  AND s.IsActive = 1
            ", con);

            cmd.Parameters.AddWithValue("@StaffId", staffId);

            con.Open();
            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                result.StaffId = Convert.ToInt32(reader["StaffId"]);
                result.StaffName = reader["StaffName"]?.ToString();
                result.ProfilePhoto = reader["ProfilePhoto"]?.ToString();
                result.PlantId = reader["PlantId"] == DBNull.Value
                                       ? 0
                                       : Convert.ToInt32(reader["PlantId"]);
                result.PlantName = reader["PlantName"]?.ToString();
                result.PlantLocation = reader["PlantLocation"]?.ToString();
            }

            return result;
        }

        // ─────────────────────────────────────────────
        // GET ALL TRANSFERS FOR A PLANT
        // Shows both In Transit + Received
        // ─────────────────────────────────────────────
        public List<PlantTransferViewModel> GetTransfersByPlant(int plantId)
        {
            var list = new List<PlantTransferViewModel>();

            using var con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand(
                "Production.usp_GetTransfersByPlant", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@PlantId", plantId);

            con.Open();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new PlantTransferViewModel
                {
                    TransferId = Convert.ToInt32(reader["TransferId"]),
                    DispatchDate = Convert.ToDateTime(reader["DispatchDate"]),
                    DispatchQty = Convert.ToDecimal(reader["DispatchQty"]),
                    ReceivedQty = reader["ReceivedQty"] == DBNull.Value
                                     ? null
                                     : Convert.ToDecimal(reader["ReceivedQty"]),
                    LossQty = reader["LossQty"] == DBNull.Value
                                     ? null
                                     : Convert.ToDecimal(reader["LossQty"]),
                    ReceivedDate = reader["ReceivedDate"] == DBNull.Value
                                     ? null
                                     : Convert.ToDateTime(reader["ReceivedDate"]),
                    BatchId = Convert.ToInt32(reader["BatchId"]),
                    BatchDate = Convert.ToDateTime(reader["BatchDate"]),
                    Shift = reader["Shift"]?.ToString(),
                    CenterName = reader["CenterName"]?.ToString(),
                    PlantName = reader["PlantName"]?.ToString(),
                    VehicleNumber = reader["VehicleNumber"]?.ToString(),
                    DriverName = reader["DriverName"]?.ToString(),
                    MilkTypeName = reader["MilkTypeName"]?.ToString(),
                    TransferStatus = reader["TransferStatus"]?.ToString()
                });
            }

            return list;
        }

        // ─────────────────────────────────────────────
        // GET SINGLE TRANSFER BY ID
        // Used by ReceiveMilk GET
        // ─────────────────────────────────────────────
        public PlantTransferViewModel GetTransferById(int transferId)
        {
            using var con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand(@"
                SELECT
                    mt.TransferId,
                    mt.DispatchDate,
                    mt.DispatchQty,
                    mt.ReceivedQty,
                    mt.LossQty,
                    mt.ReceivedDate,
                    cb.BatchId,
                    cb.BatchDate,
                    cb.Shift,
                    cc.CenterName,
                    pp.PlantName,
                    v.VehicleNumber,
                    d.DriverName,
                    mtype.MilkTypeName,
                    CASE
                        WHEN mt.ReceivedQty IS NOT NULL
                        THEN 'Received'
                        ELSE 'In Transit'
                    END AS TransferStatus
                FROM Production.MilkTransfers mt
                INNER JOIN Collection.CollectionBatches cb
                    ON cb.BatchId       = mt.BatchId
                INNER JOIN Collection.CollectionCenters cc
                    ON cc.CenterId      = cb.CenterId
                INNER JOIN Production.ProcessingPlants pp
                    ON pp.PlantId       = mt.PlantId
                INNER JOIN Logistics.Vehicles v
                    ON v.VehicleId      = mt.VehicleId
                LEFT  JOIN Logistics.Drivers d
                    ON d.DriverId       = v.DriverId
                LEFT  JOIN Finance.MilkTypes mtype
                    ON mtype.MilkTypeId = mt.MilkTypeId
                WHERE mt.TransferId = @TransferId
            ", con);

            cmd.Parameters.AddWithValue("@TransferId", transferId);

            con.Open();
            using var reader = cmd.ExecuteReader();

            if (!reader.Read()) return null;

            return new PlantTransferViewModel
            {
                TransferId = Convert.ToInt32(reader["TransferId"]),
                DispatchDate = Convert.ToDateTime(reader["DispatchDate"]),
                DispatchQty = Convert.ToDecimal(reader["DispatchQty"]),
                ReceivedQty = reader["ReceivedQty"] == DBNull.Value
                                 ? null
                                 : Convert.ToDecimal(reader["ReceivedQty"]),
                LossQty = reader["LossQty"] == DBNull.Value
                                 ? null
                                 : Convert.ToDecimal(reader["LossQty"]),
                ReceivedDate = reader["ReceivedDate"] == DBNull.Value
                                 ? null
                                 : Convert.ToDateTime(reader["ReceivedDate"]),
                BatchId = Convert.ToInt32(reader["BatchId"]),
                BatchDate = Convert.ToDateTime(reader["BatchDate"]),
                Shift = reader["Shift"]?.ToString(),
                CenterName = reader["CenterName"]?.ToString(),
                PlantName = reader["PlantName"]?.ToString(),
                VehicleNumber = reader["VehicleNumber"]?.ToString(),
                DriverName = reader["DriverName"]?.ToString(),
                MilkTypeName = reader["MilkTypeName"]?.ToString(),
                TransferStatus = reader["TransferStatus"]?.ToString()
            };
        }

        // ─────────────────────────────────────────────
        // RECEIVE MILK TRANSFER
        // Saves received qty, calculates loss,
        // updates RawMilkInventory at plant
        // ─────────────────────────────────────────────
        public int ReceiveMilkTransfer(
            int transferId,
            decimal receivedQty,
            DateTime receivedDate)
        {
            using var con = _dbHelper.GetConnection();
            using var cmd = new SqlCommand(
                "Production.usp_ReceiveMilkTransfer", con);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@TransferId", transferId);
            cmd.Parameters.AddWithValue("@ReceivedQty", receivedQty);
            cmd.Parameters.AddWithValue("@ReceivedDate", receivedDate.Date);

            try
            {
                con.Open();
                using var reader = cmd.ExecuteReader();

                if (reader.Read())
                    return Convert.ToInt32(reader["TransferId"]);

                return 0;
            }
            catch (SqlException ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
