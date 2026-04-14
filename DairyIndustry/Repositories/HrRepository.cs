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
        //  2. GET STAFF BY ID — inline query
        // ═══════════════════════════════════════════════════════════
        public StaffModel? GetStaffById(int staffId)
        {
            string query = @"
                SELECT
                    s.StaffId, s.FirstName, s.LastName, s.Phone, s.Email,
                    s.RoleId, r.RoleName,
                    s.DOJ, s.IsActive, s.ProfilePhoto,
                    s.BankAccountId,
                    ba.BankName, ba.AccountNumber, ba.IFSCCode,
                    s.PlantId,  pp.PlantName,
                    s.CenterId, cc.CenterName,
                    s.Salary
                FROM HR.Staffs s
                INNER JOIN Admin.Roles                   r  ON r.RoleId        = s.RoleId
                LEFT  JOIN Finance.BankAccounts          ba ON ba.BankAccountId = s.BankAccountId
                LEFT  JOIN Production.ProcessingPlants   pp ON pp.PlantId       = s.PlantId
                LEFT  JOIN Collection.CollectionCenters  cc ON cc.CenterId      = s.CenterId
                WHERE s.StaffId = @StaffId";

            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@StaffId", staffId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
                return MapStaffFull(reader);

            return null;
        }

        // ═══════════════════════════════════════════════════════════
        //  3. ADD STAFF — inline INSERT
        //     FIX: All nullable string params wrapped with DBNull.Value
        //     FIX: FirstName/LastName also safely passed
        // ═══════════════════════════════════════════════════════════
        public int AddStaff(StaffFormModel model)
        {
            using var con = _db.GetConnection();
            con.Open();
            using var tran = con.BeginTransaction();

            try
            {
                int? bankAccountId = null;

                // Step 1 — create bank account if all 3 fields provided
                if (!string.IsNullOrWhiteSpace(model.BankName) &&
                    !string.IsNullOrWhiteSpace(model.AccountNumber) &&
                    !string.IsNullOrWhiteSpace(model.IFSCCode))
                {
                    string bankQuery = @"
                        INSERT INTO Finance.BankAccounts (BankName, AccountNumber, IFSCCode)
                        VALUES (@BankName, @AccountNumber, @IFSCCode);
                        SELECT SCOPE_IDENTITY();";

                    using var bankCmd = new SqlCommand(bankQuery, con, tran);
                    bankCmd.Parameters.AddWithValue("@BankName", model.BankName.Trim());
                    bankCmd.Parameters.AddWithValue("@AccountNumber", model.AccountNumber.Trim());
                    bankCmd.Parameters.AddWithValue("@IFSCCode", model.IFSCCode.Trim());
                    bankAccountId = Convert.ToInt32(bankCmd.ExecuteScalar());
                }

                // Step 2 — insert staff
                // FIX: FirstName and LastName are Required so won't be null here,
                //      but still pass safely. All nullable fields use DBNull.Value.
                string staffQuery = @"
                    INSERT INTO HR.Staffs
                        (FirstName, LastName, Phone, Email, RoleId,
                         BankAccountId, DOJ, IsActive, ProfilePhoto,
                         PlantId, CenterId, Salary)
                    VALUES
                        (@FirstName, @LastName, @Phone, @Email, @RoleId,
                         @BankAccountId, @DOJ, @IsActive, @ProfilePhoto,
                         @PlantId, @CenterId, @Salary);
                    SELECT SCOPE_IDENTITY();";

                using var staffCmd = new SqlCommand(staffQuery, con, tran);
                staffCmd.Parameters.AddWithValue("@FirstName", (object?)model.FirstName ?? DBNull.Value);
                staffCmd.Parameters.AddWithValue("@LastName", (object?)model.LastName ?? DBNull.Value);
                staffCmd.Parameters.AddWithValue("@Phone", (object?)model.Phone ?? DBNull.Value);
                staffCmd.Parameters.AddWithValue("@Email", (object?)model.Email ?? DBNull.Value);
                staffCmd.Parameters.AddWithValue("@RoleId", model.RoleId);
                staffCmd.Parameters.AddWithValue("@BankAccountId", (object?)bankAccountId ?? DBNull.Value);
                staffCmd.Parameters.AddWithValue("@DOJ", (object?)model.DOJ ?? DBNull.Value);
                staffCmd.Parameters.AddWithValue("@IsActive", model.IsActive);
                staffCmd.Parameters.AddWithValue("@ProfilePhoto", (object?)model.ProfilePhoto ?? DBNull.Value);
                staffCmd.Parameters.AddWithValue("@PlantId", (object?)model.PlantId ?? DBNull.Value);
                staffCmd.Parameters.AddWithValue("@CenterId", (object?)model.CenterId ?? DBNull.Value);
                staffCmd.Parameters.AddWithValue("@Salary", (object?)model.Salary ?? DBNull.Value);

                int newId = Convert.ToInt32(staffCmd.ExecuteScalar());
                tran.Commit();
                return newId;
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  4. UPDATE STAFF — inline UPDATE
        //     FIX: Bank account upsert added — on Edit, update existing
        //          bank record OR insert a new one if not yet linked
        // ═══════════════════════════════════════════════════════════
        public bool UpdateStaff(StaffFormModel model)
        {
            using var con = _db.GetConnection();
            con.Open();
            using var tran = con.BeginTransaction();

            try
            {
                // Step 1: Handle bank account update/insert
                bool hasBankDetails = !string.IsNullOrWhiteSpace(model.BankName)
                                   && !string.IsNullOrWhiteSpace(model.AccountNumber)
                                   && !string.IsNullOrWhiteSpace(model.IFSCCode);

                if (hasBankDetails)
                {
                    // Check if staff already has a bank account linked
                    string checkQuery = "SELECT BankAccountId FROM HR.Staffs WHERE StaffId = @StaffId";
                    using var checkCmd = new SqlCommand(checkQuery, con, tran);
                    checkCmd.Parameters.AddWithValue("@StaffId", model.StaffId);
                    var existingBankId = checkCmd.ExecuteScalar();

                    if (existingBankId != null && existingBankId != DBNull.Value)
                    {
                        // Update existing bank account
                        string updateBank = @"
                            UPDATE Finance.BankAccounts
                            SET BankName = @BankName, AccountNumber = @AccountNumber, IFSCCode = @IFSCCode
                            WHERE BankAccountId = @BankAccountId";
                        using var updBankCmd = new SqlCommand(updateBank, con, tran);
                        updBankCmd.Parameters.AddWithValue("@BankAccountId", Convert.ToInt32(existingBankId));
                        updBankCmd.Parameters.AddWithValue("@BankName", model.BankName!.Trim());
                        updBankCmd.Parameters.AddWithValue("@AccountNumber", model.AccountNumber!.Trim());
                        updBankCmd.Parameters.AddWithValue("@IFSCCode", model.IFSCCode!.Trim());
                        updBankCmd.ExecuteNonQuery();
                    }
                    else
                    {
                        // Insert new bank account and link to staff
                        string insertBank = @"
                            DECLARE @NewBankId INT;
                            INSERT INTO Finance.BankAccounts (BankName, AccountNumber, IFSCCode)
                            VALUES (@BankName, @AccountNumber, @IFSCCode);
                            SET @NewBankId = SCOPE_IDENTITY();
                            UPDATE HR.Staffs SET BankAccountId = @NewBankId WHERE StaffId = @StaffId;";
                        using var insBankCmd = new SqlCommand(insertBank, con, tran);
                        insBankCmd.Parameters.AddWithValue("@BankName", model.BankName!.Trim());
                        insBankCmd.Parameters.AddWithValue("@AccountNumber", model.AccountNumber!.Trim());
                        insBankCmd.Parameters.AddWithValue("@IFSCCode", model.IFSCCode!.Trim());
                        insBankCmd.Parameters.AddWithValue("@StaffId", model.StaffId);
                        insBankCmd.ExecuteNonQuery();
                    }
                }

                // Step 2: Update staff record
                string query = @"
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
                    WHERE StaffId = @StaffId";

                using var cmd = new SqlCommand(query, con, tran);
                cmd.Parameters.AddWithValue("@StaffId", model.StaffId);
                cmd.Parameters.AddWithValue("@FirstName", (object?)model.FirstName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@LastName", (object?)model.LastName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Phone", (object?)model.Phone ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Email", (object?)model.Email ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@RoleId", model.RoleId);
                cmd.Parameters.AddWithValue("@DOJ", (object?)model.DOJ ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IsActive", model.IsActive);
                cmd.Parameters.AddWithValue("@PlantId", (object?)model.PlantId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CenterId", (object?)model.CenterId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Salary", (object?)model.Salary ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ProfilePhoto", (object?)model.ProfilePhoto ?? DBNull.Value);

                bool result = cmd.ExecuteNonQuery() > 0;
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
        //  5. TOGGLE ACTIVE — SP usp_HR_ToggleActive
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
        //  6. UPDATE PROFILE PHOTO — SP
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
        //  7. GET ROLES
        // ═══════════════════════════════════════════════════════════
        public List<RoleModel> GetRoles()
        {
            var list = new List<RoleModel>();
            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand("SELECT RoleId, RoleName FROM Admin.Roles ORDER BY RoleName", con);
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
        //  8. GET PLANTS
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
        //  9. GET CENTERS
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
        //  10. DASHBOARD SUMMARY
        // ═══════════════════════════════════════════════════════════
        public HRDashboardSummaryModel GetDashboardSummary()
        {
            var model = new HRDashboardSummaryModel();

            string staffQuery = @"
                SELECT
                    COUNT(*)                                                        AS TotalStaff,
                    SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END)                  AS ActiveStaff,
                    SUM(CASE WHEN IsActive = 0 THEN 1 ELSE 0 END)                  AS InactiveStaff,
                    SUM(CASE WHEN MONTH(DOJ) = MONTH(GETDATE())
                              AND YEAR(DOJ)  = YEAR(GETDATE())
                              THEN 1 ELSE 0 END)                                   AS NewJoiningThisMonth
                FROM HR.Staffs";

            string payQuery = @"
                SELECT COUNT(*) AS PendingCount, ISNULL(SUM(TotalAmount),0) AS PendingAmount
                FROM Finance.StaffPayments WHERE PaymentStatus = 'Pending'";

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
        //  11. STAFF BY TYPE
        // ═══════════════════════════════════════════════════════════
        public List<StaffTypeCountModel> GetStaffByType()
        {
            var list = new List<StaffTypeCountModel>();

            string query = @"
                SELECT
                    ISNULL(r.RoleName, 'Not Assigned')               AS StaffType,
                    COUNT(*)                                          AS Count,
                    SUM(CASE WHEN s.IsActive = 1 THEN 1 ELSE 0 END)  AS ActiveCount
                FROM HR.Staffs s
                LEFT JOIN Admin.Roles r ON r.RoleId = s.RoleId
                GROUP BY r.RoleName
                ORDER BY Count DESC";

            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(query, con);
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
        //  12. GET PAYMENTS BY STAFF
        // ═══════════════════════════════════════════════════════════
        public List<StaffPaymentModel> GetPaymentsByStaff(int staffId)
        {
            var list = new List<StaffPaymentModel>();

            string query = @"
                SELECT
                    sp.PaymentId, sp.StaffId, sp.PlantId,
                    s.FirstName + ' ' + s.LastName           AS StaffName,
                    ISNULL(r.RoleName, 'Not Assigned')        AS StaffType,
                    pp.PlantName,
                    sp.FromDate, sp.ToDate, sp.TotalAmount,
                    sp.PaymentDate, sp.PaymentStatus
                FROM Finance.StaffPayments sp
                INNER JOIN HR.Staffs                   s  ON s.StaffId  = sp.StaffId
                LEFT  JOIN Admin.Roles                 r  ON r.RoleId   = s.RoleId
                INNER JOIN Production.ProcessingPlants pp ON pp.PlantId = sp.PlantId
                WHERE sp.StaffId = @StaffId
                ORDER BY sp.PaymentDate DESC";

            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@StaffId", staffId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(MapPayment(reader));
            return list;
        }

        // ═══════════════════════════════════════════════════════════
        //  13. GET ALL PAYMENTS
        // ═══════════════════════════════════════════════════════════
        public List<StaffPaymentModel> GetAllPayments(string? status)
        {
            var list = new List<StaffPaymentModel>();

            string query = @"
                SELECT
                    sp.PaymentId, sp.StaffId, sp.PlantId,
                    s.FirstName + ' ' + s.LastName           AS StaffName,
                    ISNULL(r.RoleName, 'Not Assigned')        AS StaffType,
                    pp.PlantName,
                    sp.FromDate, sp.ToDate, sp.TotalAmount,
                    sp.PaymentDate, sp.PaymentStatus
                FROM Finance.StaffPayments sp
                INNER JOIN HR.Staffs                   s  ON s.StaffId  = sp.StaffId
                LEFT  JOIN Admin.Roles                 r  ON r.RoleId   = s.RoleId
                INNER JOIN Production.ProcessingPlants pp ON pp.PlantId = sp.PlantId
                WHERE (@Status IS NULL OR sp.PaymentStatus = @Status)
                ORDER BY sp.PaymentDate DESC";

            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@Status", (object?)status ?? DBNull.Value);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(MapPayment(reader));
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
                BankName = reader["BankName"] == DBNull.Value ? null : reader["BankName"].ToString(),
                AccountNumber = reader["AccountNumber"] == DBNull.Value ? null : reader["AccountNumber"].ToString()
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
                PlantId = Convert.ToInt32(reader["PlantId"]),
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