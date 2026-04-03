using DairyIndustry.Data;
using DairyIndustry.Models.Admin;
using DairyIndustry.Models.Logistics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DairyIndustry.Repositories
{
    public class LogisticsRepository : ILogisticsRepository
    {
        private readonly DbHelper _db;

        public LogisticsRepository(DbHelper db)
        {
            _db = db;
        }
        public int RegisterDriver(string driverName, string licenseNo,
                          string phone, string username, string passwordHash)
        {
            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Logistics.usp_Logistics_RegisterDriver", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@DriverName", driverName);
                    cmd.Parameters.AddWithValue("@LicenseNo", licenseNo);
                    cmd.Parameters.AddWithValue("@Phone", (object?)phone ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Username", username);
                    cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);

                    con.Open();
                    var result = cmd.ExecuteScalar();
                    return Convert.ToInt32(result);
                }
            }
        }
        public DriversModel GetDriverByUserId(int userId)
        {
            DriversModel driver = null;

            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Logistics.usp_Logistics_GetDriverByUserId", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            driver = new DriversModel
                            {
                                DriverId = Convert.ToInt32(reader["DriverId"]),
                                DriverName = reader["DriverName"].ToString(),
                                LicenseNo = reader["LicenseNo"].ToString(),
                                Phone = reader["Phone"] == DBNull.Value ? null : reader["Phone"].ToString(),
                                Status = reader["Status"].ToString(),
                                RegisteredOn = Convert.ToDateTime(reader["RegisteredOn"]),
                                Username = reader["Username"].ToString(),
                                IsActive = Convert.ToBoolean(reader["IsActive"])
                            };
                        }
                    }
                }
            }
            return driver;
        }
        public List<VehiclesModel> GetVehicleByDriverId(int driverId)
        {
            List<VehiclesModel> vehicles = new List<VehiclesModel>();
            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Logistics.usp_Logistics_GetVehicleByDriverId", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@DriverId", driverId);
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            vehicles.Add(new VehiclesModel
                            {
                                VehicleId = Convert.ToInt32(reader["VehicleId"]),
                                VehicleNumber = reader["VehicleNumber"].ToString(),
                                Capacity = Convert.ToDecimal(reader["Capacity"]),
                                Status = reader["Status"].ToString(),
                                RegisteredOn = Convert.ToDateTime(reader["RegisteredOn"]),
                                DriverName = reader["DriverName"].ToString(),
                                Phone = reader["Phone"].ToString()
                            });
                        }
                    }
                }
            }
            return vehicles;
        }
        public int AddVehicle(int driverId, string vehicleNumber, decimal capacity)
        {
            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Logistics.usp_Logistics_AddVehicle", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@DriverId", driverId);
                    cmd.Parameters.AddWithValue("@VehicleNumber", vehicleNumber);
                    cmd.Parameters.AddWithValue("@Capacity", capacity);

                    con.Open();
                    var result = cmd.ExecuteScalar();
                    return Convert.ToInt32(result);
                }
            }
        }
        public List<VehiclesModel> GetVehiclesByDriverId(int driverId)
        {
            var list = new List<VehiclesModel>();

            using (SqlConnection con = _db.GetConnection())
            {
                string query = @"
            SELECT VehicleId, DriverId, VehicleNumber, 
                   Capacity, Status, RegisteredOn
            FROM Logistics.VehiclesNew
            WHERE DriverId = @DriverId
            ORDER BY RegisteredOn DESC";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@DriverId", driverId);
                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new VehiclesModel
                            {
                                VehicleId = Convert.ToInt32(reader["VehicleId"]),
                                DriverId = Convert.ToInt32(reader["DriverId"]),
                                VehicleNumber = reader["VehicleNumber"].ToString(),
                                Capacity = Convert.ToDecimal(reader["Capacity"]),
                                Status = reader["Status"].ToString(),
                                RegisteredOn = Convert.ToDateTime(reader["RegisteredOn"])
                            });
                        }
                    }
                }
            }

            return list;
        }
        public List<DriversModel> GetAllDrivers()
        {
            var list = new List<DriversModel>();

            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Logistics.usp_Logistics_GetAllDrivers", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new DriversModel
                            {
                                DriverId = Convert.ToInt32(reader["DriverId"]),
                                DriverName = reader["DriverName"].ToString(),
                                LicenseNo = reader["LicenseNo"].ToString(),
                                Phone = reader["Phone"] == DBNull.Value ? null : reader["Phone"].ToString(),
                                Status = reader["Status"].ToString(),
                                RegisteredOn = Convert.ToDateTime(reader["RegisteredOn"]),
                                Username = reader["Username"].ToString(),
                                IsActive = Convert.ToBoolean(reader["IsActive"]),
                                VehicleNumber = reader["VehicleNumber"] == DBNull.Value ? null : reader["VehicleNumber"].ToString(),
                                VehicleStatus = reader["VehicleStatus"] == DBNull.Value ? null : reader["VehicleStatus"].ToString()
                            });
                        }
                    }
                }
            }

            return list;
        }

        public void UpdateDriverStatus(int driverId, string status)
        {
            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Logistics.usp_Logistics_UpdateDriverStatus", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@DriverId", driverId);
                    cmd.Parameters.AddWithValue("@Status", status);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }
        public List<VehiclesModel> GetAllVehicles()
        {
            var list = new List<VehiclesModel>();

            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Logistics.usp_Logistics_GetAllVehicles", con))
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
                                Capacity = Convert.ToDecimal(reader["Capacity"]),
                                Status = reader["Status"].ToString(),
                                RegisteredOn = Convert.ToDateTime(reader["RegisteredOn"]),
                                DriverId = Convert.ToInt32(reader["DriverId"]),
                                DriverName = reader["DriverName"].ToString(),
                                Phone = reader["Phone"] == DBNull.Value ? null : reader["Phone"].ToString(),
                                DriverStatus = reader["DriverStatus"].ToString()
                            });
                        }
                    }
                }
            }

            return list;
        }

        public void UpdateVehicleStatus(int vehicleId, string status)
        {
            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Logistics.usp_Logistics_UpdateVehicleStatus", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@VehicleId", vehicleId);
                    cmd.Parameters.AddWithValue("@Status", status);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }
        public List<MilkTransferModel> GetDriverTransfers(int driverId)
        {
            var list = new List<MilkTransferModel>();

            string query = @"
        SELECT
            mt.TransferId,
            mt.DispatchDate,
            mt.ReceivedDate,
            mt.DispatchQty,
            mt.ReceivedQty,
            mt.LossQty,
            CASE
                WHEN mt.DispatchQty > 0 AND mt.LossQty IS NOT NULL
                THEN ROUND((mt.LossQty / mt.DispatchQty) * 100, 2)
                ELSE 0
            END AS LossPercent,
            CASE
                WHEN mt.ReceivedDate IS NULL THEN 'Pending'
                ELSE 'Received'
            END AS TransferStatus,
            cc.CenterId,
            cc.CenterName,
            pp.PlantId,
            pp.PlantName,
            v.VehicleId,
            v.VehicleNumber,
            d.DriverId,
            d.DriverName,
            d.Phone AS DriverPhone,
            tqt.TestedFat,
            tqt.TestedCLR,
            tqt.TestDate,
            cb.BatchId,
            cb.Shift,
            cb.BatchDate,
            cb.AvgFat AS BatchAvgFat,
            cb.AvgCLR AS BatchAvgCLR
        FROM Production.MilkTransfers mt
        INNER JOIN Collection.CollectionBatches    cb  ON cb.BatchId  = mt.BatchId
        INNER JOIN Collection.CollectionCenters    cc  ON cc.CenterId = cb.CenterId
        INNER JOIN Production.ProcessingPlants     pp  ON pp.PlantId  = mt.PlantId
        INNER JOIN Logistics.VehiclesNew           v   ON v.VehicleId = mt.VehicleId
        INNER JOIN Logistics.DriversNew            d   ON d.DriverId  = v.DriverId
        LEFT  JOIN Production.TransferQualityTests tqt ON tqt.TransferId = mt.TransferId
        WHERE d.DriverId = @DriverId
        ORDER BY mt.DispatchDate DESC";

            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@DriverId", driverId);
                con.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new MilkTransferModel
                        {
                            TransferId = Convert.ToInt32(reader["TransferId"]),
                            DispatchDate = Convert.ToDateTime(reader["DispatchDate"]),
                            ReceivedDate = reader["ReceivedDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["ReceivedDate"]),
                            DispatchQty = Convert.ToDecimal(reader["DispatchQty"]),
                            ReceivedQty = reader["ReceivedQty"] == DBNull.Value ? null : Convert.ToDecimal(reader["ReceivedQty"]),
                            LossQty = reader["LossQty"] == DBNull.Value ? null : Convert.ToDecimal(reader["LossQty"]),
                            LossPercent = Convert.ToDecimal(reader["LossPercent"]),
                            TransferStatus = reader["TransferStatus"].ToString(),
                            CenterId = Convert.ToInt32(reader["CenterId"]),
                            CenterName = reader["CenterName"].ToString(),
                            PlantId = Convert.ToInt32(reader["PlantId"]),
                            PlantName = reader["PlantName"].ToString(),
                            VehicleId = Convert.ToInt32(reader["VehicleId"]),
                            VehicleNumber = reader["VehicleNumber"].ToString(),
                            DriverId = Convert.ToInt32(reader["DriverId"]),
                            DriverName = reader["DriverName"] == DBNull.Value ? null : reader["DriverName"].ToString(),
                            DriverPhone = reader["DriverPhone"] == DBNull.Value ? null : reader["DriverPhone"].ToString(),
                            TestedFat = reader["TestedFat"] == DBNull.Value ? null : Convert.ToDecimal(reader["TestedFat"]),
                            TestedCLR = reader["TestedCLR"] == DBNull.Value ? null : Convert.ToDecimal(reader["TestedCLR"]),
                            TestDate = reader["TestDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["TestDate"]),
                            BatchId = Convert.ToInt32(reader["BatchId"]),
                            Shift = reader["Shift"].ToString(),
                            BatchDate = Convert.ToDateTime(reader["BatchDate"]),
                            BatchAvgFat = reader["BatchAvgFat"] == DBNull.Value ? null : Convert.ToDecimal(reader["BatchAvgFat"]),
                            BatchAvgCLR = reader["BatchAvgCLR"] == DBNull.Value ? null : Convert.ToDecimal(reader["BatchAvgCLR"])
                        });
                    }
                }
            }
            return list;
        }

    }
}
