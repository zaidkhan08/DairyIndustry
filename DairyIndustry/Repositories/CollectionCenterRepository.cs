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

    
        public CollectionCenter GetCollectionCenterById(int id)
        {
            CollectionCenter collectionCenter = null;
            using(SqlConnection con = _dbHelper.GetSqlConnection())
            {
                con.Open();

                string query = @"
                    SELECT cc.CenterId, cc.CenterName, cc.Capacity, cc.Location,
                           v.VillageName, c.CityName, s.StateName
                    FROM Collection.CollectionCenters cc
                    INNER JOIN Location.Village v ON v.VillageId = cc.VillageId
                    INNER JOIN Location.City c ON c.CityId = v.CityId
                    INNER JOIN Location.State s ON s.StateId = c.StateId
                    WHERE cc.CenterId = @CenterId";

                SqlCommand cmd = new SqlCommand(query,con);
              
                cmd.Parameters.AddWithValue("@CenterId", id);

                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read()) {
                    collectionCenter = new CollectionCenter()
                    {

                        CenterId = (int)reader["CenterId"],
                        CenterName = reader["CenterName"].ToString(),
                        Capacity = reader["Capacity"] != DBNull.Value ? (decimal)reader["Capacity"] : 0,
                        Location = reader["Location"].ToString(),
                        VillageName = reader["VillageName"].ToString()

                    };
                }
                return collectionCenter;
            }

           
        }

        public bool AddFarmer(Farmer farmer, int centerId)
        {
            using (SqlConnection conn = _dbHelper.GetSqlConnection())
            {
                SqlCommand cmd = new SqlCommand("Farmer.usp_Farmer_RegisterFarmer", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@FarmerName", farmer.FarmerName);
                cmd.Parameters.AddWithValue("@VillageId", farmer.VillageId);
                cmd.Parameters.AddWithValue("@Phone", (object)farmer.Phone ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@BankName", (object)farmer.BankName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@AccountNumber", (object)farmer.AccountNumber ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IFSCCode", (object)farmer.IFSCCode ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ProfilePhoto", (object)farmer.ProfilePhoto ?? DBNull.Value);

                // 🔥 ADD THIS (MOST IMPORTANT)
                cmd.Parameters.AddWithValue("@CenterId", centerId);

                conn.Open();
                int rows = cmd.ExecuteNonQuery();
                conn.Close();

                return rows > 0;
            }
        }


        public List<Farmer> GetFarmersByCenterStaff(int centerId)
        {


            List<Farmer> farmers = new List<Farmer>();

            using (SqlConnection conn = _dbHelper.GetSqlConnection())
            {
                SqlCommand cmd = new SqlCommand("Farmer.usp_GetFarmersByCenterStaff", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                // Pass the @IsActive parameter from the SP
                //    cmd.Parameters.AddWithValue("@IsActive", (object)IsActive ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CenterId", centerId);
                conn.Open();
                SqlDataReader rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    farmers.Add(new Farmer
                    {
                        FarmerId = (int)rdr["FarmerId"],
                        FarmerName = rdr["FarmerName"].ToString(),
                        Phone = rdr["Phone"]?.ToString(),
                        IsActive = (bool)rdr["IsActive"],
                        ProfilePhoto = rdr["ProfilePhoto"]?.ToString(),
                        VillageName = rdr["VillageName"].ToString(),
                        CityName = rdr["CityName"].ToString(),
                        StateName = rdr["StateName"].ToString(),
                        BankName = rdr["BankName"]?.ToString() ?? "N/A",
                        AccountNumber = rdr["AccountNumber"]?.ToString() ?? "Not Linked",
                        IFSCCode = rdr["IFSCCode"]?.ToString()??"Not Linked"
                    });
                }
            }
            return farmers;
        }

   
       
    }
}
