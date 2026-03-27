using DairyIndustry.Data;
using DairyIndustry.Models.Admin;
using DairyIndustry.Repositories.Interfaces;
using Microsoft.Data.SqlClient;

namespace DairyIndustry.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly DbHelper _dbHelper;
        public UserRepository(DbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }
        public Users ValidateUser(string username, string password)
        {
            using (SqlConnection con = _dbHelper.GetSqlConnection())
            {
                con.Open();

                string query = @"SELECT UserId, Username, PasswordHash, RoleId, StaffId, IsActive, CenterId, FarmerId
                 FROM Admin.Users
                 WHERE Username = @Username 
                 AND PasswordHash = @Password
                 AND IsActive = 1";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@Password", password);

                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {

                    Users user = new Users
                    {

                        //Id = Convert.ToInt32(reader["Id"]),
                        //Name = reader["Name"].ToString(),
                        UserId = Convert.ToInt32(reader["UserId"]),
                        UserName = reader["Username"].ToString(),
                        PasswordHash = reader["PasswordHash"].ToString(),
                        RoleId = Convert.ToInt32(reader["RoleId"]) ,
                       // StaffId = Convert.ToInt32(reader["StaffId"]),

                        StaffId = reader["StaffId"] != DBNull.Value? Convert.ToInt32(reader["StaffId"]): 0,  // or default
                        IsActive = reader["IsActive"] != DBNull.Value? Convert.ToBoolean(reader["IsActive"]): false,
                        CenterId = reader["CenterId"] != DBNull.Value? Convert.ToInt32(reader["CenterId"]):0,

                        FarmerId = reader["FarmerId"] != DBNull.Value? Convert.ToInt32(reader["FarmerId"]):0
                    };

                    return user;

                }
            }

            return null;
        }
    }
}
