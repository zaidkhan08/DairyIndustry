// Repositories/LogisticsRepository.cs
using DairyIndustry.Data;
using DairyIndustry.Models.Admin;
using DairyIndustry.Models.Logistics;
using Microsoft.Data.SqlClient;
using MimeKit;
using System.Data;

namespace DairyIndustry.Repositories
{
    public class LogisticsRepository : ILogisticsRepository
    {
        private readonly DbHelper _db;
        private readonly EmailSettings _settings;
        public LogisticsRepository(DbHelper db, IConfiguration configuration)
        {
            _db = db;
            _settings = configuration.GetSection("EmailSettings").Get<EmailSettings>();
        }

        public void SaveEmailOtp(string email, string otpCode)
        {
            using SqlConnection con = _db.GetConnection();
            using SqlCommand cmd = new("Logistics.usp_Logistics_SendEmailOtp", con)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue("@OtpCode", otpCode);
            con.Open();
            cmd.ExecuteNonQuery();
        }

        public bool VerifyEmailOtp(string email, string otpCode)
        {
            using SqlConnection con = _db.GetConnection();
            using SqlCommand cmd = new("Logistics.usp_Logistics_VerifyEmailOtp", con)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue("@OtpCode", otpCode);
            con.Open();
            var result = cmd.ExecuteScalar();
            return Convert.ToInt32(result) == 1;
        }
        public int RegisterDriver(string driverName, string licenseNo, string phone,
                                  string email, string username, string passwordHash,
                                  string drivingLicensePath)
        {
            using SqlConnection con = _db.GetConnection();
            using SqlCommand cmd = new("Logistics.usp_Logistics_RegisterDriver", con)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@DriverName", driverName);
            cmd.Parameters.AddWithValue("@LicenseNo", licenseNo);
            cmd.Parameters.AddWithValue("@Phone", (object?)phone ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
            cmd.Parameters.AddWithValue("@DrivingLicensePath", drivingLicensePath);
            con.Open();
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public DriversModel GetDriverByUserId(int userId)
        {
            using SqlConnection con = _db.GetConnection();
            using SqlCommand cmd = new("Logistics.usp_Logistics_GetDriverByUserId", con)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@UserId", userId);
            con.Open();
            using SqlDataReader r = cmd.ExecuteReader();
            if (!r.Read()) return null;
            return MapDriver(r);
        }

        public List<DriversModel> GetAllDrivers()
        {
            var list = new List<DriversModel>();
            using SqlConnection con = _db.GetConnection();
            using SqlCommand cmd = new("Logistics.usp_Logistics_GetAllDrivers", con)
            {
                CommandType = CommandType.StoredProcedure
            };
            con.Open();
            using SqlDataReader r = cmd.ExecuteReader();
            while (r.Read()) list.Add(MapDriver(r, includeVehicle: true));
            return list;
        }

        public void UpdateDriverStatus(int driverId, string status)
        {
            using SqlConnection con = _db.GetConnection();
            using SqlCommand cmd = new("Logistics.usp_Logistics_UpdateDriverStatus", con)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@DriverId", driverId);
            cmd.Parameters.AddWithValue("@Status", status);
            con.Open();
            cmd.ExecuteNonQuery();
        }

        public int AddVehicle(int driverId, string vehicleNumber, decimal capacity,
                              string vehicleRcPath)
        {
            using SqlConnection con = _db.GetConnection();
            using SqlCommand cmd = new("Logistics.usp_Logistics_AddVehicle", con)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@DriverId", driverId);
            cmd.Parameters.AddWithValue("@VehicleNumber", vehicleNumber);
            cmd.Parameters.AddWithValue("@Capacity", capacity);
            cmd.Parameters.AddWithValue("@VehicleRCPath", vehicleRcPath);
            con.Open();
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public List<VehiclesModel> GetVehiclesByDriverId(int driverId)
        {
            var list = new List<VehiclesModel>();
            using SqlConnection con = _db.GetConnection();
            const string query = @"
                SELECT VehicleId, DriverId, VehicleNumber,
                       Capacity, VehicleRCPath, Status, RegisteredOn
                FROM Logistics.VehiclesNew
                WHERE DriverId = @DriverId
                ORDER BY RegisteredOn DESC";
            using SqlCommand cmd = new(query, con);
            cmd.Parameters.AddWithValue("@DriverId", driverId);
            con.Open();
            using SqlDataReader r = cmd.ExecuteReader();
            while (r.Read()) list.Add(MapVehicle(r));
            return list;
        }

        public List<VehiclesModel> GetAllVehicles()
        {
            var list = new List<VehiclesModel>();
            using SqlConnection con = _db.GetConnection();
            using SqlCommand cmd = new("Logistics.usp_Logistics_GetAllVehicles", con)
            {
                CommandType = CommandType.StoredProcedure
            };
            con.Open();
            using SqlDataReader r = cmd.ExecuteReader();
            while (r.Read()) list.Add(MapVehicle(r, includeDriver: true));
            return list;
        }

        public void UpdateVehicleStatus(int vehicleId, string status)
        {
            using SqlConnection con = _db.GetConnection();
            using SqlCommand cmd = new("Logistics.usp_Logistics_UpdateVehicleStatus", con)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@VehicleId", vehicleId);
            cmd.Parameters.AddWithValue("@Status", status);
            con.Open();
            cmd.ExecuteNonQuery();
        }

        public List<MilkTransferModel> GetDriverTransfers(int driverId)
        {
            var list = new List<MilkTransferModel>();
            const string query = @"
                SELECT mt.TransferId, mt.DispatchDate, mt.ReceivedDate,
                    mt.DispatchQty, mt.ReceivedQty, mt.LossQty,
                    CASE WHEN mt.DispatchQty > 0 AND mt.LossQty IS NOT NULL
                         THEN ROUND((mt.LossQty / mt.DispatchQty) * 100, 2)
                         ELSE 0 END AS LossPercent,
                    CASE WHEN mt.ReceivedDate IS NULL THEN 'Pending' ELSE 'Received' END AS TransferStatus,
                    cc.CenterId, cc.CenterName,
                    pp.PlantId,  pp.PlantName,
                    v.VehicleId, v.VehicleNumber,
                    d.DriverId,  d.DriverName, d.Phone AS DriverPhone,
                    tqt.TestedFat, tqt.TestedCLR, tqt.TestDate,
                    cb.BatchId, cb.Shift, cb.BatchDate, cb.AvgFat AS BatchAvgFat, cb.AvgCLR AS BatchAvgCLR
                FROM Production.MilkTransfers mt
                INNER JOIN Collection.CollectionBatches    cb  ON cb.BatchId  = mt.BatchId
                INNER JOIN Collection.CollectionCenters    cc  ON cc.CenterId = cb.CenterId
                INNER JOIN Production.ProcessingPlants     pp  ON pp.PlantId  = mt.PlantId
                INNER JOIN Logistics.VehiclesNew           v   ON v.VehicleId = mt.VehicleId
                INNER JOIN Logistics.DriversNew            d   ON d.DriverId  = v.DriverId
                LEFT  JOIN Production.TransferQualityTests tqt ON tqt.TransferId = mt.TransferId
                WHERE d.DriverId = @DriverId
                ORDER BY mt.DispatchDate DESC";

            using SqlConnection con = _db.GetConnection();
            using SqlCommand cmd = new(query, con);
            cmd.Parameters.AddWithValue("@DriverId", driverId);
            con.Open();
            using SqlDataReader r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new MilkTransferModel
                {
                    TransferId = Convert.ToInt32(r["TransferId"]),
                    DispatchDate = Convert.ToDateTime(r["DispatchDate"]),
                    ReceivedDate = r["ReceivedDate"] == DBNull.Value ? null : Convert.ToDateTime(r["ReceivedDate"]),
                    DispatchQty = Convert.ToDecimal(r["DispatchQty"]),
                    ReceivedQty = r["ReceivedQty"] == DBNull.Value ? null : Convert.ToDecimal(r["ReceivedQty"]),
                    LossQty = r["LossQty"] == DBNull.Value ? null : Convert.ToDecimal(r["LossQty"]),
                    LossPercent = Convert.ToDecimal(r["LossPercent"]),
                    TransferStatus = r["TransferStatus"].ToString(),
                    CenterId = Convert.ToInt32(r["CenterId"]),
                    CenterName = r["CenterName"].ToString(),
                    PlantId = Convert.ToInt32(r["PlantId"]),
                    PlantName = r["PlantName"].ToString(),
                    VehicleId = Convert.ToInt32(r["VehicleId"]),
                    VehicleNumber = r["VehicleNumber"].ToString(),
                    DriverId = Convert.ToInt32(r["DriverId"]),
                    DriverName = r["DriverName"] == DBNull.Value ? null : r["DriverName"].ToString(),
                    DriverPhone = r["DriverPhone"] == DBNull.Value ? null : r["DriverPhone"].ToString(),
                    TestedFat = r["TestedFat"] == DBNull.Value ? null : Convert.ToDecimal(r["TestedFat"]),
                    TestedCLR = r["TestedCLR"] == DBNull.Value ? null : Convert.ToDecimal(r["TestedCLR"]),
                    TestDate = r["TestDate"] == DBNull.Value ? null : Convert.ToDateTime(r["TestDate"]),
                    BatchId = Convert.ToInt32(r["BatchId"]),
                    Shift = r["Shift"].ToString(),
                    BatchDate = Convert.ToDateTime(r["BatchDate"]),
                    BatchAvgFat = r["BatchAvgFat"] == DBNull.Value ? null : Convert.ToDecimal(r["BatchAvgFat"]),
                    BatchAvgCLR = r["BatchAvgCLR"] == DBNull.Value ? null : Convert.ToDecimal(r["BatchAvgCLR"])
                });
            }
            return list;
        }

        private static DriversModel MapDriver(SqlDataReader r, bool includeVehicle = false)
        {
            var d = new DriversModel
            {
                DriverId = Convert.ToInt32(r["DriverId"]),
                DriverName = r["DriverName"].ToString(),
                LicenseNo = r["LicenseNo"].ToString(),
                Phone = r["Phone"] == DBNull.Value ? null : r["Phone"].ToString(),
                Email = r["Email"] == DBNull.Value ? null : r["Email"].ToString(),
                IsEmailVerified = Convert.ToBoolean(r["IsEmailVerified"]),
                DrivingLicensePath = r["DrivingLicensePath"] == DBNull.Value ? null : r["DrivingLicensePath"].ToString(),
                Status = r["Status"].ToString(),
                RegisteredOn = Convert.ToDateTime(r["RegisteredOn"]),
                Username = r["Username"].ToString(),
                IsActive = Convert.ToBoolean(r["IsActive"])
            };
            if (includeVehicle)
            {
                d.VehicleNumber = r["VehicleNumber"] == DBNull.Value ? null : r["VehicleNumber"].ToString();
                d.VehicleStatus = r["VehicleStatus"] == DBNull.Value ? null : r["VehicleStatus"].ToString();
                d.VehicleRCPath = r["VehicleRCPath"] == DBNull.Value ? null : r["VehicleRCPath"].ToString();
            }
            return d;
        }

        private static VehiclesModel MapVehicle(SqlDataReader r, bool includeDriver = false)
        {
            var v = new VehiclesModel
            {
                VehicleId = Convert.ToInt32(r["VehicleId"]),
                DriverId = Convert.ToInt32(r["DriverId"]),
                VehicleNumber = r["VehicleNumber"].ToString(),
                Capacity = Convert.ToDecimal(r["Capacity"]),
                VehicleRCPath = r["VehicleRCPath"] == DBNull.Value ? null : r["VehicleRCPath"].ToString(),
                Status = r["Status"].ToString(),
                RegisteredOn = Convert.ToDateTime(r["RegisteredOn"])
            };
            if (includeDriver)
            {
                v.DriverName = r["DriverName"] == DBNull.Value ? null : r["DriverName"].ToString();
                v.Phone = r["Phone"] == DBNull.Value ? null : r["Phone"].ToString();
                v.DriverStatus = r["DriverStatus"].ToString();
            }
            return v;
        }

        public DriverContactInfo GetDriverContactInfo(int driverId)
        {
            string query = @"
    SELECT
        d.DriverId,
        d.DriverName,
        d.Phone,
        u.Username,
        COALESCE(s.Email, d.Email) AS Email   -- ← fallback to driver's own email
    FROM Logistics.DriversNew d
    INNER JOIN Admin.Users    u ON u.UserId  = d.UserId
    LEFT  JOIN HR.Staffs      s ON s.StaffId = u.StaffId
    WHERE d.DriverId = @DriverId";

            using var con = _db.GetConnection();
            using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@DriverId", driverId);
            con.Open();
            using var r = cmd.ExecuteReader();
            if (r.Read())
            {
                return new DriverContactInfo
                {
                    DriverId = Convert.ToInt32(r["DriverId"]),
                    DriverName = r["DriverName"].ToString(),
                    Username = r["Username"].ToString(),
                    Phone = r["Phone"] == DBNull.Value ? null : r["Phone"].ToString(),
                    Email = r["Email"] == DBNull.Value ? null : r["Email"].ToString()
                };
            }
            return null;
        }

        public DriverContactInfo GetDriverContactInfoByVehicleId(int vehicleId)
        {
            string query = @"
    SELECT
        d.DriverId,
        d.DriverName,
        d.Phone,
        u.Username,
        COALESCE(s.Email, d.Email) AS Email,   -- ← fallback
        v.VehicleNumber
    FROM Logistics.VehiclesNew v
    INNER JOIN Logistics.DriversNew d ON d.DriverId = v.DriverId
    INNER JOIN Admin.Users          u ON u.UserId   = d.UserId
    LEFT  JOIN HR.Staffs            s ON s.StaffId  = u.StaffId
    WHERE v.VehicleId = @VehicleId";

            using var con = _db.GetConnection();
            using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@VehicleId", vehicleId);
            con.Open();
            using var r = cmd.ExecuteReader();
            if (r.Read())
            {
                return new DriverContactInfo
                {
                    DriverId = Convert.ToInt32(r["DriverId"]),
                    DriverName = r["DriverName"].ToString(),
                    Username = r["Username"].ToString(),
                    Phone = r["Phone"] == DBNull.Value ? null : r["Phone"].ToString(),
                    Email = r["Email"] == DBNull.Value ? null : r["Email"].ToString(),
                    VehicleNumber = r["VehicleNumber"].ToString()
                };
            }
            return null;
        }

        // ════════════════════════════════════════════════════════
        // FILE 1 — Add these two methods to AdminRepository.cs
        // (same pattern as your existing SendOtpEmail method)
        // ════════════════════════════════════════════════════════

        // ── 1. Driver approval / rejection email ─────────────────
        public void SendDriverStatusEmail(string toEmail, string driverName,
                                           string username, string status)
        {
            if (string.IsNullOrWhiteSpace(toEmail)) return;

            bool approved = status == "Active";

            string subject = approved
                ? "✅ DMS — Your Driver Account Has Been Approved"
                : "❌ DMS — Your Driver Registration Update";

            string statusColor = approved ? "#16a34a" : "#dc2626";
            string statusBg = approved ? "#f0fdf4" : "#fef2f2";
            string statusBorder = approved ? "#bbf7d0" : "#fecaca";
            string statusLabel = approved ? "APPROVED" : status.ToUpper();
            string statusIcon = approved ? "✅" : "❌";

            string actionBlock = approved ? $@"
        <div style='background:#eff6ff;border:1px solid #bfdbfe;border-radius:10px;padding:16px 20px;margin:20px 0;'>
            <p style='margin:0 0 8px;font-weight:600;color:#1e40af;'>What happens next?</p>
            <ul style='margin:0;padding-left:20px;color:#374151;font-size:14px;line-height:1.8;'>
                <li>Log in at <strong>DMS</strong> using your username: <strong>{username}</strong></li>
                <li>Register your vehicle from your dashboard</li>
                <li>Your vehicle will also need admin approval before assignments</li>
                <li>Once your vehicle is approved, you will start receiving transfer assignments</li>
            </ul>
        </div>" : $@"
        <div style='background:#fef9c3;border:1px solid #fde047;border-radius:10px;padding:16px 20px;margin:20px 0;'>
            <p style='margin:0;color:#713f12;font-size:14px;'>
                If you believe this is a mistake or need more information,
                please contact the DMS admin team.
            </p>
        </div>";

            string body = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'/></head>
<body style='margin:0;padding:0;background:#f1f5f9;font-family:Segoe UI,Arial,sans-serif;'>
<table width='100%' cellpadding='0' cellspacing='0' style='padding:40px 20px;'>
  <tr><td align='center'>
    <table width='560' cellpadding='0' cellspacing='0'
           style='background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.08);'>

      <!-- Header -->
      <tr>
        <td style='background:linear-gradient(135deg,#0a1628,#1e40af);padding:32px 36px;text-align:center;'>
          <div style='font-size:28px;margin-bottom:8px;'>🥛</div>
          <div style='font-family:Georgia,serif;font-size:22px;font-weight:600;color:#ffffff;letter-spacing:-0.3px;'>
            DMS
          </div>
          <div style='color:rgba(255,255,255,0.65);font-size:12px;margin-top:4px;letter-spacing:1px;text-transform:uppercase;'>
            Driver Registration Update
          </div>
        </td>
      </tr>

      <!-- Status Badge -->
      <tr>
        <td style='padding:28px 36px 0;text-align:center;'>
          <div style='display:inline-block;background:{statusBg};border:1.5px solid {statusBorder};
                      border-radius:50px;padding:8px 24px;'>
            <span style='color:{statusColor};font-weight:700;font-size:13px;letter-spacing:1px;'>
              {statusIcon} &nbsp; {statusLabel}
            </span>
          </div>
        </td>
      </tr>

      <!-- Body -->
      <tr>
        <td style='padding:24px 36px 32px;'>
          <p style='font-size:16px;color:#1e293b;margin:0 0 12px;'>
            Hi <strong>{driverName}</strong>,
          </p>
          <p style='font-size:14px;color:#475569;line-height:1.7;margin:0 0 16px;'>
            {(approved
                        ? "Great news! Your driver registration on <strong>DMS</strong> has been reviewed and <strong style='color:#16a34a;'>approved</strong> by the admin team. You can now log in and get started."
                        : $"Your driver registration on <strong>DMS</strong> has been <strong style='color:#dc2626;'>{status.ToLower()}</strong> by the admin team."
                    )}
          </p>

          <!-- Info Row -->
          <table width='100%' cellpadding='0' cellspacing='0'
                 style='background:#f8fafc;border:1px solid #e2e8f0;border-radius:10px;padding:16px;margin:0 0 16px;'>
            <tr>
              <td style='padding:6px 12px;width:50%;'>
                <div style='font-size:11px;color:#94a3b8;text-transform:uppercase;letter-spacing:0.5px;'>Driver Name</div>
                <div style='font-size:14px;font-weight:600;color:#1e293b;margin-top:2px;'>{driverName}</div>
              </td>
              <td style='padding:6px 12px;width:50%;'>
                <div style='font-size:11px;color:#94a3b8;text-transform:uppercase;letter-spacing:0.5px;'>Username</div>
                <div style='font-size:14px;font-weight:600;color:#1e293b;margin-top:2px;'>{username}</div>
              </td>
            </tr>
            <tr>
              <td colspan='2' style='padding:6px 12px;'>
                <div style='font-size:11px;color:#94a3b8;text-transform:uppercase;letter-spacing:0.5px;'>Status Updated</div>
                <div style='font-size:14px;font-weight:600;color:{statusColor};margin-top:2px;'>{statusLabel}</div>
              </td>
            </tr>
          </table>

          {actionBlock}

          <p style='font-size:13px;color:#94a3b8;margin:20px 0 0;border-top:1px solid #f1f5f9;padding-top:16px;'>
            This is an automated message from DMS. Please do not reply to this email.
          </p>
        </td>
      </tr>

      <!-- Footer -->
      <tr>
        <td style='background:#0a1628;padding:16px 36px;text-align:center;'>
          <p style='color:rgba(255,255,255,0.4);font-size:11px;margin:0;'>
            © {DateTime.Now.Year} DMS Management System · All rights reserved
          </p>
        </td>
      </tr>

    </table>
  </td></tr>
</table>
</body>
</html>";

            SendEmail(toEmail, subject, body);
        }


        // ── 2. Vehicle approval / rejection email ────────────────
        public void SendVehicleStatusEmail(string toEmail, string driverName,
                                            string vehicleNumber, string status)
        {
            if (string.IsNullOrWhiteSpace(toEmail)) return;

            bool approved = status == "Approved";

            string subject = approved
                ? "✅ DMS — Your Vehicle Has Been Approved"
                : "❌ DMS — Your Vehicle Registration Update";

            string statusColor = approved ? "#16a34a" : "#dc2626";
            string statusBg = approved ? "#f0fdf4" : "#fef2f2";
            string statusBorder = approved ? "#bbf7d0" : "#fecaca";
            string statusLabel = approved ? "APPROVED" : status.ToUpper();
            string statusIcon = approved ? "✅" : "❌";

            string actionBlock = approved ? @"
        <div style='background:#eff6ff;border:1px solid #bfdbfe;border-radius:10px;padding:16px 20px;margin:20px 0;'>
            <p style='margin:0 0 8px;font-weight:600;color:#1e40af;'>You are all set!</p>
            <ul style='margin:0;padding-left:20px;color:#374151;font-size:14px;line-height:1.8;'>
                <li>Your vehicle is now active in the DMS system</li>
                <li>You may receive milk transfer assignments to your vehicle</li>
                <li>Log in to your dashboard to view upcoming transfers</li>
            </ul>
        </div>" : @"
        <div style='background:#fef9c3;border:1px solid #fde047;border-radius:10px;padding:16px 20px;margin:20px 0;'>
            <p style='margin:0;color:#713f12;font-size:14px;'>
                If you believe this is a mistake, please contact the DMS admin team
                or register a different vehicle from your dashboard.
            </p>
        </div>";

            string body = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'/></head>
<body style='margin:0;padding:0;background:#f1f5f9;font-family:Segoe UI,Arial,sans-serif;'>
<table width='100%' cellpadding='0' cellspacing='0' style='padding:40px 20px;'>
  <tr><td align='center'>
    <table width='560' cellpadding='0' cellspacing='0'
           style='background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.08);'>

      <!-- Header -->
      <tr>
        <td style='background:linear-gradient(135deg,#0a1628,#1e40af);padding:32px 36px;text-align:center;'>
          <div style='font-size:28px;margin-bottom:8px;'>🚛</div>
          <div style='font-family:Georgia,serif;font-size:22px;font-weight:600;color:#ffffff;letter-spacing:-0.3px;'>
            DMS
          </div>
          <div style='color:rgba(255,255,255,0.65);font-size:12px;margin-top:4px;letter-spacing:1px;text-transform:uppercase;'>
            Vehicle Registration Update
          </div>
        </td>
      </tr>

      <!-- Status Badge -->
      <tr>
        <td style='padding:28px 36px 0;text-align:center;'>
          <div style='display:inline-block;background:{statusBg};border:1.5px solid {statusBorder};
                      border-radius:50px;padding:8px 24px;'>
            <span style='color:{statusColor};font-weight:700;font-size:13px;letter-spacing:1px;'>
              {statusIcon} &nbsp; {statusLabel}
            </span>
          </div>
        </td>
      </tr>

      <!-- Body -->
      <tr>
        <td style='padding:24px 36px 32px;'>
          <p style='font-size:16px;color:#1e293b;margin:0 0 12px;'>
            Hi <strong>{driverName}</strong>,
          </p>
          <p style='font-size:14px;color:#475569;line-height:1.7;margin:0 0 16px;'>
            {(approved
                        ? $"Your vehicle <strong>{vehicleNumber}</strong> has been reviewed and <strong style='color:#16a34a;'>approved</strong> by the admin team. It is now active in the DMS system."
                        : $"Your vehicle <strong>{vehicleNumber}</strong> registration has been <strong style='color:#dc2626;'>{status.ToLower()}</strong> by the admin team."
                    )}
          </p>

          <!-- Vehicle Info -->
          <table width='100%' cellpadding='0' cellspacing='0'
                 style='background:#f8fafc;border:1px solid #e2e8f0;border-radius:10px;padding:16px;margin:0 0 16px;'>
            <tr>
              <td style='padding:6px 12px;width:50%;'>
                <div style='font-size:11px;color:#94a3b8;text-transform:uppercase;letter-spacing:0.5px;'>Driver</div>
                <div style='font-size:14px;font-weight:600;color:#1e293b;margin-top:2px;'>{driverName}</div>
              </td>
              <td style='padding:6px 12px;width:50%;'>
                <div style='font-size:11px;color:#94a3b8;text-transform:uppercase;letter-spacing:0.5px;'>Vehicle Number</div>
                <div style='font-size:14px;font-weight:700;color:#1e293b;margin-top:2px;
                            letter-spacing:0.05em;'>{vehicleNumber}</div>
              </td>
            </tr>
            <tr>
              <td colspan='2' style='padding:6px 12px;'>
                <div style='font-size:11px;color:#94a3b8;text-transform:uppercase;letter-spacing:0.5px;'>Vehicle Status</div>
                <div style='font-size:14px;font-weight:600;color:{statusColor};margin-top:2px;'>{statusLabel}</div>
              </td>
            </tr>
          </table>

          {actionBlock}

          <p style='font-size:13px;color:#94a3b8;margin:20px 0 0;border-top:1px solid #f1f5f9;padding-top:16px;'>
            This is an automated message from DMS. Please do not reply to this email.
          </p>
        </td>
      </tr>

      <!-- Footer -->
      <tr>
        <td style='background:#0a1628;padding:16px 36px;text-align:center;'>
          <p style='color:rgba(255,255,255,0.4);font-size:11px;margin:0;'>
            © {DateTime.Now.Year} DMS Management System · All rights reserved
          </p>
        </td>
      </tr>

    </table>
  </td></tr>
</table>
</body>
</html>";

            SendEmail(toEmail, subject, body);
        }


        // ── 3. Private shared SendEmail helper ───────────────────
        // If you already have this from SendOtpEmail, skip it.
        // Otherwise add this private method to AdminRepository.
        private void SendEmail(string toEmail, string subject, string htmlBody)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = subject;
                message.Body = new TextPart("html") { Text = htmlBody };

                using var client = new MailKit.Net.Smtp.SmtpClient();
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                client.Connect(_settings.SmtpHost, _settings.SmtpPort,
                               MailKit.Security.SecureSocketOptions.StartTls);
                client.Authenticate(_settings.SenderEmail, _settings.AppPassword);
                client.Send(message);
                client.Disconnect(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Email] Failed to send to {toEmail}: {ex.Message}");
            }
        }
    }
}