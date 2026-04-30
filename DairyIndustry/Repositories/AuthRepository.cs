using DairyIndustry.Data;
using DairyIndustry.Interfaces;
using DairyIndustry.Models.Admin;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Data;

namespace DairyIndustry.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly DbHelper _db;
        private readonly EmailSettings _settings;

        public AuthRepository(DbHelper db)
        {
            _db = db;
         
        }
        // ════════════════════════════════════════════════════
        // OTP
        // ════════════════════════════════════════════════════

        public string GenerateOtp(int userId, string purpose)
        {
            using var con = _db.GetConnection();
            using var cmd = new SqlCommand("Admin.usp_Auth_GenerateOtp", con);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@Purpose", purpose);

            con.Open();
            var result = cmd.ExecuteScalar();
            return result?.ToString();
        }

        public OtpValidationResult ValidateOtp(int userId, string otpCode, string purpose)
        {
            using var con = _db.GetConnection();
            using var cmd = new SqlCommand("Admin.usp_Auth_ValidateOtp", con);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@OtpCode", otpCode);
            cmd.Parameters.AddWithValue("@Purpose", purpose);

            con.Open();
            using var reader = cmd.ExecuteReader();

            var result = new OtpValidationResult();
            if (reader.Read())
            {
                result.IsValid = Convert.ToBoolean(reader["IsValid"]);
                result.Reason = reader["Reason"].ToString();
            }
            return result;
        }

        // ════════════════════════════════════════════════════
        // TRUSTED DEVICE
        // ════════════════════════════════════════════════════

        public bool CheckTrustedDevice(int userId, string deviceToken)
        {
            using var con = _db.GetConnection();
            using var cmd = new SqlCommand("Admin.usp_Auth_CheckTrustedDevice", con);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@DeviceToken", deviceToken);

            con.Open();
            using var reader = cmd.ExecuteReader();

            if (reader.Read())
                return Convert.ToBoolean(reader["IsTrusted"]);

            return false;
        }

        public void RegisterTrustedDevice(int userId, string deviceToken, string deviceName, int daysDuration = 30)
        {
            using var con = _db.GetConnection();
            using var cmd = new SqlCommand("Admin.usp_Auth_RegisterTrustedDevice", con);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@DeviceToken", deviceToken);
            cmd.Parameters.AddWithValue("@DeviceName", deviceName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@DaysDuration", daysDuration);

            con.Open();
            cmd.ExecuteNonQuery();
        }

        public void RevokeTrustedDevice(int userId, string deviceToken)
        {
            using var con = _db.GetConnection();
            using var cmd = new SqlCommand("Admin.usp_Auth_RevokeTrustedDevices", con);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@DeviceToken", deviceToken);

            con.Open();
            cmd.ExecuteNonQuery();
        }

        public void RevokeAllTrustedDevices(int userId)
        {
            using var con = _db.GetConnection();
            using var cmd = new SqlCommand("Admin.usp_Auth_RevokeTrustedDevices", con);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@DeviceToken", DBNull.Value);

            con.Open();
            cmd.ExecuteNonQuery();
        }

        public List<TrustedDevice> GetTrustedDevices(int userId)
        {
            using var con = _db.GetConnection();
            using var cmd = new SqlCommand("Admin.usp_Auth_GetTrustedDevices", con);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@UserId", userId);

            con.Open();
            using var reader = cmd.ExecuteReader();

            var list = new List<TrustedDevice>();
            while (reader.Read())
            {
                list.Add(new TrustedDevice
                {
                    DeviceId = Convert.ToInt32(reader["DeviceId"]),
                    DeviceToken = reader["DeviceToken"].ToString(),       
                    DeviceName = reader["DeviceName"] == DBNull.Value ? "Unknown Device" : reader["DeviceName"].ToString(),
                    CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                    LastUsedAt = Convert.ToDateTime(reader["LastUsedAt"]),
                    ExpiresAt = Convert.ToDateTime(reader["ExpiresAt"]),
                    IsActive = Convert.ToBoolean(reader["IsActive"])
                });
            }
            return list;
        }
        public void RevokeDeviceById(int userId, int deviceId)
        {
            using var con = _db.GetConnection();
            using var cmd = new SqlCommand("Admin.usp_Auth_RevokeDeviceById", con);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@DeviceId", deviceId);

            con.Open();
            cmd.ExecuteNonQuery();
        }
    }
}

