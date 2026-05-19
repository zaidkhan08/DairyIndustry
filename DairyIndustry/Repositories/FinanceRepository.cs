using DairyIndustry.Data;
using DairyIndustry.Models.Finance;
using DairyIndustry.Models.Admin;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DairyIndustry.Repositories
{
    public class FinanceRepository : IFinanceRepository
    {
        private readonly DbHelper _db;

        public FinanceRepository(DbHelper db)
        {
            _db = db;
        }

        // ════════════════════════════════════════════════════════
        // PREVIEW — calculate totals from unpaid collections
        // ════════════════════════════════════════════════════════

        public FarmerPaymentPreviewModel PreviewFarmerPayment(int farmerId, int centerId,
                                                           DateTime fromDate, DateTime toDate)
        {
            string query = @"
        SELECT
            f.FarmerName,
            cc.CenterName,
            COUNT(mc.CollectionId)       AS UnpaidCollections,
            ISNULL(SUM(mc.Quantity), 0)  AS TotalQty,
            ISNULL(SUM(mc.Amount),   0)  AS TotalAmount
        FROM Collection.MilkCollection mc
        INNER JOIN Farmer.Farmers               f  ON f.FarmerId  = mc.FarmerId
        INNER JOIN Collection.CollectionCenters cc ON cc.CenterId = mc.CenterId
        WHERE mc.FarmerId  = @FarmerId
          AND mc.CenterId  = @CenterId
          AND mc.CollectionDate BETWEEN @FromDate AND @ToDate
          AND mc.CollectionId NOT IN (
              SELECT pd.CollectionId
              FROM   Finance.PaymentDetails pd
              INNER JOIN Finance.FarmerPayments fp ON fp.PaymentId = pd.PaymentId
              WHERE  pd.CollectionId IS NOT NULL
                AND  fp.PaymentStatus NOT IN ('Cancelled')
          )
        GROUP BY f.FarmerName, cc.CenterName";

            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@FarmerId", farmerId);
                    cmd.Parameters.AddWithValue("@CenterId", centerId);
                    cmd.Parameters.AddWithValue("@FromDate", fromDate);
                    cmd.Parameters.AddWithValue("@ToDate", toDate);

                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new FarmerPaymentPreviewModel
                            {
                                FarmerId = farmerId,
                                CenterId = centerId,
                                FarmerName = reader["FarmerName"].ToString(),
                                CenterName = reader["CenterName"].ToString(),
                                FromDate = fromDate,
                                ToDate = toDate,
                                UnpaidCollections = Convert.ToInt32(reader["UnpaidCollections"]),
                                TotalQty = Convert.ToDecimal(reader["TotalQty"]),
                                TotalAmount = Convert.ToDecimal(reader["TotalAmount"])
                            };
                        }
                    }
                }
            }

            return null;
        }

        // ════════════════════════════════════════════════════════
        // CREATE FARMER PAYMENT
        // ════════════════════════════════════════════════════════

        public int CreateFarmerPayment(int centerId, int farmerId,
                                       DateTime fromDate, DateTime toDate, DateTime paymentDate,
                                       int paidByUserId)
        {
            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Finance.usp_Finance_ProcessFarmerPayment", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@CenterId", centerId);
                    cmd.Parameters.AddWithValue("@FarmerId", farmerId);
                    cmd.Parameters.AddWithValue("@FromDate", fromDate);
                    cmd.Parameters.AddWithValue("@ToDate", toDate);
                    cmd.Parameters.AddWithValue("@PaymentDate", paymentDate);
                    cmd.Parameters.AddWithValue("@PaidByUserId", paidByUserId);

                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                            return Convert.ToInt32(reader["NewPaymentId"]);
                    }
                }
            }

            return 0;
        }

        // ════════════════════════════════════════════════════════
        // GET ALL FARMER PAYMENTS
        // ════════════════════════════════════════════════════════

        public List<FarmerPaymentModel> GetAllFarmerPayments(int? centerId = null)
        {
            var list = new List<FarmerPaymentModel>();

            string query = @"
        SELECT
            fp.PaymentId,
            fp.CenterId,
            fp.FarmerId,
            fp.FromDate,
            fp.ToDate,
            fp.TotalQty,
            fp.TotalAmount,
            fp.PaymentDate,
            fp.PaymentStatus,
            fp.PaidByUserId,
            f.FarmerName,
            cc.CenterName,
            pt.BankStatus,
            pt.TransactionReference,
            ba.BankName,
            ba.AccountNumber,
            ba.IFSCCode,
            u.Username AS PaidBy
        FROM Finance.FarmerPayments fp
        INNER JOIN Farmer.Farmers               f  ON f.FarmerId      = fp.FarmerId
        INNER JOIN Collection.CollectionCenters cc ON cc.CenterId     = fp.CenterId
        LEFT  JOIN Finance.PaymentTransactions  pt ON pt.PaymentId    = fp.PaymentId
                                                  AND pt.PaymentType  = 'Farmer'
        LEFT  JOIN Finance.BankAccounts         ba ON ba.BankAccountId = f.BankAccountId
        LEFT  JOIN Admin.Users                  u  ON u.UserId        = fp.PaidByUserId
        WHERE (@CenterId IS NULL OR fp.CenterId = @CenterId)
        ORDER BY fp.PaymentDate DESC";

            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@CenterId", (object?)centerId ?? DBNull.Value);
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            list.Add(MapFarmerPayment(reader));
                    }
                }
            }
            return list;
        }

        // ════════════════════════════════════════════════════════
        // GET SINGLE FARMER PAYMENT BY ID
        // ════════════════════════════════════════════════════════

        public FarmerPaymentModel GetFarmerPaymentById(int paymentId)
        {
            FarmerPaymentModel payment = null;

            string query = @"
                SELECT
                    fp.PaymentId,
                    fp.CenterId,
                    fp.FarmerId,
                    fp.FromDate,
                    fp.ToDate,
                    fp.TotalQty,
                    fp.TotalAmount,
                    fp.PaymentDate,
                    fp.PaymentStatus,
                    fp.PaidByUserId,
                    f.FarmerName,
                    cc.CenterName,
                    pt.BankStatus,
                    pt.TransactionReference,
                    ba.BankName,
                    ba.AccountNumber,
                    ba.IFSCCode,
                    u.Username AS PaidBy
                FROM Finance.FarmerPayments fp
                INNER JOIN Farmer.Farmers               f  ON f.FarmerId      = fp.FarmerId
                INNER JOIN Collection.CollectionCenters cc ON cc.CenterId     = fp.CenterId
                LEFT  JOIN Finance.PaymentTransactions  pt ON pt.PaymentId    = fp.PaymentId
                                                          AND pt.PaymentType  = 'Farmer'
                LEFT  JOIN Finance.BankAccounts         ba ON ba.BankAccountId = f.BankAccountId
                LEFT  JOIN Admin.Users                  u  ON u.UserId        = fp.PaidByUserId
                WHERE fp.PaymentId = @PaymentId";

            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@PaymentId", paymentId);
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                            payment = MapFarmerPayment(reader);
                    }
                }
            }

            return payment;
        }

        // ════════════════════════════════════════════════════════
        // RECORD STRIPE TRANSACTION
        // ════════════════════════════════════════════════════════

        public void RecordPaymentTransaction(string paymentType, int paymentId,
                                             string bankStatus, string transactionReference)
        {
            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Finance.usp_Finance_RecordPaymentTransaction", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@PaymentType", paymentType);
                    cmd.Parameters.AddWithValue("@PaymentId", paymentId);
                    cmd.Parameters.AddWithValue("@BankStatus", bankStatus);
                    cmd.Parameters.AddWithValue("@TransactionReference", (object?)transactionReference ?? DBNull.Value);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ════════════════════════════════════════════════════════
        // FARMER HAS BANK ACCOUNT
        // ════════════════════════════════════════════════════════

        public bool FarmerHasBankAccount(int farmerId)
        {
            string query = @"
                SELECT COUNT(*)
                FROM Farmer.Farmers f
                INNER JOIN Finance.BankAccounts ba ON ba.BankAccountId = f.BankAccountId
                WHERE f.FarmerId = @FarmerId
                  AND f.BankAccountId IS NOT NULL";

            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@FarmerId", farmerId);
                    con.Open();
                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
        }

        // ════════════════════════════════════════════════════════
        // DROPDOWNS
        // ════════════════════════════════════════════════════════

        public List<FarmerDropdownModel> GetAllFarmers()
        {
            var list = new List<FarmerDropdownModel>();
            string query = "SELECT FarmerId, FarmerName FROM Farmer.Farmers WHERE IsActive = 1 ORDER BY FarmerName";
            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            list.Add(new FarmerDropdownModel
                            {
                                FarmerId = Convert.ToInt32(reader["FarmerId"]),
                                FarmerName = reader["FarmerName"].ToString()
                            });
                    }
                }
            }
            return list;
        }

        public List<CenterDropdownModel> GetAllCenters()
        {
            var list = new List<CenterDropdownModel>();
            string query = "SELECT CenterId, CenterName FROM Collection.CollectionCenters ORDER BY CenterName";
            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            list.Add(new CenterDropdownModel
                            {
                                CenterId = Convert.ToInt32(reader["CenterId"]),
                                CenterName = reader["CenterName"].ToString()
                            });
                    }
                }
            }
            return list;
        }

        // ════════════════════════════════════════════════════════
        // GET FARMERS BY CENTER
        // ════════════════════════════════════════════════════════

        public List<FarmerDropdownModel> GetFarmersByCenter(int centerId)
        {
            var list = new List<FarmerDropdownModel>();
            string query = @"
                SELECT DISTINCT f.FarmerId, f.FarmerName, f.FarmerCode
                FROM Farmer.Farmers f
                WHERE f.IsActive = 1
                  AND f.FarmerId IN (
                      SELECT DISTINCT mc.FarmerId
                      FROM Collection.MilkCollection mc
                      WHERE mc.CenterId = @CenterId
                  )
                ORDER BY f.FarmerName";

            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@CenterId", centerId);
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            list.Add(new FarmerDropdownModel
                            {
                                FarmerId = Convert.ToInt32(reader["FarmerId"]),
                                FarmerName = reader["FarmerName"].ToString(),
                                FarmerCode = reader["FarmerCode"] == DBNull.Value
                                             ? null
                                             : reader["FarmerCode"].ToString()
                            });
                    }
                }
            }
            return list;
        }

        // ════════════════════════════════════════════════════════
        // GET FARMER BY CODE
        // ════════════════════════════════════════════════════════

        public FarmerDropdownModel GetFarmerByCode(string farmerCode)
        {
            string query = @"
                SELECT FarmerId, FarmerName, FarmerCode
                FROM Farmer.Farmers
                WHERE IsActive = 1
                  AND FarmerCode = @FarmerCode";

            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@FarmerCode", farmerCode);
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                            return new FarmerDropdownModel
                            {
                                FarmerId = Convert.ToInt32(reader["FarmerId"]),
                                FarmerName = reader["FarmerName"].ToString(),
                                FarmerCode = reader["FarmerCode"].ToString()
                            };
                    }
                }
            }
            return null;
        }

        public CenterDropdownModel GetFarmerDefaultCenter(int farmerId)
        {
            string query = @"
        SELECT TOP 1 cc.CenterId, cc.CenterName
        FROM Collection.MilkCollection mc
        INNER JOIN Collection.CollectionCenters cc ON cc.CenterId = mc.CenterId
        WHERE mc.FarmerId = @FarmerId
        ORDER BY mc.CollectionDate DESC";

            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@FarmerId", farmerId);
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                            return new CenterDropdownModel
                            {
                                CenterId = Convert.ToInt32(reader["CenterId"]),
                                CenterName = reader["CenterName"].ToString()
                            };
                    }
                }
            }

            return null;
        }

        // ════════════════════════════════════════════════════════
        // GET ELIGIBLE TRANSFERS — scoped by PlantId for Plant Manager
        // ════════════════════════════════════════════════════════
        public List<TransferForPaymentModel> GetEligibleTransfers(int? plantId = null)
        {
            var list = new List<TransferForPaymentModel>();

            string query = @"
                SELECT
                    mt.TransferId,
                    mt.BatchId,
                    cb.CenterId,
                    mt.PlantId,
                    mt.ReceivedQty,
                    COALESCE(tqt.TestedFat, cb.AvgFat)  AS TestedFat,
                    COALESCE(tqt.TestedCLR, cb.AvgCLR)  AS TestedCLR,
                    cmi.MilkTypeId,
                    CONCAT('T-', mt.TransferId,
                           ' | ', cc.CenterName,
                           ' | ', mt.ReceivedQty, ' L',
                           ' | ', FORMAT(mt.ReceivedDate,'dd-MMM-yyyy')) AS DisplayText,
                    cp_cancelled.CenterPaymentId AS CancelledPaymentId,
                    CASE WHEN cp_cancelled.CenterPaymentId IS NOT NULL THEN 1 ELSE 0 END AS HasCancelledPayment
                FROM  Production.MilkTransfers             mt
                INNER JOIN Collection.CollectionBatches    cb  ON cb.BatchId     = mt.BatchId
                INNER JOIN Collection.CollectionCenters    cc  ON cc.CenterId    = cb.CenterId
                LEFT  JOIN Production.TransferQualityTests tqt ON tqt.TransferId = mt.TransferId
                LEFT  JOIN Collection.CenterMilkInventory  cmi ON cmi.CenterId  = cb.CenterId
                LEFT  JOIN (
                    SELECT BatchId, PlantId, MAX(CenterPaymentId) AS CenterPaymentId
                    FROM   Finance.CenterPayments
                    WHERE  PaymentStatus = 'Cancelled'
                    GROUP  BY BatchId, PlantId
                ) cp_cancelled ON cp_cancelled.BatchId = mt.BatchId
                             AND  cp_cancelled.PlantId = mt.PlantId
                WHERE mt.ReceivedQty IS NOT NULL
                  AND (@PlantId IS NULL OR mt.PlantId = @PlantId)
                  AND NOT EXISTS (
                        SELECT 1 FROM Finance.CenterPayments cp
                        WHERE  cp.BatchId       = mt.BatchId
                          AND  cp.PlantId       = mt.PlantId
                          AND  cp.PaymentStatus IN ('Pending','Processed','Failed')
                  )
                ORDER BY mt.ReceivedDate DESC";

            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("@PlantId", (object?)plantId ?? DBNull.Value);
                con.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new TransferForPaymentModel
                        {
                            TransferId = Convert.ToInt32(r["TransferId"]),
                            DisplayText = r["DisplayText"].ToString(),
                            CenterId = Convert.ToInt32(r["CenterId"]),
                            PlantId = Convert.ToInt32(r["PlantId"]),
                            ReceivedQty = Convert.ToDecimal(r["ReceivedQty"]),
                            TestedFat = r["TestedFat"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(r["TestedFat"]),
                            TestedCLR = r["TestedCLR"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(r["TestedCLR"]),
                            MilkTypeId = r["MilkTypeId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["MilkTypeId"]),
                            BatchId = Convert.ToInt32(r["BatchId"]),
                            HasCancelledPayment = Convert.ToInt32(r["HasCancelledPayment"]) == 1,
                            CancelledPaymentId = r["CancelledPaymentId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["CancelledPaymentId"])
                        });
                    }
                }
            }

            return list;
        }

        // ════════════════════════════════════════════════════════
        // GET ACTIVE RATE
        // ════════════════════════════════════════════════════════
        public decimal GetActiveRate(int milkTypeId, decimal fat, decimal clr)
        {
            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand("Finance.usp_Finance_GetActiveRate", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@MilkTypeId", milkTypeId);
                cmd.Parameters.AddWithValue("@Fat", fat);
                cmd.Parameters.AddWithValue("@CLR", clr);

                con.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (r.Read())
                        return Convert.ToDecimal(r["RatePerLiter"]);
                }
            }
            return 0m;
        }

        // ════════════════════════════════════════════════════════
        // CREATE CENTER PAYMENT
        // ════════════════════════════════════════════════════════
        public int CreateCenterPayment(int transferId, decimal ratePerLiter, DateTime paymentDate)
        {
            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand("Finance.usp_Finance_ProcessCenterPayment", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@TransferId", transferId);
                cmd.Parameters.AddWithValue("@RatePerLiter", ratePerLiter);
                cmd.Parameters.AddWithValue("@PaymentDate", paymentDate.Date);

                con.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (r.Read())
                        return Convert.ToInt32(r["NewCenterPaymentId"]);
                }
            }

            return 0;
        }

        // ════════════════════════════════════════════════════════
        // GET ALL CENTER PAYMENTS — scoped by PlantId for Plant Manager
        // ════════════════════════════════════════════════════════
        public List<CenterPaymentModel> GetAllCenterPayments(int? plantId = null)
        {
            var list = new List<CenterPaymentModel>();

            string query = @"
        SELECT
            cp.CenterPaymentId,
            cp.BatchId,
            cp.CenterId,
            cp.PlantId,
            cp.ReceivedQty,
            cp.RatePerLiter,
            cp.TestedFat,
            cp.TestedCLR,
            cp.TotalAmount,
            cp.PaymentDate,
            cp.PaymentStatus,
            cc.CenterName,
            pp.PlantName,
            CONCAT('T-', mt.TransferId, ' | ', FORMAT(mt.ReceivedDate,'dd-MMM-yyyy')) AS BatchRef,
            pt.BankStatus,
            pt.TransactionReference
        FROM Finance.CenterPayments cp
        INNER JOIN Collection.CollectionBatches   cb ON cb.BatchId  = cp.BatchId
        INNER JOIN Collection.CollectionCenters   cc ON cc.CenterId = cp.CenterId
        INNER JOIN Production.ProcessingPlants    pp ON pp.PlantId  = cp.PlantId
        LEFT  JOIN Production.MilkTransfers       mt ON mt.BatchId  = cp.BatchId
                                                    AND mt.PlantId  = cp.PlantId
        LEFT  JOIN Finance.PaymentTransactions    pt ON pt.PaymentId   = cp.CenterPaymentId
                                                    AND pt.PaymentType = 'Center'
        WHERE (@PlantId IS NULL OR cp.PlantId = @PlantId)
        ORDER BY cp.PaymentDate DESC";

            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("@PlantId", (object?)plantId ?? DBNull.Value);
                con.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                        list.Add(MapCenterPayment(r));
                }
            }

            return list;
        }

        // ════════════════════════════════════════════════════════
        // GET SINGLE CENTER PAYMENT BY ID
        // ════════════════════════════════════════════════════════
        public CenterPaymentModel GetCenterPaymentById(int centerPaymentId)
        {
            string query = @"
        SELECT
            cp.CenterPaymentId,
            cp.BatchId,
            cp.CenterId,
            cp.PlantId,
            cp.ReceivedQty,
            cp.RatePerLiter,
            cp.TestedFat,
            cp.TestedCLR,
            cp.TotalAmount,
            cp.PaymentDate,
            cp.PaymentStatus,
            cc.CenterName,
            pp.PlantName,
            CONCAT('T-', mt.TransferId, ' | ', FORMAT(mt.ReceivedDate,'dd-MMM-yyyy')) AS BatchRef,
            pt.BankStatus,
            pt.TransactionReference
        FROM Finance.CenterPayments cp
        INNER JOIN Collection.CollectionBatches   cb ON cb.BatchId  = cp.BatchId
        INNER JOIN Collection.CollectionCenters   cc ON cc.CenterId = cp.CenterId
        INNER JOIN Production.ProcessingPlants    pp ON pp.PlantId  = cp.PlantId
        LEFT  JOIN Production.MilkTransfers       mt ON mt.BatchId  = cp.BatchId
                                                    AND mt.PlantId  = cp.PlantId
        LEFT  JOIN Finance.PaymentTransactions    pt ON pt.PaymentId   = cp.CenterPaymentId
                                                    AND pt.PaymentType = 'Center'
        WHERE cp.CenterPaymentId = @CenterPaymentId";

            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("@CenterPaymentId", centerPaymentId);
                con.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (r.Read()) return MapCenterPayment(r);
                }
            }

            return null;
        }

        // ════════════════════════════════════════════════════════
        // PRIVATE MAPPERS
        // ════════════════════════════════════════════════════════

        private FarmerPaymentModel MapFarmerPayment(SqlDataReader reader)
        {
            return new FarmerPaymentModel
            {
                PaymentId = Convert.ToInt32(reader["PaymentId"]),
                CenterId = Convert.ToInt32(reader["CenterId"]),
                FarmerId = Convert.ToInt32(reader["FarmerId"]),
                FromDate = Convert.ToDateTime(reader["FromDate"]),
                ToDate = Convert.ToDateTime(reader["ToDate"]),
                TotalQty = Convert.ToDecimal(reader["TotalQty"]),
                TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                PaymentDate = Convert.ToDateTime(reader["PaymentDate"]),
                PaymentStatus = reader["PaymentStatus"].ToString(),
                FarmerName = reader["FarmerName"].ToString(),
                CenterName = reader["CenterName"].ToString(),
                BankStatus = reader["BankStatus"] == DBNull.Value ? null : reader["BankStatus"].ToString(),
                TransactionReference = reader["TransactionReference"] == DBNull.Value ? null : reader["TransactionReference"].ToString(),
                BankName = reader["BankName"] == DBNull.Value ? null : reader["BankName"].ToString(),
                AccountNumber = reader["AccountNumber"] == DBNull.Value ? null : reader["AccountNumber"].ToString(),
                IFSCCode = reader["IFSCCode"] == DBNull.Value ? null : reader["IFSCCode"].ToString(),
                PaidByUserId = reader["PaidByUserId"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["PaidByUserId"]),
                PaidBy = reader["PaidBy"] == DBNull.Value ? null : reader["PaidBy"].ToString()
            };
        }

        public void CancelFarmerPayment(int paymentId)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Finance.FarmerPayments SET PaymentStatus = 'Cancelled' WHERE PaymentId = @id";
            cmd.Parameters.AddWithValue("@id", paymentId);
            cmd.ExecuteNonQuery();
        }

        public void ReactivateFarmerPayment(int paymentId)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Finance.FarmerPayments SET PaymentStatus = 'Pending' WHERE PaymentId = @id";
            cmd.Parameters.AddWithValue("@id", paymentId);
            cmd.ExecuteNonQuery();
        }

        private CenterPaymentModel MapCenterPayment(SqlDataReader r)
        {
            return new CenterPaymentModel
            {
                CenterPaymentId = Convert.ToInt32(r["CenterPaymentId"]),
                BatchId = Convert.ToInt32(r["BatchId"]),
                CenterId = Convert.ToInt32(r["CenterId"]),
                PlantId = Convert.ToInt32(r["PlantId"]),
                ReceivedQty = Convert.ToDecimal(r["ReceivedQty"]),
                RatePerLiter = Convert.ToDecimal(r["RatePerLiter"]),
                TestedFat = r["TestedFat"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(r["TestedFat"]),
                TestedCLR = r["TestedCLR"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(r["TestedCLR"]),
                TotalAmount = Convert.ToDecimal(r["TotalAmount"]),
                PaymentDate = Convert.ToDateTime(r["PaymentDate"]),
                PaymentStatus = r["PaymentStatus"].ToString(),
                CenterName = r["CenterName"].ToString(),
                PlantName = r["PlantName"].ToString(),
                BatchRef = r["BatchRef"].ToString(),
                BankStatus = r["BankStatus"] == DBNull.Value ? null : r["BankStatus"].ToString(),
                TransactionReference = r["TransactionReference"] == DBNull.Value ? null : r["TransactionReference"].ToString()
            };
        }

        // ════════════════════════════════════════════════════════
        // REACTIVATE CANCELLED CENTER PAYMENT
        // ════════════════════════════════════════════════════════
        public int ReactivateCenterPayment(int centerPaymentId, decimal ratePerLiter, DateTime paymentDate)
        {
            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand("Finance.usp_Finance_ReactivateCenterPayment", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@CenterPaymentId", centerPaymentId);
                cmd.Parameters.AddWithValue("@RatePerLiter", ratePerLiter);
                cmd.Parameters.AddWithValue("@PaymentDate", paymentDate.Date);
                con.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (r.Read())
                        return Convert.ToInt32(r["CenterPaymentId"]);
                }
            }
            return centerPaymentId;
        }

        public void CancelCenterPayment(int centerPaymentId)
        {
            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand("Finance.usp_Finance_CancelCenterPayment", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@CenterPaymentId", centerPaymentId);
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // ════════════════════════════════════════════════════════
        // GET CENTER WALLET
        // ════════════════════════════════════════════════════════

        public CenterWalletViewModel GetCenterWallet(int? centerId = null)
        {
            var vm = new CenterWalletViewModel();

            string summaryQuery = @"
        SELECT
            cc.CenterId,
            cc.CenterName,
            ISNULL(SUM(CASE WHEN cp.PaymentStatus = 'Processed' THEN cp.TotalAmount ELSE 0 END), 0) AS TotalReceived,
            ISNULL(SUM(CASE WHEN cp.PaymentStatus = 'Pending'   THEN cp.TotalAmount ELSE 0 END), 0) AS TotalPending,
            COUNT(cp.CenterPaymentId) AS TotalTxns,
            ISNULL((
                SELECT SUM(sp.TotalAmount)
                FROM Finance.StaffPayments sp
                INNER JOIN HR.Staffs s ON s.StaffId = sp.StaffId
                WHERE s.CenterId = cc.CenterId
            ), 0) AS TotalStaffCost,
            ISNULL((
                SELECT COUNT(*)
                FROM HR.Staffs s
                WHERE s.CenterId = cc.CenterId AND s.IsActive = 1
            ), 0) AS TotalStaffCount,
            ISNULL((
                SELECT SUM(cw.BonusAmount)
                FROM Finance.CenterWallet cw
                WHERE cw.CenterId = cc.CenterId
            ), 0) AS TotalBonusEarned,
            ISNULL((
                SELECT SUM(cw.BaseAmount)
                FROM Finance.CenterWallet cw
                WHERE cw.CenterId = cc.CenterId
            ), 0) AS TotalBaseEarned
        FROM Collection.CollectionCenters cc
        LEFT JOIN Finance.CenterPayments cp ON cp.CenterId = cc.CenterId
        WHERE (@CenterId IS NULL OR cc.CenterId = @CenterId)
        GROUP BY cc.CenterId, cc.CenterName";

            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(summaryQuery, con))
            {
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("@CenterId", (object?)centerId ?? DBNull.Value);
                con.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    decimal totalReceived = 0, totalPending = 0, totalStaffCost = 0;
                    decimal totalBonusEarned = 0, totalBaseEarned = 0;
                    int totalTxns = 0, totalStaffCount = 0;
                    string centerName = "All Centers";
                    int cId = 0;

                    while (r.Read())
                    {
                        totalReceived += Convert.ToDecimal(r["TotalReceived"]);
                        totalPending += Convert.ToDecimal(r["TotalPending"]);
                        totalStaffCost += Convert.ToDecimal(r["TotalStaffCost"]);
                        totalTxns += Convert.ToInt32(r["TotalTxns"]);
                        totalStaffCount += Convert.ToInt32(r["TotalStaffCount"]);
                        totalBonusEarned += Convert.ToDecimal(r["TotalBonusEarned"]);
                        totalBaseEarned += Convert.ToDecimal(r["TotalBaseEarned"]);

                        if (centerId.HasValue)
                        {
                            cId = Convert.ToInt32(r["CenterId"]);
                            centerName = r["CenterName"].ToString();
                        }
                    }

                    vm.Summary = new CenterWalletSummary
                    {
                        CenterId = cId,
                        CenterName = centerName,
                        TotalReceived = totalReceived,
                        TotalPending = totalPending,
                        TotalStaffCost = totalStaffCost,
                        TotalTxns = totalTxns,
                        TotalStaffCount = totalStaffCount,
                        TotalBonusEarned = totalBonusEarned,
                        TotalBaseEarned = totalBaseEarned
                    };
                }
            }

            string txnQuery = @"
        SELECT
            cp.CenterPaymentId,
            cc.CenterName,
            pp.PlantName,
            CONCAT('T-', mt.TransferId, ' | ', FORMAT(mt.ReceivedDate,'dd-MMM-yyyy')) AS BatchRef,
            cp.ReceivedQty,
            cp.RatePerLiter,
            cp.TotalAmount,
            cp.PaymentDate,
            cp.PaymentStatus,
            pt.BankStatus,
            pt.TransactionReference
        FROM Finance.CenterPayments cp
        INNER JOIN Collection.CollectionCenters  cc ON cc.CenterId = cp.CenterId
        INNER JOIN Production.ProcessingPlants   pp ON pp.PlantId  = cp.PlantId
        LEFT  JOIN Production.MilkTransfers      mt ON mt.BatchId  = cp.BatchId
                                                   AND mt.PlantId  = cp.PlantId
        LEFT  JOIN Finance.PaymentTransactions   pt ON pt.PaymentId   = cp.CenterPaymentId
                                                   AND pt.PaymentType = 'Center'
        WHERE (@CenterId IS NULL OR cp.CenterId = @CenterId)
        ORDER BY cp.PaymentDate DESC";

            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(txnQuery, con))
            {
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("@CenterId", (object?)centerId ?? DBNull.Value);
                con.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        vm.Transactions.Add(new CenterWalletTransaction
                        {
                            CenterPaymentId = Convert.ToInt32(r["CenterPaymentId"]),
                            CenterName = r["CenterName"].ToString(),
                            PlantName = r["PlantName"].ToString(),
                            BatchRef = r["BatchRef"].ToString(),
                            ReceivedQty = Convert.ToDecimal(r["ReceivedQty"]),
                            RatePerLiter = Convert.ToDecimal(r["RatePerLiter"]),
                            TotalAmount = Convert.ToDecimal(r["TotalAmount"]),
                            PaymentDate = Convert.ToDateTime(r["PaymentDate"]),
                            PaymentStatus = r["PaymentStatus"].ToString(),
                            BankStatus = r["BankStatus"] == DBNull.Value ? null : r["BankStatus"].ToString(),
                            TransactionReference = r["TransactionReference"] == DBNull.Value ? null : r["TransactionReference"].ToString()
                        });
                    }
                }
            }

            string staffQuery = @"
        SELECT
            sp.PaymentId,
            s.FirstName + ' ' + s.LastName  AS StaffName,
            ISNULL(r.RoleName, '')           AS RoleName,
            ISNULL(s.Salary, 0)             AS MonthlySalary,
            sp.FromDate,
            sp.ToDate,
            sp.TotalAmount,
            sp.PaymentDate,
            sp.PaymentStatus
        FROM Finance.StaffPayments sp
        INNER JOIN HR.Staffs  s  ON s.StaffId  = sp.StaffId
        LEFT  JOIN Admin.Roles r ON r.RoleId    = s.RoleId
        WHERE (@CenterId IS NULL OR s.CenterId = @CenterId)
        ORDER BY sp.PaymentDate DESC";

            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(staffQuery, con))
            {
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("@CenterId", (object?)centerId ?? DBNull.Value);
                con.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        vm.StaffPayments.Add(new CenterStaffPayment
                        {
                            PaymentId = Convert.ToInt32(r["PaymentId"]),
                            StaffName = r["StaffName"].ToString(),
                            RoleName = r["RoleName"].ToString(),
                            MonthlySalary = Convert.ToDecimal(r["MonthlySalary"]),
                            FromDate = Convert.ToDateTime(r["FromDate"]),
                            ToDate = Convert.ToDateTime(r["ToDate"]),
                            TotalAmount = Convert.ToDecimal(r["TotalAmount"]),
                            PaymentDate = Convert.ToDateTime(r["PaymentDate"]),
                            PaymentStatus = r["PaymentStatus"].ToString()
                        });
                    }
                }
            }

            vm.WalletEntries = GetCenterWalletEntries(centerId);

            return vm;
        }

        public List<CenterWalletEntry> GetCenterWalletEntries(int? centerId = null)
        {
            var list = new List<CenterWalletEntry>();

            string query = @"
        SELECT
            cw.WalletId,
            cw.CenterPaymentId,
            cc.CenterName,
            pp.PlantName,
            CONCAT('T-', mt.TransferId, ' | ', FORMAT(mt.ReceivedDate,'dd-MMM-yyyy')) AS BatchRef,
            cw.ReceivedQty,
            cw.BonusRatePerLiter,
            cw.BaseRatePerLiter,
            cw.FullRatePerLiter,
            cw.BaseAmount,
            cw.BonusAmount,
            cw.TotalEarned,
            cw.PaymentDate,
            cp.PaymentStatus,
            cw.CreatedAt
        FROM Finance.CenterWallet cw
        INNER JOIN Finance.CenterPayments          cp ON cp.CenterPaymentId = cw.CenterPaymentId
        INNER JOIN Collection.CollectionCenters    cc ON cc.CenterId        = cw.CenterId
        INNER JOIN Production.ProcessingPlants     pp ON pp.PlantId         = cw.PlantId
        LEFT  JOIN Production.MilkTransfers        mt ON mt.BatchId         = cp.BatchId
                                                     AND mt.PlantId         = cp.PlantId
        WHERE (@CenterId IS NULL OR cw.CenterId = @CenterId)
        ORDER BY cw.PaymentDate DESC, cw.WalletId DESC";

            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("@CenterId", (object?)centerId ?? DBNull.Value);
                con.Open();

                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new CenterWalletEntry
                        {
                            WalletId = Convert.ToInt32(r["WalletId"]),
                            CenterPaymentId = Convert.ToInt32(r["CenterPaymentId"]),
                            CenterName = r["CenterName"].ToString(),
                            PlantName = r["PlantName"].ToString(),
                            BatchRef = r["BatchRef"].ToString(),
                            ReceivedQty = Convert.ToDecimal(r["ReceivedQty"]),
                            BonusRatePerLiter = Convert.ToDecimal(r["BonusRatePerLiter"]),
                            BaseRatePerLiter = Convert.ToDecimal(r["BaseRatePerLiter"]),
                            FullRatePerLiter = Convert.ToDecimal(r["FullRatePerLiter"]),
                            BaseAmount = Convert.ToDecimal(r["BaseAmount"]),
                            BonusAmount = Convert.ToDecimal(r["BonusAmount"]),
                            TotalEarned = Convert.ToDecimal(r["TotalEarned"]),
                            PaymentDate = Convert.ToDateTime(r["PaymentDate"]),
                            PaymentStatus = r["PaymentStatus"].ToString(),
                            CreatedAt = Convert.ToDateTime(r["CreatedAt"])
                        });
                    }
                }
            }

            return list;
        }

        // ════════════════════════════════════════════════════════
        // GET PLANT STAFF
        // Returns staff whose center has supplied milk to the given plant.
        // Plant Manager passes their PlantId; Admin passes null (sees all).
        //
        // Logic:
        //   A staff member "belongs to" a plant when the center they work at
        //   (HR.Staffs.CenterId) has sent at least one milk transfer to that
        //   plant (Production.MilkTransfers joined via Collection.CollectionBatches).
        // ════════════════════════════════════════════════════════
        public PlantStaffViewModel GetPlantStaff(int? plantId = null)
        {
            var vm = new PlantStaffViewModel();

            // ════════════════════════════════════════════════════════
            // SUMMARY
            // ════════════════════════════════════════════════════════

            string summaryQuery = @"
        SELECT
            pp.PlantId,
            pp.PlantName,

            COUNT(DISTINCT s.StaffId) AS TotalStaff,

            COUNT(DISTINCT CASE
                WHEN s.IsActive = 1 THEN s.StaffId
            END) AS ActiveStaff,

            COUNT(DISTINCT CASE
                WHEN s.IsActive = 0 THEN s.StaffId
            END) AS InactiveStaff,

            ISNULL(SUM(CASE
                WHEN s.IsActive = 1
                THEN ISNULL(s.Salary, 0)
                ELSE 0
            END), 0) AS TotalMonthlySalary,

            COUNT(DISTINCT s.CenterId) AS TotalCenters

        FROM Production.ProcessingPlants pp

        LEFT JOIN HR.Staffs s
            ON s.PlantId = pp.PlantId

        WHERE (@PlantId IS NULL OR pp.PlantId = @PlantId)

        GROUP BY
            pp.PlantId,
            pp.PlantName

        ORDER BY
            pp.PlantName";

            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(summaryQuery, con))
            {
                cmd.Parameters.AddWithValue("@PlantId",
                    (object?)plantId ?? DBNull.Value);

                con.Open();

                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    int totalStaff = 0;
                    int activeStaff = 0;
                    int inactiveStaff = 0;
                    decimal totalSalary = 0;
                    int totalCenters = 0;

                    string plantName = "All Plants";
                    int pId = 0;

                    while (r.Read())
                    {
                        totalStaff += Convert.ToInt32(r["TotalStaff"]);
                        activeStaff += Convert.ToInt32(r["ActiveStaff"]);
                        inactiveStaff += Convert.ToInt32(r["InactiveStaff"]);
                        totalSalary += Convert.ToDecimal(r["TotalMonthlySalary"]);
                        totalCenters += Convert.ToInt32(r["TotalCenters"]);

                        if (plantId.HasValue)
                        {
                            pId = Convert.ToInt32(r["PlantId"]);
                            plantName = r["PlantName"].ToString();
                        }
                    }

                    vm.Summary = new PlantStaffSummary
                    {
                        PlantId = pId,
                        PlantName = plantName,
                        TotalStaff = totalStaff,
                        ActiveStaff = activeStaff,
                        InactiveStaff = inactiveStaff,
                        TotalMonthlySalary = totalSalary,
                        TotalCenters = totalCenters
                    };
                }
            }

            // ════════════════════════════════════════════════════════
            // STAFF LIST
            // ════════════════════════════════════════════════════════

            string staffQuery = @"
        SELECT
            s.StaffId,

            s.FirstName + ' ' + s.LastName AS FullName,

            ISNULL(ar.RoleName, '') AS RoleName,

            s.Email,
            s.Phone,
            s.Salary,
            s.IsActive,

            s.DOJ AS JoiningDate,

            s.CenterId,
            cc.CenterName,

            s.PlantId,
            pp.PlantName,

            (
                SELECT TOP 1 sp.PaymentDate
                FROM Finance.StaffPayments sp
                WHERE sp.StaffId = s.StaffId
                ORDER BY sp.PaymentDate DESC
            ) AS LastPaymentDate,

            (
                SELECT TOP 1 sp.PaymentStatus
                FROM Finance.StaffPayments sp
                WHERE sp.StaffId = s.StaffId
                ORDER BY sp.PaymentDate DESC
            ) AS LastPaymentStatus

        FROM HR.Staffs s

        LEFT JOIN Collection.CollectionCenters cc
            ON cc.CenterId = s.CenterId

        LEFT JOIN Production.ProcessingPlants pp
            ON pp.PlantId = s.PlantId

        LEFT JOIN Admin.Roles ar
            ON ar.RoleId = s.RoleId

        WHERE (@PlantId IS NULL OR s.PlantId = @PlantId)

        ORDER BY
            pp.PlantName,
            cc.CenterName,
            FullName";

            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(staffQuery, con))
            {
                cmd.Parameters.AddWithValue("@PlantId",
                    (object?)plantId ?? DBNull.Value);

                con.Open();

                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        vm.Staff.Add(new PlantStaffModel
                        {
                            StaffId = Convert.ToInt32(r["StaffId"]),

                            FullName = r["FullName"].ToString(),

                            RoleName = r["RoleName"].ToString(),

                            Email = r["Email"] == DBNull.Value
                                ? null
                                : r["Email"].ToString(),

                            Phone = r["Phone"] == DBNull.Value
                                ? null
                                : r["Phone"].ToString(),

                            Salary = r["Salary"] == DBNull.Value
                                ? (decimal?)null
                                : Convert.ToDecimal(r["Salary"]),

                            IsActive = Convert.ToBoolean(r["IsActive"]),

                            JoiningDate = r["JoiningDate"] == DBNull.Value
                                ? (DateTime?)null
                                : Convert.ToDateTime(r["JoiningDate"]),

                            CenterId = r["CenterId"] == DBNull.Value
                                ? 0
                                : Convert.ToInt32(r["CenterId"]),

                            CenterName = r["CenterName"] == DBNull.Value
                                ? "-"
                                : r["CenterName"].ToString(),

                            PlantId = r["PlantId"] == DBNull.Value
                                ? 0
                                : Convert.ToInt32(r["PlantId"]),

                            PlantName = r["PlantName"] == DBNull.Value
                                ? "-"
                                : r["PlantName"].ToString(),

                            LastPaymentDate = r["LastPaymentDate"] == DBNull.Value
                                ? (DateTime?)null
                                : Convert.ToDateTime(r["LastPaymentDate"]),

                            LastPaymentStatus = r["LastPaymentStatus"] == DBNull.Value
                                ? null
                                : r["LastPaymentStatus"].ToString()
                        });
                    }
                }
            }

            return vm;
        }
    }
}