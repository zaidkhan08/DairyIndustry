using DairyIndustry.Data;
using DairyIndustry.Models.Collection;
using DairyIndustry.Models.FarmerModel;
using DairyIndustry.Repositories;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DairyIndustry.Repository
{
    public class CollectionCenterRepository : ICollectionCenterRepository
    {
        private readonly DbHelper _dbHelper;

        public CollectionCenterRepository(DbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        // ─────────────────────────────────────────────────────────────
        // DASHBOARD  —  get center & staff info for logged-in user
        // ─────────────────────────────────────────────────────────────
        public DashboardViewModel GetCollectionCenterByStaff(int staffId)
        {
            DashboardViewModel model = null;

            using (SqlConnection con = _dbHelper.GetConnection())
            {
                SqlCommand cmd = new SqlCommand(@"
                    SELECT
                        s.FirstName + ' ' + s.LastName AS StaffName,
                        cc.CenterId,
                        cc.CenterName
                    FROM HR.Staffs s
                    LEFT JOIN Collection.StaffCenters sc ON sc.StaffId = s.StaffId
                    LEFT JOIN Collection.CollectionCenters cc ON cc.CenterId = sc.CenterId
                    WHERE s.StaffId = @StaffId", con);

                cmd.Parameters.AddWithValue("@StaffId", staffId);

                con.Open();
                var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    model = new DashboardViewModel
                    {
                        StaffName = reader["StaffName"].ToString(),
                        CenterId = reader["CenterId"] != DBNull.Value ? Convert.ToInt32(reader["CenterId"]) : 0,
                        CenterName = reader["CenterName"]?.ToString() ?? "Not Assigned"
                    };
                }
            }

            return model;
        }

        // ─────────────────────────────────────────────────────────────
        // BATCH STATUS PAGE
        // Calls usp_SyncBatches internally, then returns all 3 shifts
        // for the given center with Open / Closed / Not Started status
        // ─────────────────────────────────────────────────────────────
        public List<BatchStatusViewModel> GetBatchStatus(int centerId)
        {
            var list = new List<BatchStatusViewModel>();

            using (SqlConnection con = _dbHelper.GetConnection())
            {
                // usp_GetBatchStatus already calls usp_SyncBatches first
                SqlCommand cmd = new SqlCommand("Collection.usp_GetBatchStatus", con);
                cmd.CommandType = CommandType.StoredProcedure;

                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // Filter to only this center
                        int rowCenterId = Convert.ToInt32(reader["CenterId"]);
                        if (rowCenterId != centerId) continue;

                        list.Add(new BatchStatusViewModel
                        {
                            CenterId = rowCenterId,
                            CenterName = reader["CenterName"].ToString(),
                            Shift = reader["Shift"].ToString(),
                            ShiftWindow = reader["ShiftWindow"].ToString(),
                            BatchStatus = reader["BatchStatus"].ToString(),
                            BatchId = reader["BatchId"] != DBNull.Value ? Convert.ToInt32(reader["BatchId"]) : null,
                            BatchDate = reader["BatchDate"] != DBNull.Value ? Convert.ToDateTime(reader["BatchDate"]) : null,
                            TotalQuantity = reader["TotalQuantity"] != DBNull.Value ? Convert.ToDecimal(reader["TotalQuantity"]) : 0,
                            AvgFat = reader["AvgFat"] != DBNull.Value ? Convert.ToDecimal(reader["AvgFat"]) : null,
                            AvgCLR = reader["AvgCLR"] != DBNull.Value ? Convert.ToDecimal(reader["AvgCLR"]) : null,
                        });
                    }
                }
            }

            return list;
        }

        // ─────────────────────────────────────────────────────────────
        // MILK ENTRY
        // Calls usp_AddMilkEntry — SP handles:
        //   • usp_SyncBatches (auto open/close)
        //   • CollectionDate  = today only (enforced in SP)
        //   • Shift           = auto-detected from current time (in SP)
        //   • BatchId         = resolved from open batch (in SP)
        //   • Rate lookup, receipt generation, inventory update
        // ─────────────────────────────────────────────────────────────
        public (int collectionId, decimal rate, decimal amount) RecordMilk(
            int farmerId,
            int centerId,
            int milkTypeId,
            string shift,
            decimal quantity,
            decimal fat,
            decimal clr)
        {
            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Collection.usp_AddMilkEntry", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Shift", shift);
                    cmd.Parameters.AddWithValue("@FarmerId", farmerId);
                    cmd.Parameters.AddWithValue("@CenterId", centerId);
                    cmd.Parameters.AddWithValue("@MilkTypeId", milkTypeId);
                    cmd.Parameters.AddWithValue("@Quantity", quantity);
                    cmd.Parameters.AddWithValue("@AppliedFat", fat);
                    cmd.Parameters.AddWithValue("@AppliedCLR", clr);

                    conn.Open();

                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            return (
                                Convert.ToInt32(rdr["CollectionId"]),
                                Convert.ToDecimal(rdr["RatePerLiter"]),
                                Convert.ToDecimal(rdr["Amount"])
                            );
                        }
                    }
                }
            }

            return (0, 0, 0);
        }

        // ─────────────────────────────────────────────────────────────
        // BATCH COLLECTIONS  —  view all entries for a given batch
        // ─────────────────────────────────────────────────────────────
        public List<BatchCollectionView> GetBatchCollections(int batchId)
        {
            var list = new List<BatchCollectionView>();

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Collection.usp_Collection_GetBatchCollections", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@BatchId", batchId);

                    conn.Open();
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            list.Add(new BatchCollectionView
                            {
                                CollectionId = (int)rdr["CollectionId"],
                                CollectionDate = (DateTime)rdr["CollectionDate"],
                                Shift = rdr["Shift"].ToString(),
                                FarmerName = rdr["FarmerName"].ToString(),
                                MilkTypeName = rdr["MilkTypeName"].ToString(),
                                Quantity = (decimal)rdr["Quantity"],
                                AppliedFat = (decimal)rdr["AppliedFat"],
                                AppliedCLR = (decimal)rdr["AppliedCLR"],
                                RatePerLiter = (decimal)rdr["RatePerLiter"],
                                Amount = (decimal)rdr["Amount"]
                            });
                        }
                    }
                }
            }

            return list;
        }

        // ─────────────────────────────────────────────────────────────
        // FARMERS DROPDOWN
        // ─────────────────────────────────────────────────────────────
        public List<FarmerViewModel> GetFarmers()
        {
            var farmers = new List<FarmerViewModel>();

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                SqlCommand cmd = new SqlCommand(
                    "SELECT FarmerId, FarmerName FROM Farmer.Farmers WHERE IsActive = 1", conn);

                conn.Open();
                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        farmers.Add(new FarmerViewModel
                        {
                            FarmerId = (int)rdr["FarmerId"],
                            FarmerName = rdr["FarmerName"].ToString()
                        });
                    }
                }
            }

            return farmers;
        }

        // ─────────────────────────────────────────────────────────────
        // MILK TYPES DROPDOWN
        // ─────────────────────────────────────────────────────────────
        public List<MilkTypes> GetMilkTypes()
        {
            var list = new List<MilkTypes>();

            using (SqlConnection con = _dbHelper.GetConnection())
            {
                SqlCommand cmd = new SqlCommand("Finance.usp_Finance_GetMilkTypes", con);
                cmd.CommandType = CommandType.StoredProcedure;

                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new MilkTypes
                        {
                            MilkTypeId = Convert.ToInt32(reader["MilkTypeId"]),
                            MilkTypeName = reader["MilkTypeName"].ToString()
                        });
                    }
                }
            }

            return list;
        }

        // ─────────────────────────────────────────────────────────────
        // CENTER INVENTORY
        // ─────────────────────────────────────────────────────────────
        public List<CenterInventoryViewModel> GetCenterInventory(int? centerId)
        {
            var list = new List<CenterInventoryViewModel>();

            using (SqlConnection con = _dbHelper.GetConnection())
            {
                SqlCommand cmd = new SqlCommand("Collection.usp_Collection_GetCenterInventory", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@CenterId",
                    centerId.HasValue ? (object)centerId.Value : DBNull.Value);

                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new CenterInventoryViewModel
                        {
                            CenterId = Convert.ToInt32(reader["CenterId"]),
                            CenterName = reader["CenterName"].ToString(),
                            MilkTypeName = reader["MilkTypeName"].ToString(),
                            AvailableQuantity = Convert.ToDecimal(reader["AvailableQuantity"]),
                            LastUpdated = Convert.ToDateTime(reader["LastUpdated"])
                        });
                    }
                }
            }

            return list;
        }
    }
}