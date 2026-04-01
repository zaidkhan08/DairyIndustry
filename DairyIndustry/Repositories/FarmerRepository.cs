using DairyIndustry.Data;
using DairyIndustry.Models.Admin;
using DairyIndustry.Models.FarmerModel;
using DairyIndustry.Repositories;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;

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
        {
            SqlCommand cmd = new SqlCommand("SELECT StateId, StateName FROM Location.States", con);
            con.Open();

            SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                list.Add(new StateModel
                {
                    StateId = (int)dr["StateId"],
                    StateName = dr["StateName"].ToString()
                });
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
        {
            SqlCommand cmd = new SqlCommand(
                "SELECT CityId, CityName FROM Location.Cities WHERE StateId = @StateId", con);

            cmd.Parameters.AddWithValue("@StateId", stateId);

            con.Open();
            SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                list.Add(new CityModel
                {
                    CityId = (int)dr["CityId"],
                    CityName = dr["CityName"].ToString()
                });
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
        {
            SqlCommand cmd = new SqlCommand(
                "SELECT VillageId, VillageName FROM Location.Villages WHERE CityId = @CityId", con);

            cmd.Parameters.AddWithValue("@CityId", cityId);

            con.Open();
            SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                list.Add(new VillageModel
                {
                    VillageId = (int)dr["VillageId"],
                    VillageName = dr["VillageName"].ToString()
                });
            }
        }

        return list;
    }

    // =========================
    // ADD FARMER
    // =========================

    public void AddFarmer(FarmerViewModel model, int staffId)
    {
        using (SqlConnection con = _dbHelper.GetConnection())
        {
            SqlCommand cmd = new SqlCommand("Farmer.usp_Center_RegisterFarmer", con);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@StaffId", staffId);
            cmd.Parameters.AddWithValue("@FarmerName", model.FarmerName);
            cmd.Parameters.AddWithValue("@VillageId", model.VillageId);
            cmd.Parameters.AddWithValue("@Phone", model.Phone);

            cmd.Parameters.AddWithValue("@BankName", (object?)model.BankName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@AccountNumber", (object?)model.AccountNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IFSCCode", (object?)model.IFSCCode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ProfilePhoto", (object?)model.ProfilePhoto ?? DBNull.Value);

            con.Open();
            cmd.ExecuteNonQuery();
        }
           
    }
}
