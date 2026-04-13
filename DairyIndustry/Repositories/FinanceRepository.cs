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
                    COUNT(mc.CollectionId)  AS UnpaidCollections,
                    ISNULL(SUM(mc.Quantity), 0)  AS TotalQty,
                    ISNULL(SUM(mc.Amount),   0)  AS TotalAmount
                FROM Collection.MilkCollection mc
                INNER JOIN Farmer.Farmers              f  ON f.FarmerId  = mc.FarmerId
                INNER JOIN Collection.CollectionCenters cc ON cc.CenterId = mc.CenterId
                WHERE mc.FarmerId  = @FarmerId
                  AND mc.CenterId  = @CenterId
                  AND mc.CollectionDate BETWEEN @FromDate AND @ToDate
                  AND mc.CollectionId NOT IN (
                      SELECT CollectionId FROM Finance.PaymentDetails
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
                                       DateTime fromDate, DateTime toDate, DateTime paymentDate)
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

        public List<FarmerPaymentModel> GetAllFarmerPayments()
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
                    f.FarmerName,
                    cc.CenterName,
                    pt.BankStatus,
                    pt.TransactionReference,
                    ba.BankName,
                    ba.AccountNumber,
                    ba.IFSCCode
                FROM Finance.FarmerPayments fp
                INNER JOIN Farmer.Farmers               f  ON f.FarmerId      = fp.FarmerId
                INNER JOIN Collection.CollectionCenters cc ON cc.CenterId     = fp.CenterId
                LEFT  JOIN Finance.PaymentTransactions  pt ON pt.PaymentId    = fp.PaymentId
                                                          AND pt.PaymentType  = 'Farmer'
                LEFT  JOIN Finance.BankAccounts         ba ON ba.BankAccountId = f.BankAccountId
                ORDER BY fp.PaymentDate DESC";

            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.CommandType = CommandType.Text;
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
                    f.FarmerName,
                    cc.CenterName,
                    pt.BankStatus,
                    pt.TransactionReference,
                    ba.BankName,
                    ba.AccountNumber,
                    ba.IFSCCode
                FROM Finance.FarmerPayments fp
                INNER JOIN Farmer.Farmers               f  ON f.FarmerId      = fp.FarmerId
                INNER JOIN Collection.CollectionCenters cc ON cc.CenterId     = fp.CenterId
                LEFT  JOIN Finance.PaymentTransactions  pt ON pt.PaymentId    = fp.PaymentId
                                                          AND pt.PaymentType  = 'Farmer'
                LEFT  JOIN Finance.BankAccounts         ba ON ba.BankAccountId = f.BankAccountId
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
        // A transfer is eligible when it has NO active (Pending/Processed/Failed)
        // center payment. Cancelled payments free the transfer for re-payment.
        //
        // Column sources (confirmed from schema):
        //   TestedFat / TestedCLR  →  Production.TransferQualityTests  (on TransferId)
        //   MilkTypeId             →  Collection.CenterMilkInventory   (on CenterId)
        //   Fallback fat/clr       →  Collection.CollectionBatches.AvgFat / AvgCLR
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
                    COALESCE(tqt.TestedFat, cb.AvgFat)   AS TestedFat,
                    COALESCE(tqt.TestedCLR, cb.AvgCLR)   AS TestedCLR,
                    -- MilkTypeId: prefer CenterMilkInventory, fall back to batch's most recent collection
                    COALESCE(
                        cmi.MilkTypeId,
                        (SELECT TOP 1 mc.MilkTypeId
                         FROM Collection.MilkCollection mc
                         WHERE mc.BatchId = cb.BatchId
                         ORDER BY mc.CollectionId DESC)
                    ) AS MilkTypeId,
                    cc.CenterName,
                    pp.PlantName,
                    CONCAT('T-', mt.TransferId,
                           ' | ', cc.CenterName,
                           ' | ', mt.ReceivedQty, ' L',
                           ' | ', FORMAT(mt.ReceivedDate,'dd-MMM-yyyy')) AS DisplayText
                FROM  Production.MilkTransfers              mt
                INNER JOIN Collection.CollectionBatches     cb  ON cb.BatchId   = mt.BatchId
                INNER JOIN Collection.CollectionCenters     cc  ON cc.CenterId  = cb.CenterId
                INNER JOIN Production.ProcessingPlants      pp  ON pp.PlantId   = mt.PlantId
                LEFT  JOIN Production.TransferQualityTests  tqt ON tqt.TransferId = mt.TransferId
                LEFT  JOIN Collection.CenterMilkInventory   cmi ON cmi.CenterId  = cb.CenterId
                WHERE mt.ReceivedQty IS NOT NULL
                  AND (@PlantId IS NULL OR mt.PlantId = @PlantId)
                  AND NOT EXISTS (
                        SELECT 1
                        FROM   Finance.CenterPayments cp
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
                            CenterName = r["CenterName"].ToString(),
                            PlantName = r["PlantName"].ToString()
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
        // Guards against duplicates at the C# level before hitting the SP.
        // The SP also has its own duplicate check as a second safety net.
        // ════════════════════════════════════════════════════════
        public int CreateCenterPayment(int transferId, decimal ratePerLiter, DateTime paymentDate)
        {
            // ── Guard: resolve BatchId + PlantId for this transfer ──
            string lookupSql = @"
                SELECT mt.BatchId, mt.PlantId
                FROM   Production.MilkTransfers mt
                WHERE  mt.TransferId = @TransferId";

            int batchId, plantId;
            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(lookupSql, con))
            {
                cmd.Parameters.AddWithValue("@TransferId", transferId);
                con.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (!r.Read())
                        throw new Exception($"Transfer T-{transferId} not found.");
                    batchId = Convert.ToInt32(r["BatchId"]);
                    plantId = Convert.ToInt32(r["PlantId"]);
                }
            }

            // ── Guard: block if an active (non-cancelled) payment already exists ──
            string dupCheckSql = @"
                SELECT COUNT(*)
                FROM   Finance.CenterPayments
                WHERE  BatchId       = @BatchId
                  AND  PlantId       = @PlantId
                  AND  PaymentStatus IN ('Pending', 'Processed', 'Failed')";

            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(dupCheckSql, con))
            {
                cmd.Parameters.AddWithValue("@BatchId", batchId);
                cmd.Parameters.AddWithValue("@PlantId", plantId);
                con.Open();
                int existing = Convert.ToInt32(cmd.ExecuteScalar());
                if (existing > 0)
                    throw new Exception(
                        $"A payment for Transfer T-{transferId} already exists and is active. " +
                        "Cancel it first before creating a new one.");
            }

            // ── Safe to create ──
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
            ISNULL(
                CONCAT('T-',
                    (SELECT TOP 1 mt2.TransferId FROM Production.MilkTransfers mt2
                     WHERE mt2.BatchId = cp.BatchId AND mt2.PlantId = cp.PlantId
                     ORDER BY mt2.TransferId),
                    ' | ',
                    FORMAT(
                        (SELECT TOP 1 mt2.ReceivedDate FROM Production.MilkTransfers mt2
                         WHERE mt2.BatchId = cp.BatchId AND mt2.PlantId = cp.PlantId
                         ORDER BY mt2.TransferId),
                    'dd-MMM-yyyy')),
                CONCAT('Batch-', cp.BatchId)
            ) AS BatchRef,
            (SELECT TOP 1 mt2.TransferId FROM Production.MilkTransfers mt2
             WHERE mt2.BatchId = cp.BatchId AND mt2.PlantId = cp.PlantId
             ORDER BY mt2.TransferId) AS OriginalTransferId,
            pt.BankStatus,
            pt.TransactionReference
        FROM Finance.CenterPayments cp
        INNER JOIN Collection.CollectionBatches   cb ON cb.BatchId  = cp.BatchId
        INNER JOIN Collection.CollectionCenters   cc ON cc.CenterId = cp.CenterId
        INNER JOIN Production.ProcessingPlants    pp ON pp.PlantId  = cp.PlantId
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
            ISNULL(
                CONCAT('T-',
                    (SELECT TOP 1 mt2.TransferId FROM Production.MilkTransfers mt2
                     WHERE mt2.BatchId = cp.BatchId AND mt2.PlantId = cp.PlantId
                     ORDER BY mt2.TransferId),
                    ' | ',
                    FORMAT(
                        (SELECT TOP 1 mt2.ReceivedDate FROM Production.MilkTransfers mt2
                         WHERE mt2.BatchId = cp.BatchId AND mt2.PlantId = cp.PlantId
                         ORDER BY mt2.TransferId),
                    'dd-MMM-yyyy')),
                CONCAT('Batch-', cp.BatchId)
            ) AS BatchRef,
            (SELECT TOP 1 mt2.TransferId FROM Production.MilkTransfers mt2
             WHERE mt2.BatchId = cp.BatchId AND mt2.PlantId = cp.PlantId
             ORDER BY mt2.TransferId) AS OriginalTransferId,
            pt.BankStatus,
            pt.TransactionReference
        FROM Finance.CenterPayments cp
        INNER JOIN Collection.CollectionBatches   cb ON cb.BatchId  = cp.BatchId
        INNER JOIN Collection.CollectionCenters   cc ON cc.CenterId = cp.CenterId
        INNER JOIN Production.ProcessingPlants    pp ON pp.PlantId  = cp.PlantId
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
                IFSCCode = reader["IFSCCode"] == DBNull.Value ? null : reader["IFSCCode"].ToString()
            };
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
                TransactionReference = r["TransactionReference"] == DBNull.Value ? null : r["TransactionReference"].ToString(),
                OriginalTransferId = r["OriginalTransferId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["OriginalTransferId"])
            };
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
    }
}