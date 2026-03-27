using DairyIndustry.Data;
using DairyIndustry.Models.Location;
using DairyIndustry.Repositories.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DairyIndustry.Repositories
{
    public class LocationRepository:ILocationRepository
    {
        private readonly DbHelper _dbHelper;
        public LocationRepository(DbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public List<State> GetAllStates()
        {
            List<State> states = new List<State>();
            using (SqlConnection conn = _dbHelper.GetSqlConnection()) {

                SqlCommand cmd = new SqlCommand("Location.usp_Location_GetStates",conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;

                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read()) {
                    states.Add(new State
                    {
                        StateId = Convert.ToInt32(reader["StateId"]),
                        StateName = reader["StateName"].ToString()
                    });
                }
            }
            return states;
        }

        public List<City> GetCitiesByState(int stateId)
        {
            List<City> cities = new List<City>();
            using (SqlConnection conn = _dbHelper.GetSqlConnection())
            {

                SqlCommand cmd = new SqlCommand("Location.usp_Location_GetCitiesByState", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@StateId", stateId);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    cities.Add(new City
                    {
                        CityId = Convert.ToInt32(reader["CityId"]),
                        CityName = reader["CityName "].ToString()
                    });
                }
            }
            return cities;
        }

        public List<Village> GetVillagesByCity(int cityId)
        {
            List<Village> villages = new List<Village>();

            using (SqlConnection conn = _dbHelper.GetSqlConnection())
            {
                SqlCommand cmd = new SqlCommand("Location.usp_Location_GetVillagesByCity", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@CityId", cityId);

                conn.Open();
                SqlDataReader rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    villages.Add(new Village
                    {
                        VillageId = (int)rdr["VillageId"],
                        VillageName = rdr["VillageName"].ToString()
                    });
                }
            }

            return villages;
        }
    }
}

