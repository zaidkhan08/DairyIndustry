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
        //  2. GET STAFF BY ID — SP usp_HR_GetStaffById
        //  FEATURE 1 — Also fetches login account status from
        //  Admin.Users by StaffId. Uses a second query on the same
        //  open connection — no extra connection overhead.
        //  This is only done on Details page (single record) so
        //  performance impact is negligible.
        // ═══════════════════════════════════════════════════════════
        public StaffModel? GetStaffById(int staffId)
        {
            using var con = _db.GetConnection();
            con.Open();

            // Step 1 — get full staff record via SP
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

            // Step 2 — FEATURE 1: check if this staff has a login account
            // Queries Admin.Users where StaffId matches.
            // Never throws — if Admin.Users is unavailable, login info
            // stays at default (HasLoginAccount = false).
            try
            {
                using var loginCmd = new SqlCommand(@"
                    SELECT TOP 1
                        Username, IsActive, CreatedDate
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
            catch
            {
                // Silently ignore — login status is supplementary info
                // Staff details page still loads fully without it
            }

            return staff;
        }

        // ═══════════════════════════════════════════════════════════
        //  3. ADD STAFF — SP usp_HR_AddStaff
        //
        //  FIX — Bank details bug root cause:
        //  The previous inline INSERT created the bank account and
        //  staff record in two separate steps. If the second step
        //  failed silently or the BankAccountId was not linked back,
        //  the staff record had BankAccountId = NULL.
        //
        //  Now uses usp_HR_AddStaff SP which:
        //  - Creates bank account + inserts staff in ONE transaction
        //  - Rolls back both if anything fails
        //  - Returns the new StaffId via SELECT SCOPE_IDENTITY()
        //  - Validates RoleId, PlantId, CenterId before inserting
        //
        //  FIX 5 — Duplicate phone/email check before calling SP
        // ═══════════════════════════════════════════════════════════
        public int AddStaff(StaffFormModel model)
        {
            using var con = _db.GetConnection();
            con.Open();

            // Duplicate phone check
            if (!string.IsNullOrWhiteSpace(model.Phone))
            {
                using var dupCmd = new SqlCommand(
                    "SELECT COUNT(1) FROM HR.Staffs WHERE Phone = @Phone", con);
                dupCmd.Parameters.AddWithValue("@Phone", model.Phone.Trim());
                if (Convert.ToInt32(dupCmd.ExecuteScalar()) > 0)
                    throw new InvalidOperationException(
                        $"Phone number '{model.Phone}' is already registered to another staff member.");
            }

            // Duplicate email check
            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                using var dupCmd = new SqlCommand(
                    "SELECT COUNT(1) FROM HR.Staffs WHERE Email = @Email", con);
                dupCmd.Parameters.AddWithValue("@Email", model.Email.Trim());
                if (Convert.ToInt32(dupCmd.ExecuteScalar()) > 0)
                    throw new InvalidOperationException(
                        $"Email address '{model.Email}' is already registered to another staff member.");
            }

            // Use the SP — handles bank + staff in one atomic transaction
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

            // Bank details — pass all 3 or none (SP handles the condition)
            bool hasBankDetails = !string.IsNullOrWhiteSpace(model.BankName)
                                && !string.IsNullOrWhiteSpace(model.AccountNumber)
                                && !string.IsNullOrWhiteSpace(model.IFSCCode);

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
        //  4. UPDATE STAFF — inline UPDATE
        //
        //  usp_HR_UpdateStaff SP is intentionally NOT used here
        //  because it only updates 6 fields and misses DOJ, IsActive,
        //  Salary, PlantId, CenterId, and bank details entirely.
        //  The inline approach covers all fields correctly.
        //
        //  Bank account: upsert — updates if exists, creates if not.
        //  FIX 5 — Duplicate phone/email check excludes current StaffId.
        // ═══════════════════════════════════════════════════════════
        public bool UpdateStaff(StaffFormModel model)
        {
            using var con = _db.GetConnection();
            con.Open();

            // Duplicate phone check (excluding self)
            if (!string.IsNullOrWhiteSpace(model.Phone))
            {
                using var dupCmd = new SqlCommand(
                    "SELECT COUNT(1) FROM HR.Staffs WHERE Phone = @Phone AND StaffId <> @StaffId", con);
                dupCmd.Parameters.AddWithValue("@Phone", model.Phone.Trim());
                dupCmd.Parameters.AddWithValue("@StaffId", model.StaffId);
                if (Convert.ToInt32(dupCmd.ExecuteScalar()) > 0)
                    throw new InvalidOperationException(
                        $"Phone number '{model.Phone}' is already registered to another staff member.");
            }

            // Duplicate email check (excluding self)
            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                using var dupCmd = new SqlCommand(
                    "SELECT COUNT(1) FROM HR.Staffs WHERE Email = @Email AND StaffId <> @StaffId", con);
                dupCmd.Parameters.AddWithValue("@Email", model.Email.Trim());
                dupCmd.Parameters.AddWithValue("@StaffId", model.StaffId);
                if (Convert.ToInt32(dupCmd.ExecuteScalar()) > 0)
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
                    // Check if staff already has a bank account linked
                    using var checkCmd = new SqlCommand(
                        "SELECT BankAccountId FROM HR.Staffs WHERE StaffId = @StaffId", con, tran);
                    checkCmd.Parameters.AddWithValue("@StaffId", model.StaffId);
                    var existingBankId = checkCmd.ExecuteScalar();

                    if (existingBankId != null && existingBankId != DBNull.Value)
                    {
                        // Update existing bank account record
                        using var updBankCmd = new SqlCommand(@"
                            UPDATE Finance.BankAccounts
                            SET BankName = @BankName, AccountNumber = @AccountNumber, IFSCCode = @IFSCCode
                            WHERE BankAccountId = @BankAccountId", con, tran);
                        updBankCmd.Parameters.AddWithValue("@BankAccountId", Convert.ToInt32(existingBankId));
                        updBankCmd.Parameters.AddWithValue("@BankName", model.BankName!.Trim());
                        updBankCmd.Parameters.AddWithValue("@AccountNumber", model.AccountNumber!.Trim());
                        updBankCmd.Parameters.AddWithValue("@IFSCCode", model.IFSCCode!.Trim());
                        updBankCmd.ExecuteNonQuery();
                    }
                    else
                    {
                        // Create new bank account and link it to staff
                        using var insBankCmd = new SqlCommand(@"
                            DECLARE @NewBankId INT;
                            INSERT INTO Finance.BankAccounts (BankName, AccountNumber, IFSCCode)
                            VALUES (@BankName, @AccountNumber, @IFSCCode);
                            SET @NewBankId = SCOPE_IDENTITY();
                            UPDATE HR.Staffs SET BankAccountId = @NewBankId WHERE StaffId = @StaffId;",
                            con, tran);
                        insBankCmd.Parameters.AddWithValue("@BankName", model.BankName!.Trim());
                        insBankCmd.Parameters.AddWithValue("@AccountNumber", model.AccountNumber!.Trim());
                        insBankCmd.Parameters.AddWithValue("@IFSCCode", model.IFSCCode!.Trim());
                        insBankCmd.Parameters.AddWithValue("@StaffId", model.StaffId);
                        insBankCmd.ExecuteNonQuery();
                    }
                }

                // Update all staff fields — covers everything the SP misses
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
                                           THEN @ProfilePhoto
                                           ELSE ProfilePhoto END
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
            catch
            {
                tran.Rollback();
                throw;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  5. REGISTER STAFF USER — SP usp_Admin_RegisterUser
        //
        //  NEW — Called after AddStaff if HR provides credentials.
        //  BCrypt hash is generated in the controller and passed here.
        //  SP already checks for duplicate username and throws if found.
        //  Links the new Admin.Users record to the StaffId via FK.
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

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (SqlException ex) when (ex.Message.Contains("Username already exists"))
            {
                throw new InvalidOperationException(
                    $"Username '{username}' is already taken. Please choose a different username.");
            }
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
        //  8. GET ROLES
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
        //  9. GET PLANTS
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
        //  10. GET CENTERS
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
        //  11. DASHBOARD SUMMARY
        // ═══════════════════════════════════════════════════════════
        public HRDashboardSummaryModel GetDashboardSummary()
        {
            var model = new HRDashboardSummaryModel();

            string staffQuery = @"
                SELECT
                    COUNT(*)                                                      AS TotalStaff,
                    SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END)                AS ActiveStaff,
                    SUM(CASE WHEN IsActive = 0 THEN 1 ELSE 0 END)                AS InactiveStaff,
                    SUM(CASE WHEN MONTH(DOJ) = MONTH(GETDATE())
                              AND YEAR(DOJ)  = YEAR(GETDATE()) THEN 1 ELSE 0 END) AS NewJoiningThisMonth
                FROM HR.Staffs";

            string payQuery = @"
                SELECT COUNT(*) AS PendingCount, ISNULL(SUM(TotalAmount),0) AS PendingAmount
                FROM Finance.StaffPayments
                WHERE PaymentStatus = 'Pending'";

            using var con = _db.GetConnection();
            con.Open();

            using (var cmd = new SqlCommand(staffQuery, con))
            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    model.TotalStaff = reader["TotalStaff"] == DBNull.Value ? 0 : Convert.ToInt32(reader["TotalStaff"]);
                    model.ActiveStaff = reader["ActiveStaff"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ActiveStaff"]);
                    model.InactiveStaff = reader["InactiveStaff"] == DBNull.Value ? 0 : Convert.ToInt32(reader["InactiveStaff"]);
                    model.NewJoiningThisMonth = reader["NewJoiningThisMonth"] == DBNull.Value ? 0 : Convert.ToInt32(reader["NewJoiningThisMonth"]);
                }
            }

            using (var cmd = new SqlCommand(payQuery, con))
            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    model.PendingPaymentsCount = reader["PendingCount"] == DBNull.Value ? 0 : Convert.ToInt32(reader["PendingCount"]);
                    model.PendingPaymentsAmount = reader["PendingAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["PendingAmount"]);
                }
            }

            return model;
        }

        // ═══════════════════════════════════════════════════════════
        //  12. STAFF BY TYPE
        // ═══════════════════════════════════════════════════════════
        public List<StaffTypeCountModel> GetStaffByType()
        {
            var list = new List<StaffTypeCountModel>();

            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(@"
                SELECT
                    ISNULL(r.RoleName, 'Not Assigned')               AS StaffType,
                    COUNT(*)                                          AS Count,
                    SUM(CASE WHEN s.IsActive = 1 THEN 1 ELSE 0 END)  AS ActiveCount
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
        //  13. GET PAYMENTS BY STAFF — LEFT JOIN fix applied
        // ═══════════════════════════════════════════════════════════
        public List<StaffPaymentModel> GetPaymentsByStaff(int staffId)
        {
            var list = new List<StaffPaymentModel>();

            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(@"
                SELECT
                    sp.PaymentId, sp.StaffId, sp.PlantId,
                    s.FirstName + ' ' + s.LastName            AS StaffName,
                    ISNULL(r.RoleName, 'Not Assigned')         AS StaffType,
                    ISNULL(pp.PlantName, 'N/A')                AS PlantName,
                    sp.FromDate, sp.ToDate, sp.TotalAmount,
                    sp.PaymentDate, sp.PaymentStatus
                FROM Finance.StaffPayments sp
                INNER JOIN HR.Staffs                    s  ON s.StaffId  = sp.StaffId
                LEFT  JOIN Admin.Roles                  r  ON r.RoleId   = s.RoleId
                LEFT  JOIN Production.ProcessingPlants  pp ON pp.PlantId = sp.PlantId
                WHERE sp.StaffId = @StaffId
                ORDER BY sp.PaymentDate DESC", con);

            cmd.Parameters.AddWithValue("@StaffId", staffId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(MapPayment(reader));

            return list;
        }

        // ═══════════════════════════════════════════════════════════
        //  14. GET ALL PAYMENTS — LEFT JOIN fix applied
        // ═══════════════════════════════════════════════════════════
        public List<StaffPaymentModel> GetAllPayments(string? status)
        {
            var list = new List<StaffPaymentModel>();

            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(@"
                SELECT
                    sp.PaymentId, sp.StaffId, sp.PlantId,
                    s.FirstName + ' ' + s.LastName            AS StaffName,
                    ISNULL(r.RoleName, 'Not Assigned')         AS StaffType,
                    ISNULL(pp.PlantName, 'N/A')                AS PlantName,
                    sp.FromDate, sp.ToDate, sp.TotalAmount,
                    sp.PaymentDate, sp.PaymentStatus
                FROM Finance.StaffPayments sp
                INNER JOIN HR.Staffs                    s  ON s.StaffId  = sp.StaffId
                LEFT  JOIN Admin.Roles                  r  ON r.RoleId   = s.RoleId
                LEFT  JOIN Production.ProcessingPlants  pp ON pp.PlantId = sp.PlantId
                WHERE (@Status IS NULL OR sp.PaymentStatus = @Status)
                ORDER BY sp.PaymentDate DESC", con);

            cmd.Parameters.AddWithValue("@Status", (object?)status ?? DBNull.Value);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(MapPayment(reader));

            return list;
        }

        // ═══════════════════════════════════════════════════════════
        //  FEATURE 3 — ADD SALARY HISTORY
        //  Called from HrController.Edit POST when salary changes.
        //  Inserts one row into HR.SalaryHistory with old salary,
        //  new salary, reason and who changed it.
        //  If this fails, the staff update is NOT rolled back —
        //  history logging is supplementary and must never block
        //  the main save operation.
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
                    (@StaffId, @OldSalary, @NewSalary, GETDATE(), @Reason, @ChangedBy)",
                con);

            cmd.Parameters.AddWithValue("@StaffId", staffId);
            cmd.Parameters.AddWithValue("@OldSalary", (object?)oldSalary ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@NewSalary", newSalary);
            cmd.Parameters.AddWithValue("@Reason", (object?)reason ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ChangedBy", (object?)changedBy ?? DBNull.Value);

            cmd.ExecuteNonQuery();
        }

        // ═══════════════════════════════════════════════════════════
        //  FEATURE 3 — GET SALARY HISTORY
        //  Returns full salary timeline for a staff member,
        //  ordered most recent first.
        // ═══════════════════════════════════════════════════════════
        public List<SalaryHistoryModel> GetSalaryHistory(int staffId)
        {
            var list = new List<SalaryHistoryModel>();

            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(@"
                SELECT
                    HistoryId, StaffId, OldSalary, NewSalary,
                    ChangedDate, Reason, ChangedBy
                FROM HR.SalaryHistory
                WHERE StaffId = @StaffId
                ORDER BY ChangedDate DESC",
                con);

            cmd.Parameters.AddWithValue("@StaffId", staffId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
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
            }

            return list;
        }

        // ═══════════════════════════════════════════════════════════
        //  PRIVATE MAPPERS
        // ═══════════════════════════════════════════════════════════
        private StaffModel MapStaffList(SqlDataReader reader)
        {
            return new StaffModel
            {
                StaffId = Convert.ToInt32(reader["StaffId"]),
                FirstName = reader["FirstName"].ToString(),
                LastName = reader["LastName"].ToString(),
                Phone = reader["Phone"] == DBNull.Value ? null : reader["Phone"].ToString(),
                Email = reader["Email"] == DBNull.Value ? null : reader["Email"].ToString(),
                RoleId = reader["RoleId"] == DBNull.Value ? 0 : Convert.ToInt32(reader["RoleId"]),
                RoleName = reader["RoleName"] == DBNull.Value ? null : reader["RoleName"].ToString(),
                DOJ = reader["DOJ"] == DBNull.Value ? null : Convert.ToDateTime(reader["DOJ"]),
                IsActive = Convert.ToBoolean(reader["IsActive"]),
                ProfilePhoto = reader["ProfilePhoto"] == DBNull.Value ? null : reader["ProfilePhoto"].ToString(),
                Salary = reader["Salary"] == DBNull.Value ? null : Convert.ToDecimal(reader["Salary"]),
                PlantId = reader["PlantId"] == DBNull.Value ? null : Convert.ToInt32(reader["PlantId"]),
                PlantName = reader["PlantName"] == DBNull.Value ? null : reader["PlantName"].ToString(),
                CenterId = reader["CenterId"] == DBNull.Value ? null : Convert.ToInt32(reader["CenterId"]),
                CenterName = reader["CenterName"] == DBNull.Value ? null : reader["CenterName"].ToString(),
                BankAccountId = reader["BankAccountId"] == DBNull.Value ? null : Convert.ToInt32(reader["BankAccountId"]),
                BankName = reader["BankName"] == DBNull.Value ? null : reader["BankName"].ToString(),
                AccountNumber = reader["AccountNumber"] == DBNull.Value ? null : reader["AccountNumber"].ToString(),
                IFSCCode = reader["IFSCCode"] == DBNull.Value ? null : reader["IFSCCode"].ToString()
            };
        }

        private StaffModel MapStaffFull(SqlDataReader reader)
        {
            return new StaffModel
            {
                StaffId = Convert.ToInt32(reader["StaffId"]),
                FirstName = reader["FirstName"].ToString(),
                LastName = reader["LastName"].ToString(),
                Phone = reader["Phone"] == DBNull.Value ? null : reader["Phone"].ToString(),
                Email = reader["Email"] == DBNull.Value ? null : reader["Email"].ToString(),
                RoleId = reader["RoleId"] == DBNull.Value ? 0 : Convert.ToInt32(reader["RoleId"]),
                RoleName = reader["RoleName"] == DBNull.Value ? null : reader["RoleName"].ToString(),
                DOJ = reader["DOJ"] == DBNull.Value ? null : Convert.ToDateTime(reader["DOJ"]),
                IsActive = Convert.ToBoolean(reader["IsActive"]),
                ProfilePhoto = reader["ProfilePhoto"] == DBNull.Value ? null : reader["ProfilePhoto"].ToString(),
                BankAccountId = reader["BankAccountId"] == DBNull.Value ? null : Convert.ToInt32(reader["BankAccountId"]),
                BankName = reader["BankName"] == DBNull.Value ? null : reader["BankName"].ToString(),
                AccountNumber = reader["AccountNumber"] == DBNull.Value ? null : reader["AccountNumber"].ToString(),
                IFSCCode = reader["IFSCCode"] == DBNull.Value ? null : reader["IFSCCode"].ToString(),
                PlantId = reader["PlantId"] == DBNull.Value ? null : Convert.ToInt32(reader["PlantId"]),
                PlantName = reader["PlantName"] == DBNull.Value ? null : reader["PlantName"].ToString(),
                CenterId = reader["CenterId"] == DBNull.Value ? null : Convert.ToInt32(reader["CenterId"]),
                CenterName = reader["CenterName"] == DBNull.Value ? null : reader["CenterName"].ToString(),
                Salary = reader["Salary"] == DBNull.Value ? null : Convert.ToDecimal(reader["Salary"])
            };
        }

        private StaffPaymentModel MapPayment(SqlDataReader reader)
        {
            return new StaffPaymentModel
            {
                PaymentId = Convert.ToInt32(reader["PaymentId"]),
                StaffId = Convert.ToInt32(reader["StaffId"]),
                PlantId = reader["PlantId"] == DBNull.Value ? 0 : Convert.ToInt32(reader["PlantId"]),
                StaffName = reader["StaffName"].ToString(),
                StaffType = reader["StaffType"].ToString(),
                PlantName = reader["PlantName"].ToString(),
                FromDate = Convert.ToDateTime(reader["FromDate"]),
                ToDate = Convert.ToDateTime(reader["ToDate"]),
                TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                PaymentDate = Convert.ToDateTime(reader["PaymentDate"]),
                PaymentStatus = reader["PaymentStatus"].ToString()
            };
        }
    }
}