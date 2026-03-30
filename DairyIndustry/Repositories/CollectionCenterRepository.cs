using DairyIndustry.Data;
using DairyIndustry.Models.Collection;
using DairyIndustry.Models.FarmerModel;
using DairyIndustry.Repositories.Interfaces;
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
        /*
        public CollectionCenter GetCollectionCenterByStaff(int staffId)
        {
            CollectionCenter center = null;

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                string query = @"
                    SELECT c.CenterId, c.CenterName, c.VillageId, c.Capacity, c.Location,
                           s.FirstName, s.LastName
                    FROM Collection.CollectionCenters c
                    INNER JOIN HR.Staffs s ON s.StaffId = @StaffId
                   
                    "; 

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("@StaffId", staffId);

                conn.Open();
                SqlDataReader rdr = cmd.ExecuteReader();

                if (rdr.Read())
                {
                    center = new CollectionCenter
                    {
                        CenterId = (int)rdr["CenterId"],
                        CenterName = rdr["CenterName"].ToString(),
                        VillageId = (int)rdr["VillageId"],
                        Capacity = rdr["Capacity"] != DBNull.Value ? (decimal)rdr["Capacity"] : 0,
                        Location = rdr["Location"]?.ToString() ?? string.Empty,
                        StaffFirstName = rdr["FirstName"].ToString(),
                        StaffLastName = rdr["LastName"].ToString()
                    };
                }
            }

            return center;
        }*/
        // ✅ Open Batch (NO DTO)
        public int OpenBatch(int centerId, string shift, DateTime batchDate)
        {
            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Collection.usp_Collection_OpenBatch", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@CenterId", centerId);
                    cmd.Parameters.AddWithValue("@Shift", shift);
                    cmd.Parameters.AddWithValue("@BatchDate", batchDate);

                    conn.Open();
                    var result = cmd.ExecuteScalar();

                    return Convert.ToInt32(result);
                }
            }
        }

        public bool CloseBatch(int batchId)
        {
            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Collection.usp_Collection_CloseBatch", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@BatchId", batchId);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    return true;
                }
            }
        }

        // ✅ Farmer Dropdown
        public List<Farmer> GetFarmers()
        {
            var farmers = new List<Farmer>();

            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                string query = "SELECT FarmerId, FarmerName FROM Farmer.Farmers";

                SqlCommand cmd = new SqlCommand(query, conn);

                conn.Open();
                SqlDataReader rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    farmers.Add(new Farmer
                    {
                        FarmerId = (int)rdr["FarmerId"],
                        FarmerName = rdr["FarmerName"].ToString()
                    });
                }
            }

            return farmers;
        }

        // Record Milk (SP CALL)
        public (int collectionId, decimal rate, decimal amount) RecordMilk(
            int farmerId,
            int centerId,
            int milkTypeId,
            int batchId,
            decimal quantity,
            string shift,
            DateTime collectionDate,
            decimal fat,
            decimal clr)
        {
            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Collection.usp_Collection_RecordMilk", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@FarmerId", farmerId);
                    cmd.Parameters.AddWithValue("@CenterId", centerId);
                    cmd.Parameters.AddWithValue("@MilkTypeId", milkTypeId);
                    cmd.Parameters.AddWithValue("@BatchId", batchId);
                    cmd.Parameters.AddWithValue("@Quantity", quantity);
                    cmd.Parameters.AddWithValue("@Shift", shift);
                    cmd.Parameters.AddWithValue("@CollectionDate", collectionDate);
                    cmd.Parameters.AddWithValue("@AppliedFat", fat);
                    cmd.Parameters.AddWithValue("@AppliedCLR", clr);

                    conn.Open();

                    //SqlDataReader rdr = cmd.ExecuteReader();

                    //if (rdr.Read())
                    //{
                    //    return (
                    //        Convert.ToInt32(rdr["NewCollectionId"]),
                    //        Convert.ToDecimal(rdr["RateApplied"]),
                    //        Convert.ToDecimal(rdr["AmountPayable"])
                    //    );
                    //}
                    try
                    {
                        SqlDataReader rdr = cmd.ExecuteReader();

                        if (rdr.Read())
                        {
                            return (
                                Convert.ToInt32(rdr["NewCollectionId"]),
                                Convert.ToDecimal(rdr["RateApplied"]),
                                Convert.ToDecimal(rdr["AmountPayable"])
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("SP ERROR: " + ex.Message);
                    }
                }
            }

            return (0, 0, 0);
        }
        public int GetCurrentBatchId(int centerId)
        {
            using (SqlConnection conn = _dbHelper.GetConnection())
            {
                string query = @"SELECT TOP 1 BatchId 
                         FROM Collection.CollectionBatches
                         WHERE CenterId = @CenterId AND Status = 'Open'
                         ORDER BY BatchDate DESC";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@CenterId", centerId);

                conn.Open();

                var result = cmd.ExecuteScalar();

                return result != null ? Convert.ToInt32(result) : 0;
            }
        }
        public List<BatchViewModel> GetBatchesByCenter(int centerId)
        {
            var list = new List<BatchViewModel>();

            using (SqlConnection con = _dbHelper.GetConnection())
            {
                SqlCommand cmd = new SqlCommand("SELECT * FROM Collection.CollectionBatches WHERE CenterId = @CenterId ORDER BY BatchId DESC", con);
                cmd.Parameters.AddWithValue("@CenterId", centerId);

                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(new BatchViewModel
                    {
                        BatchId = (int)reader["BatchId"],
                        BatchDate = (DateTime)reader["BatchDate"],
                        Shift = reader["Shift"].ToString(),
                        Status = reader["Status"].ToString()
                    });
                }
            }

            return list;
        }
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
                    SqlDataReader rdr = cmd.ExecuteReader();

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
                            Amount = (decimal)rdr["Amount"],
                            //ReceiptNumber = rdr["ReceiptNumber"]?.ToString()
                        });
                    }
                }
            }

            return list;
        }
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
        public List<BatchViewModel> GetOpenBatches(int centerId)
        {
            var list = new List<BatchViewModel>();

            using (SqlConnection con = _dbHelper.GetConnection())
            {
                SqlCommand cmd = new SqlCommand(@"
            SELECT BatchId, BatchDate, Shift
            FROM Collection.CollectionBatches
            WHERE CenterId = @CenterId AND Status = 'Open'
            ORDER BY BatchId DESC", con);

                cmd.Parameters.AddWithValue("@CenterId", centerId);

                con.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new BatchViewModel
                        {
                            BatchId = Convert.ToInt32(reader["BatchId"]),
                            BatchDate = (DateTime)reader["BatchDate"],
                            Shift = reader["Shift"].ToString()
                        });
                    }
                }
            }

            return list;
        }
        public List<CenterInventoryViewModel> GetCenterInventory(int? centerId)
        {
            List<CenterInventoryViewModel> list = new();

            using (SqlConnection con = _dbHelper.GetConnection())
            {
                SqlCommand cmd = new SqlCommand("Collection.usp_Collection_GetCenterInventory", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@CenterId",
                    centerId.HasValue ? centerId : DBNull.Value);

                con.Open();
                var reader = cmd.ExecuteReader();

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

            return list;
        }
        
        public DashboardViewModel GetCollectionCenterByStaff(int staffId)
        {
            DashboardViewModel model = null;

            using (SqlConnection con = _dbHelper.GetConnection())
            {
                SqlCommand cmd = new SqlCommand(@"
            SELECT 
                s.FirstName + ' ' + s.LastName AS StaffName,
                cc.CenterId,
                cc.CenterName,
                cc.Capacity,
                cc.Location,
                lv.VillageName
            FROM HR.Staffs s
            INNER JOIN Collection.StaffCenters sc 
                ON sc.StaffId = s.StaffId
            INNER JOIN Collection.CollectionCenters cc 
                ON cc.CenterId = sc.CenterId
               INNER JOIN Location.Village lv
                ON cc.VillageId = lv.VillageId
            WHERE s.StaffId = @StaffId

        ", con);

                cmd.Parameters.AddWithValue("@StaffId", staffId);

                con.Open();
                var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    model = new DashboardViewModel
                    {
                        StaffName = reader["StaffName"].ToString(),
                        CenterId = Convert.ToInt32(reader["CenterId"]),
                        CenterName = reader["CenterName"].ToString(),

                        Capacity = reader["Capacity"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["Capacity"]) : 0,

                        Location = reader["Location"]?.ToString() ?? "",

                        VillageName = reader["VillageName"]?.ToString() ?? ""
                    };
                }
            }

            return model;
        }
    }
}
