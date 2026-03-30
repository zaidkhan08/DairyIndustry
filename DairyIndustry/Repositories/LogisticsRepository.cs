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

    }
}
