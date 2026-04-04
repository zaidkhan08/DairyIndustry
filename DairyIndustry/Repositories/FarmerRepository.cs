using DairyIndustry.Data;
using DairyIndustry.Models.Admin;
using DairyIndustry.Models.FarmerModel;
using DairyIndustry.Models.ViewModels;
using DairyIndustry.Repositories;
using Microsoft.Data.SqlClient;
using System.Data;

public class FarmerRepository : IFarmerRepository
{
    private readonly DbHelper _dbHelper;

    public FarmerRepository(DbHelper dbHelper)
    {
        _dbHelper = dbHelper;
    }

    // =========================
    // GET STATES
    // =========================
    public List<StateModel> GetStates()
    {
        var list = new List<StateModel>();

        using (SqlConnection con = _dbHelper.GetConnection())
        using (SqlCommand cmd = new SqlCommand("Location.usp_Location_GetStates", con))
        {
            cmd.CommandType = CommandType.StoredProcedure;

            con.Open();

            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    list.Add(new StateModel
                    {
                        StateId = (int)dr["StateId"],
                        StateName = dr["StateName"].ToString()
                    });
                }
            }
        }

        return list;
    }

    // =========================
    // GET CITIES BY STATE
    // =========================
    public List<CityModel> GetCitiesByState(int stateId)
    {
        var list = new List<CityModel>();

        using (SqlConnection con = _dbHelper.GetConnection())
        using (SqlCommand cmd = new SqlCommand("Location.usp_Location_GetCitiesByState", con))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@StateId", stateId);

            con.Open();

            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    list.Add(new CityModel
                    {
                        CityId = (int)dr["CityId"],
                        CityName = dr["CityName"].ToString()
                    });
                }
            }
        }

        return list;
    }
    // =========================
    // GET VILLAGES BY CITY
    // =========================
    public List<VillageModel> GetVillagesByCity(int cityId)
    {
        var list = new List<VillageModel>();

        using (SqlConnection con = _dbHelper.GetConnection())
        using (SqlCommand cmd = new SqlCommand("Location.usp_Location_GetVillagesByCity", con))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@CityId", cityId);

            con.Open();

            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    list.Add(new VillageModel
                    {
                        VillageId = (int)dr["VillageId"],
                        VillageName = dr["VillageName"].ToString()
                    });
                }
            }
        }

        return list;
    }
    // =========================
    // ADD FARMER (UPDATED )
    // =========================
    public FarmerViewModel AddFarmer(FarmerViewModel model, int staffId)
    {
        using (SqlConnection con = _dbHelper.GetConnection())
        using (SqlCommand cmd = new SqlCommand("Farmer.usp_Center_RegisterFarmer", con))
        {
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@StaffId", staffId);
            cmd.Parameters.AddWithValue("@FarmerName", model.FarmerName);
            cmd.Parameters.AddWithValue("@VillageId", model.VillageId);
            cmd.Parameters.AddWithValue("@Phone", (object?)model.Phone ?? DBNull.Value);

            cmd.Parameters.AddWithValue("@BankName", (object?)model.BankName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@AccountNumber", (object?)model.AccountNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IFSCCode", (object?)model.IFSCCode ?? DBNull.Value);

            cmd.Parameters.AddWithValue("@ProfilePhoto",
                string.IsNullOrEmpty(model.ProfilePhoto) ? (object)DBNull.Value : model.ProfilePhoto);

            con.Open();

            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    model.FarmerId = Convert.ToInt32(reader["NewFarmerId"]);
                    model.FarmerCode = reader["FarmerCode"].ToString();
                }
            }
        }

        return model;
    }


    //  GET ALL FARMERS
    public List<FarmerViewModel> GetAllFarmers(int staffId, bool? isActive = null, string search = null)
    {
        var list = new List<FarmerViewModel>();

        using (SqlConnection con = _dbHelper.GetConnection())
        using (SqlCommand cmd = new SqlCommand("Farmer.usp_Center_GetAllFarmers", con))
        {
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@StaffId", staffId);
            cmd.Parameters.AddWithValue("@IsActive", (object)isActive ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Search", (object)search ?? DBNull.Value);

            con.Open();

            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    list.Add(new FarmerViewModel
                    {
                        FarmerId = Convert.ToInt32(reader["FarmerId"]),
                        FarmerCode = reader["FarmerCode"].ToString(),
                        FarmerName = reader["FarmerName"].ToString(),
                        Phone = reader["Phone"]?.ToString(),
                        IsActive = Convert.ToBoolean(reader["IsActive"]),
                        ProfilePhoto = reader["ProfilePhoto"]?.ToString(),

                        //  IMPORTANT (your error fix)
                        VillageName = reader["VillageName"].ToString(),
                        CityName = reader["CityName"].ToString(),

                        BankName = reader["BankName"]?.ToString(),
                        AccountNumber = reader["AccountNumber"]?.ToString()
                    });
                }
            }
        }

        return list;
    }

    // TOGGLE ACTIVE / INACTIVE
    public void ToggleFarmerStatus(int staffId, int farmerId, bool isActive)
    {
        using (SqlConnection con = _dbHelper.GetConnection())
        using (SqlCommand cmd = new SqlCommand("Farmer.usp_Center_ToggleFarmerActive", con))
        {
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@StaffId", staffId);
            cmd.Parameters.AddWithValue("@FarmerId", farmerId);
            cmd.Parameters.AddWithValue("@IsActive", isActive);

            con.Open();
            cmd.ExecuteNonQuery();
        }
    }

    //  UPDATE FARMER
    public int UpdateFarmer(FarmerViewModel model, int staffId)
    {
        using (SqlConnection con = _dbHelper.GetConnection())
        using (SqlCommand cmd = new SqlCommand("Farmer.usp_Center_UpdateFarmer", con))
        {
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@StaffId", staffId);
            cmd.Parameters.AddWithValue("@FarmerId", model.FarmerId);
            cmd.Parameters.AddWithValue("@FarmerName", model.FarmerName);
            cmd.Parameters.AddWithValue("@VillageId", model.VillageId);
            cmd.Parameters.AddWithValue("@Phone", (object)model.Phone ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ProfilePhoto", (object)model.ProfilePhoto ?? DBNull.Value);

            con.Open();

            return Convert.ToInt32(cmd.ExecuteScalar());
        }
    }



    public FarmerViewModel GetFarmerById(int farmerId, int staffId)
    {
        FarmerViewModel model = null;

        using (SqlConnection con = _dbHelper.GetConnection())
        using (SqlCommand cmd = new SqlCommand("Farmer.usp_Center_GetAllFarmers", con))
        {
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@StaffId", staffId);
            cmd.Parameters.AddWithValue("@IsActive", DBNull.Value);
            cmd.Parameters.AddWithValue("@Search", DBNull.Value);

            con.Open();

            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (Convert.ToInt32(reader["FarmerId"]) == farmerId)
                    {
                        model = new FarmerViewModel
                        {
                            FarmerId = Convert.ToInt32(reader["FarmerId"]),
                            FarmerName = reader["FarmerName"].ToString(),
                            FarmerCode = reader["FarmerCode"].ToString(),
                            Phone = reader["Phone"]?.ToString(),
                            VillageName = reader["VillageName"].ToString(),
                            CityName = reader["CityName"].ToString(),
                            ProfilePhoto = reader["ProfilePhoto"]?.ToString()
                        };
                        break;
                    }
                }
            }
        }

        return model;
    }
 

}