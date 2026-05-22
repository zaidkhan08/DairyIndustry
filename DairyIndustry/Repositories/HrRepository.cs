using DairyIndustry.Data;
using DairyIndustry.Models;
using Microsoft.Data.SqlClient;

namespace DairyIndustry.Repositories
{
    public class HRRepository : IHRRepository
    {
        private readonly DbHelper _db;

        public HRRepository(DbHelper db)
        {
            _db = db;
        }

        // ═══════════════════════════════════════════════════════════
        //  1. GET ALL STAFF — SP usp_HR_GetStaff
        //  Used by HR Manager (full access)
        // ═══════════════════════════════════════════════════════════
        public List<StaffModel> GetAllStaff(int? roleId, bool? isActive)
        {
            var list = new List<StaffModel>();

            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand("HR.usp_HR_GetStaff", con);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@RoleId", (object?)roleId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IsActive", (object?)isActive ?? DBNull.Value);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(MapStaffList(reader));

            return list;
        }

        // ═══════════════════════════════════════════════════════════
        //  2. GET STAFF BY PLANT — inline query
        //
        //  NEW — Used by Plant Manager. Returns ONLY staff where
        //  HR.Staffs.PlantId = @PlantId (forced from session).
        //  roleId and isActive filters still work within that plant.
        //  PlantId comes from the controller session — never from URL.
        //
        //  Joins the same tables as usp_HR_GetStaff so the returned
        //  StaffModel is identical and works with the same Index view.
        // ═══════════════════════════════════════════════════════════
        public List<StaffModel> GetStaffByPlant(int plantId, int? roleId, bool? isActive)
        {
            var list = new List<StaffModel>();

            string query = @"
                SELECT
                    s.StaffId, s.FirstName, s.LastName, s.Phone, s.Email,
                    s.RoleId, r.RoleName,
                    s.DOJ, s.IsActive, s.ProfilePhoto,
                    s.BankAccountId,
                    ba.BankName, ba.AccountNumber, ba.IFSCCode,
                    s.PlantId, pp.PlantName,
                    s.CenterId, cc.CenterName,
                    s.Salary
                FROM HR.Staffs s
                LEFT JOIN Admin.Roles                  r  ON r.RoleId          = s.RoleId
                LEFT JOIN Finance.BankAccounts         ba ON ba.BankAccountId  = s.BankAccountId
                LEFT JOIN Production.ProcessingPlants  pp ON pp.PlantId        = s.PlantId
                LEFT JOIN Collection.CollectionCenters cc ON cc.CenterId       = s.CenterId
                WHERE s.PlantId = @PlantId
                  AND (@RoleId   IS NULL OR s.RoleId   = @RoleId)
                  AND (@IsActive IS NULL OR s.IsActive = @IsActive)
                ORDER BY s.FirstName, s.LastName";

            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@PlantId", plantId);
            cmd.Parameters.AddWithValue("@RoleId", (object?)roleId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IsActive", (object?)isActive ?? DBNull.Value);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(MapStaffList(reader));

            return list;
        }

        // ═══════════════════════════════════════════════════════════
        //  3. GET STAFF BY ID — SP usp_HR_GetStaffById
        //  Also fetches login account info from Admin.Users
        // ═══════════════════════════════════════════════════════════
        public StaffModel? GetStaffById(int staffId)
        {
            using var con = _db.GetConnection();
            con.Open();

            StaffModel? staff = null;
            using (var cmd = new SqlCommand("HR.usp_HR_GetStaffById", con))
            {
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@StaffId", staffId);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                    staff = MapStaffFull(reader);
            }

            if (staff == null) return null;

            // Feature 1 — login account status
            try
            {
                using var loginCmd = new SqlCommand(@"
                    SELECT TOP 1 Username, IsActive, CreatedDate
                    FROM Admin.Users
                    WHERE StaffId = @StaffId", con);
                loginCmd.Parameters.AddWithValue("@StaffId", staffId);
                using var lr = loginCmd.ExecuteReader();
                if (lr.Read())
                {
                    staff.HasLoginAccount = true;
                    staff.LoginUsername = lr["Username"].ToString();
                    staff.IsLoginActive = Convert.ToBoolean(lr["IsActive"]);
                    staff.LoginCreatedDate = lr["CreatedDate"] == DBNull.Value
                                                ? null
                                                : Convert.ToDateTime(lr["CreatedDate"]);
                }
            }
            catch { /* login status is supplementary — never block details load */ }

            return staff;
        }

        // ═══════════════════════════════════════════════════════════
        //  4. ADD STAFF — SP usp_HR_AddStaff
        //  Duplicate phone/email check before calling SP
        // ═══════════════════════════════════════════════════════════
        public int AddStaff(StaffFormModel model)
        {
            using var con = _db.GetConnection();
            con.Open();

            if (!string.IsNullOrWhiteSpace(model.Phone))
            {
                using var dup = new SqlCommand(
                    "SELECT COUNT(1) FROM HR.Staffs WHERE Phone = @Phone", con);
                dup.Parameters.AddWithValue("@Phone", model.Phone.Trim());
                if (Convert.ToInt32(dup.ExecuteScalar()) > 0)
                    throw new InvalidOperationException(
                        $"Phone number '{model.Phone}' is already registered to another staff member.");
            }

            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                using var dup = new SqlCommand(
                    "SELECT COUNT(1) FROM HR.Staffs WHERE Email = @Email", con);
                dup.Parameters.AddWithValue("@Email", model.Email.Trim());
                if (Convert.ToInt32(dup.ExecuteScalar()) > 0)
                    throw new InvalidOperationException(
                        $"Email address '{model.Email}' is already registered to another staff member.");
            }

            bool hasBankDetails = !string.IsNullOrWhiteSpace(model.BankName)
                                && !string.IsNullOrWhiteSpace(model.AccountNumber)
                                && !string.IsNullOrWhiteSpace(model.IFSCCode);

            using var cmd = new SqlCommand("HR.usp_HR_AddStaff", con);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@FirstName", (object?)model.FirstName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@LastName", (object?)model.LastName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Phone", (object?)model.Phone ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Email", (object?)model.Email ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@RoleId", model.RoleId);
            cmd.Parameters.AddWithValue("@DOJ", (object?)model.DOJ ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ProfilePhoto", (object?)model.ProfilePhoto ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PlantId", (object?)model.PlantId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CenterId", (object?)model.CenterId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Salary", (object?)model.Salary ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BankName",
                hasBankDetails ? model.BankName!.Trim() : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@AccountNumber",
                hasBankDetails ? model.AccountNumber!.Trim() : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@IFSCCode",
                hasBankDetails ? model.IFSCCode!.Trim() : (object)DBNull.Value);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
                return Convert.ToInt32(reader["NewStaffId"]);

            throw new Exception("Failed to retrieve new StaffId after insert.");
        }

        // ═══════════════════════════════════════════════════════════
        //  5. UPDATE STAFF — inline UPDATE with bank upsert
        //  Duplicate phone/email check excludes current StaffId
        // ═══════════════════════════════════════════════════════════
        public bool UpdateStaff(StaffFormModel model)
        {
            using var con = _db.GetConnection();
            con.Open();

            if (!string.IsNullOrWhiteSpace(model.Phone))
            {
                using var dup = new SqlCommand(
                    "SELECT COUNT(1) FROM HR.Staffs WHERE Phone = @Phone AND StaffId <> @StaffId", con);
                dup.Parameters.AddWithValue("@Phone", model.Phone.Trim());
                dup.Parameters.AddWithValue("@StaffId", model.StaffId);
                if (Convert.ToInt32(dup.ExecuteScalar()) > 0)
                    throw new InvalidOperationException(
                        $"Phone number '{model.Phone}' is already registered to another staff member.");
            }

            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                using var dup = new SqlCommand(
                    "SELECT COUNT(1) FROM HR.Staffs WHERE Email = @Email AND StaffId <> @StaffId", con);
                dup.Parameters.AddWithValue("@Email", model.Email.Trim());
                dup.Parameters.AddWithValue("@StaffId", model.StaffId);
                if (Convert.ToInt32(dup.ExecuteScalar()) > 0)
                    throw new InvalidOperationException(
                        $"Email address '{model.Email}' is already registered to another staff member.");
            }

            using var tran = con.BeginTransaction();
            try
            {
                bool hasBankDetails = !string.IsNullOrWhiteSpace(model.BankName)
                                   && !string.IsNullOrWhiteSpace(model.AccountNumber)
                                   && !string.IsNullOrWhiteSpace(model.IFSCCode);

                if (hasBankDetails)
                {
                    using var chk = new SqlCommand(
                        "SELECT BankAccountId FROM HR.Staffs WHERE StaffId = @StaffId", con, tran);
                    chk.Parameters.AddWithValue("@StaffId", model.StaffId);
                    var existingBankId = chk.ExecuteScalar();

                    if (existingBankId != null && existingBankId != DBNull.Value)
                    {
                        using var updBank = new SqlCommand(@"
                            UPDATE Finance.BankAccounts
                            SET BankName = @BankName, AccountNumber = @AccountNumber, IFSCCode = @IFSCCode
                            WHERE BankAccountId = @BankAccountId", con, tran);
                        updBank.Parameters.AddWithValue("@BankAccountId", Convert.ToInt32(existingBankId));
                        updBank.Parameters.AddWithValue("@BankName", model.BankName!.Trim());
                        updBank.Parameters.AddWithValue("@AccountNumber", model.AccountNumber!.Trim());
                        updBank.Parameters.AddWithValue("@IFSCCode", model.IFSCCode!.Trim());
                        updBank.ExecuteNonQuery();
                    }
                    else
                    {
                        using var insBank = new SqlCommand(@"
                            DECLARE @NewBankId INT;
                            INSERT INTO Finance.BankAccounts (BankName, AccountNumber, IFSCCode)
                            VALUES (@BankName, @AccountNumber, @IFSCCode);
                            SET @NewBankId = SCOPE_IDENTITY();
                            UPDATE HR.Staffs SET BankAccountId = @NewBankId WHERE StaffId = @StaffId;",
                            con, tran);
                        insBank.Parameters.AddWithValue("@BankName", model.BankName!.Trim());
                        insBank.Parameters.AddWithValue("@AccountNumber", model.AccountNumber!.Trim());
                        insBank.Parameters.AddWithValue("@IFSCCode", model.IFSCCode!.Trim());
                        insBank.Parameters.AddWithValue("@StaffId", model.StaffId);
                        insBank.ExecuteNonQuery();
                    }
                }

                using var staffCmd = new SqlCommand(@"
                    UPDATE HR.Staffs
                    SET
                        FirstName    = @FirstName,
                        LastName     = @LastName,
                        Phone        = @Phone,
                        Email        = @Email,
                        RoleId       = @RoleId,
                        DOJ          = @DOJ,
                        IsActive     = @IsActive,
                        PlantId      = @PlantId,
                        CenterId     = @CenterId,
                        Salary       = @Salary,
                        ProfilePhoto = CASE WHEN @ProfilePhoto IS NOT NULL
                                           THEN @ProfilePhoto ELSE ProfilePhoto END
                    WHERE StaffId = @StaffId", con, tran);

                staffCmd.Parameters.AddWithValue("@StaffId", model.StaffId);
                staffCmd.Parameters.AddWithValue("@FirstName", (object?)model.FirstName ?? DBNull.Value);
                staffCmd.Parameters.AddWithValue("@LastName", (object?)model.LastName ?? DBNull.Value);
                staffCmd.Parameters.AddWithValue("@Phone", (object?)model.Phone ?? DBNull.Value);
                staffCmd.Parameters.AddWithValue("@Email", (object?)model.Email ?? DBNull.Value);
                staffCmd.Parameters.AddWithValue("@RoleId", model.RoleId);
                staffCmd.Parameters.AddWithValue("@DOJ", (object?)model.DOJ ?? DBNull.Value);
                staffCmd.Parameters.AddWithValue("@IsActive", model.IsActive);
                staffCmd.Parameters.AddWithValue("@PlantId", (object?)model.PlantId ?? DBNull.Value);
                staffCmd.Parameters.AddWithValue("@CenterId", (object?)model.CenterId ?? DBNull.Value);
                staffCmd.Parameters.AddWithValue("@Salary", (object?)model.Salary ?? DBNull.Value);
                staffCmd.Parameters.AddWithValue("@ProfilePhoto", (object?)model.ProfilePhoto ?? DBNull.Value);

                bool result = staffCmd.ExecuteNonQuery() > 0;
                tran.Commit();
                return result;
            }
            catch { tran.Rollback(); throw; }
        }

        // ═══════════════════════════════════════════════════════════
        //  6. TOGGLE ACTIVE
        // ═══════════════════════════════════════════════════════════
        public bool ToggleActive(int staffId, bool isActive)
        {
            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand("HR.usp_HR_ToggleActive", con);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@StaffId", staffId);
            cmd.Parameters.AddWithValue("@IsActive", isActive);
            cmd.ExecuteNonQuery();
            return true;
        }

        // ═══════════════════════════════════════════════════════════
        //  7. UPDATE PROFILE PHOTO
        // ═══════════════════════════════════════════════════════════
        public bool UpdateProfilePhoto(int staffId, string photoPath)
        {
            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand("HR.usp_HR_UpdateProfilePhoto", con);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@StaffId", staffId);
            cmd.Parameters.AddWithValue("@ProfilePhoto", photoPath);
            cmd.ExecuteNonQuery();
            return true;
        }

        // ═══════════════════════════════════════════════════════════
        //  8. REGISTER STAFF USER
        // ═══════════════════════════════════════════════════════════
        public void RegisterStaffUser(string username, string passwordHash, int roleId, int staffId)
        {
            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand("Admin.usp_Admin_RegisterUser", con);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
            cmd.Parameters.AddWithValue("@RoleId", roleId);
            cmd.Parameters.AddWithValue("@StaffId", staffId);
            try { cmd.ExecuteNonQuery(); }
            catch (SqlException ex) when (ex.Message.Contains("Username already exists"))
            {
                throw new InvalidOperationException(
                    $"Username '{username}' is already taken. Please choose a different username.");
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  9. ADD SALARY HISTORY
        // ═══════════════════════════════════════════════════════════
        public void AddSalaryHistory(int staffId, decimal? oldSalary, decimal newSalary,
                                     string? reason, string? changedBy)
        {
            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(@"
                INSERT INTO HR.SalaryHistory
                    (StaffId, OldSalary, NewSalary, ChangedDate, Reason, ChangedBy)
                VALUES
                    (@StaffId, @OldSalary, @NewSalary, GETDATE(), @Reason, @ChangedBy)", con);
            cmd.Parameters.AddWithValue("@StaffId", staffId);
            cmd.Parameters.AddWithValue("@OldSalary", (object?)oldSalary ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@NewSalary", newSalary);
            cmd.Parameters.AddWithValue("@Reason", (object?)reason ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ChangedBy", (object?)changedBy ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        // ═══════════════════════════════════════════════════════════
        //  10. GET SALARY HISTORY
        // ═══════════════════════════════════════════════════════════
        public List<SalaryHistoryModel> GetSalaryHistory(int staffId)
        {
            var list = new List<SalaryHistoryModel>();
            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(@"
                SELECT HistoryId, StaffId, OldSalary, NewSalary,
                       ChangedDate, Reason, ChangedBy
                FROM HR.SalaryHistory
                WHERE StaffId = @StaffId
                ORDER BY ChangedDate DESC", con);
            cmd.Parameters.AddWithValue("@StaffId", staffId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(new SalaryHistoryModel
                {
                    HistoryId = Convert.ToInt32(reader["HistoryId"]),
                    StaffId = Convert.ToInt32(reader["StaffId"]),
                    OldSalary = reader["OldSalary"] == DBNull.Value ? null : Convert.ToDecimal(reader["OldSalary"]),
                    NewSalary = Convert.ToDecimal(reader["NewSalary"]),
                    ChangedDate = Convert.ToDateTime(reader["ChangedDate"]),
                    Reason = reader["Reason"] == DBNull.Value ? null : reader["Reason"].ToString(),
                    ChangedBy = reader["ChangedBy"] == DBNull.Value ? null : reader["ChangedBy"].ToString()
                });
            return list;
        }

        // ═══════════════════════════════════════════════════════════
        //  11. ADD STAFF NOTE
        // ═══════════════════════════════════════════════════════════
        public void AddStaffNote(int staffId, string noteText,
                                 string noteType, string? createdBy)
        {
            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(@"
                INSERT INTO HR.StaffNotes
                    (StaffId, NoteText, NoteType, CreatedDate, CreatedBy)
                VALUES
                    (@StaffId, @NoteText, @NoteType, GETDATE(), @CreatedBy)", con);
            cmd.Parameters.AddWithValue("@StaffId", staffId);
            cmd.Parameters.AddWithValue("@NoteText", noteText.Trim());
            cmd.Parameters.AddWithValue("@NoteType", noteType);
            cmd.Parameters.AddWithValue("@CreatedBy", (object?)createdBy ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        // ═══════════════════════════════════════════════════════════
        //  12. GET STAFF NOTES
        // ═══════════════════════════════════════════════════════════
        public List<StaffNoteModel> GetStaffNotes(int staffId)
        {
            var list = new List<StaffNoteModel>();
            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(@"
                SELECT NoteId, StaffId, NoteText, NoteType, CreatedDate, CreatedBy
                FROM HR.StaffNotes
                WHERE StaffId = @StaffId
                ORDER BY CreatedDate DESC", con);
            cmd.Parameters.AddWithValue("@StaffId", staffId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(new StaffNoteModel
                {
                    NoteId = Convert.ToInt32(reader["NoteId"]),
                    StaffId = Convert.ToInt32(reader["StaffId"]),
                    NoteText = reader["NoteText"].ToString()!,
                    NoteType = reader["NoteType"].ToString()!,
                    CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                    CreatedBy = reader["CreatedBy"] == DBNull.Value ? null : reader["CreatedBy"].ToString()
                });
            return list;
        }

        // ═══════════════════════════════════════════════════════════
        //  13. DELETE STAFF NOTE
        // ═══════════════════════════════════════════════════════════
        public bool DeleteStaffNote(int noteId, int staffId)
        {
            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(@"
                DELETE FROM HR.StaffNotes
                WHERE NoteId = @NoteId AND StaffId = @StaffId", con);
            cmd.Parameters.AddWithValue("@NoteId", noteId);
            cmd.Parameters.AddWithValue("@StaffId", staffId);
            return cmd.ExecuteNonQuery() > 0;
        }

        // ═══════════════════════════════════════════════════════════
        //  14. GET ROLES
        // ═══════════════════════════════════════════════════════════
        public List<RoleModel> GetRoles()
        {
            var list = new List<RoleModel>();
            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(
                "SELECT RoleId, RoleName FROM Admin.Roles ORDER BY RoleName", con);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(new RoleModel
                {
                    RoleId = Convert.ToInt32(reader["RoleId"]),
                    RoleName = reader["RoleName"].ToString()
                });
            return list;
        }

        // ═══════════════════════════════════════════════════════════
        //  15. GET PLANTS
        // ═══════════════════════════════════════════════════════════
        public List<PlantAssignModel> GetPlants()
        {
            var list = new List<PlantAssignModel>();
            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(
                "SELECT PlantId, PlantName, Location FROM Production.ProcessingPlants WHERE IsActive = 1 ORDER BY PlantName", con);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(new PlantAssignModel
                {
                    PlantId = Convert.ToInt32(reader["PlantId"]),
                    PlantName = reader["PlantName"].ToString(),
                    Location = reader["Location"] == DBNull.Value ? null : reader["Location"].ToString()
                });
            return list;
        }

        // ═══════════════════════════════════════════════════════════
        //  16. GET CENTERS
        // ═══════════════════════════════════════════════════════════
        public List<CenterAssignModel> GetCenters()
        {
            var list = new List<CenterAssignModel>();
            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(
                "SELECT CenterId, CenterName, Location FROM Collection.CollectionCenters ORDER BY CenterName", con);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(new CenterAssignModel
                {
                    CenterId = Convert.ToInt32(reader["CenterId"]),
                    CenterName = reader["CenterName"].ToString(),
                    Location = reader["Location"] == DBNull.Value ? null : reader["Location"].ToString()
                });
            return list;
        }

        // ═══════════════════════════════════════════════════════════
        //  17. DASHBOARD SUMMARY
        // ═══════════════════════════════════════════════════════════
        public HRDashboardSummaryModel GetDashboardSummary()
        {
            var model = new HRDashboardSummaryModel();

            using var con = _db.GetConnection();
            con.Open();

            using (var cmd = new SqlCommand(@"
                SELECT
                    COUNT(*)                                                       AS TotalStaff,
                    SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END)                 AS ActiveStaff,
                    SUM(CASE WHEN IsActive = 0 THEN 1 ELSE 0 END)                 AS InactiveStaff,
                    SUM(CASE WHEN MONTH(DOJ) = MONTH(GETDATE())
                              AND YEAR(DOJ)  = YEAR(GETDATE()) THEN 1 ELSE 0 END) AS NewJoiningThisMonth
                FROM HR.Staffs", con))
            using (var r = cmd.ExecuteReader())
            {
                if (r.Read())
                {
                    model.TotalStaff = r["TotalStaff"] == DBNull.Value ? 0 : Convert.ToInt32(r["TotalStaff"]);
                    model.ActiveStaff = r["ActiveStaff"] == DBNull.Value ? 0 : Convert.ToInt32(r["ActiveStaff"]);
                    model.InactiveStaff = r["InactiveStaff"] == DBNull.Value ? 0 : Convert.ToInt32(r["InactiveStaff"]);
                    model.NewJoiningThisMonth = r["NewJoiningThisMonth"] == DBNull.Value ? 0 : Convert.ToInt32(r["NewJoiningThisMonth"]);
                }
            }

            using (var cmd = new SqlCommand(@"
                SELECT COUNT(*) AS PendingCount, ISNULL(SUM(TotalAmount),0) AS PendingAmount
                FROM Finance.StaffPayments WHERE PaymentStatus = 'Pending'", con))
            using (var r = cmd.ExecuteReader())
            {
                if (r.Read())
                {
                    model.PendingPaymentsCount = r["PendingCount"] == DBNull.Value ? 0 : Convert.ToInt32(r["PendingCount"]);
                    model.PendingPaymentsAmount = r["PendingAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(r["PendingAmount"]);
                }
            }

            return model;
        }

        // ═══════════════════════════════════════════════════════════
        //  18. STAFF BY TYPE
        // ═══════════════════════════════════════════════════════════
        public List<StaffTypeCountModel> GetStaffByType()
        {
            var list = new List<StaffTypeCountModel>();
            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(@"
                SELECT
                    ISNULL(r.RoleName, 'Not Assigned')              AS StaffType,
                    COUNT(*)                                         AS Count,
                    SUM(CASE WHEN s.IsActive = 1 THEN 1 ELSE 0 END) AS ActiveCount
                FROM HR.Staffs s
                LEFT JOIN Admin.Roles r ON r.RoleId = s.RoleId
                GROUP BY r.RoleName
                ORDER BY Count DESC", con);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(new StaffTypeCountModel
                {
                    StaffType = reader["StaffType"].ToString(),
                    Count = Convert.ToInt32(reader["Count"]),
                    ActiveCount = Convert.ToInt32(reader["ActiveCount"])
                });
            return list;
        }

        // ═══════════════════════════════════════════════════════════
        //  19. GET PAYMENTS BY STAFF — LEFT JOIN on ProcessingPlants
        // ═══════════════════════════════════════════════════════════
        public List<StaffPaymentModel> GetPaymentsByStaff(int staffId)
        {
            var list = new List<StaffPaymentModel>();
            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(@"
                SELECT
                    sp.PaymentId, sp.StaffId, sp.PlantId,
                    s.FirstName + ' ' + s.LastName           AS StaffName,
                    ISNULL(r.RoleName, 'Not Assigned')        AS StaffType,
                    ISNULL(pp.PlantName, 'N/A')               AS PlantName,
                    sp.FromDate, sp.ToDate, sp.TotalAmount,
                    sp.PaymentDate, sp.PaymentStatus
                FROM Finance.StaffPayments sp
                INNER JOIN HR.Staffs                   s  ON s.StaffId  = sp.StaffId
                LEFT  JOIN Admin.Roles                 r  ON r.RoleId   = s.RoleId
                LEFT  JOIN Production.ProcessingPlants pp ON pp.PlantId = sp.PlantId
                WHERE sp.StaffId = @StaffId
                ORDER BY sp.PaymentDate DESC", con);
            cmd.Parameters.AddWithValue("@StaffId", staffId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) list.Add(MapPayment(reader));
            return list;
        }

        // ═══════════════════════════════════════════════════════════
        //  20. GET ALL PAYMENTS — LEFT JOIN on ProcessingPlants
        // ═══════════════════════════════════════════════════════════
        public List<StaffPaymentModel> GetAllPayments(string? status)
        {
            var list = new List<StaffPaymentModel>();
            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(@"
                SELECT
                    sp.PaymentId, sp.StaffId, sp.PlantId,
                    s.FirstName + ' ' + s.LastName           AS StaffName,
                    ISNULL(r.RoleName, 'Not Assigned')        AS StaffType,
                    ISNULL(pp.PlantName, 'N/A')               AS PlantName,
                    sp.FromDate, sp.ToDate, sp.TotalAmount,
                    sp.PaymentDate, sp.PaymentStatus
                FROM Finance.StaffPayments sp
                INNER JOIN HR.Staffs                   s  ON s.StaffId  = sp.StaffId
                LEFT  JOIN Admin.Roles                 r  ON r.RoleId   = s.RoleId
                LEFT  JOIN Production.ProcessingPlants pp ON pp.PlantId = sp.PlantId
                WHERE (@Status IS NULL OR sp.PaymentStatus = @Status)
                ORDER BY sp.PaymentDate DESC", con);
            cmd.Parameters.AddWithValue("@Status", (object?)status ?? DBNull.Value);
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) list.Add(MapPayment(reader));
            return list;
        }

        // ═══════════════════════════════════════════════════════════
        //  PRIVATE MAPPERS
        // ═══════════════════════════════════════════════════════════
        private StaffModel MapStaffList(SqlDataReader r)
        {
            return new StaffModel
            {
                StaffId = Convert.ToInt32(r["StaffId"]),
                FirstName = r["FirstName"].ToString(),
                LastName = r["LastName"].ToString(),
                Phone = r["Phone"] == DBNull.Value ? null : r["Phone"].ToString(),
                Email = r["Email"] == DBNull.Value ? null : r["Email"].ToString(),
                RoleId = r["RoleId"] == DBNull.Value ? 0 : Convert.ToInt32(r["RoleId"]),
                RoleName = r["RoleName"] == DBNull.Value ? null : r["RoleName"].ToString(),
                DOJ = r["DOJ"] == DBNull.Value ? null : Convert.ToDateTime(r["DOJ"]),
                IsActive = Convert.ToBoolean(r["IsActive"]),
                ProfilePhoto = r["ProfilePhoto"] == DBNull.Value ? null : r["ProfilePhoto"].ToString(),
                Salary = r["Salary"] == DBNull.Value ? null : Convert.ToDecimal(r["Salary"]),
                PlantId = r["PlantId"] == DBNull.Value ? null : Convert.ToInt32(r["PlantId"]),
                PlantName = r["PlantName"] == DBNull.Value ? null : r["PlantName"].ToString(),
                CenterId = r["CenterId"] == DBNull.Value ? null : Convert.ToInt32(r["CenterId"]),
                CenterName = r["CenterName"] == DBNull.Value ? null : r["CenterName"].ToString(),
                BankAccountId = r["BankAccountId"] == DBNull.Value ? null : Convert.ToInt32(r["BankAccountId"]),
                BankName = r["BankName"] == DBNull.Value ? null : r["BankName"].ToString(),
                AccountNumber = r["AccountNumber"] == DBNull.Value ? null : r["AccountNumber"].ToString(),
                IFSCCode = r["IFSCCode"] == DBNull.Value ? null : r["IFSCCode"].ToString()
            };
        }

        private StaffModel MapStaffFull(SqlDataReader r)
        {
            return new StaffModel
            {
                StaffId = Convert.ToInt32(r["StaffId"]),
                FirstName = r["FirstName"].ToString(),
                LastName = r["LastName"].ToString(),
                Phone = r["Phone"] == DBNull.Value ? null : r["Phone"].ToString(),
                Email = r["Email"] == DBNull.Value ? null : r["Email"].ToString(),
                RoleId = r["RoleId"] == DBNull.Value ? 0 : Convert.ToInt32(r["RoleId"]),
                RoleName = r["RoleName"] == DBNull.Value ? null : r["RoleName"].ToString(),
                DOJ = r["DOJ"] == DBNull.Value ? null : Convert.ToDateTime(r["DOJ"]),
                IsActive = Convert.ToBoolean(r["IsActive"]),
                ProfilePhoto = r["ProfilePhoto"] == DBNull.Value ? null : r["ProfilePhoto"].ToString(),
                BankAccountId = r["BankAccountId"] == DBNull.Value ? null : Convert.ToInt32(r["BankAccountId"]),
                BankName = r["BankName"] == DBNull.Value ? null : r["BankName"].ToString(),
                AccountNumber = r["AccountNumber"] == DBNull.Value ? null : r["AccountNumber"].ToString(),
                IFSCCode = r["IFSCCode"] == DBNull.Value ? null : r["IFSCCode"].ToString(),
                PlantId = r["PlantId"] == DBNull.Value ? null : Convert.ToInt32(r["PlantId"]),
                PlantName = r["PlantName"] == DBNull.Value ? null : r["PlantName"].ToString(),
                CenterId = r["CenterId"] == DBNull.Value ? null : Convert.ToInt32(r["CenterId"]),
                CenterName = r["CenterName"] == DBNull.Value ? null : r["CenterName"].ToString(),
                Salary = r["Salary"] == DBNull.Value ? null : Convert.ToDecimal(r["Salary"])
            };
        }

        private StaffPaymentModel MapPayment(SqlDataReader r)
        {
            return new StaffPaymentModel
            {
                PaymentId = Convert.ToInt32(r["PaymentId"]),
                StaffId = Convert.ToInt32(r["StaffId"]),
                PlantId = r["PlantId"] == DBNull.Value ? 0 : Convert.ToInt32(r["PlantId"]),
                StaffName = r["StaffName"].ToString(),
                StaffType = r["StaffType"].ToString(),
                PlantName = r["PlantName"].ToString(),
                FromDate = Convert.ToDateTime(r["FromDate"]),
                ToDate = Convert.ToDateTime(r["ToDate"]),
                TotalAmount = Convert.ToDecimal(r["TotalAmount"]),
                PaymentDate = Convert.ToDateTime(r["PaymentDate"]),
                PaymentStatus = r["PaymentStatus"].ToString()
            };
        }
    }
}