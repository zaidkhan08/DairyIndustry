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

            return null;  // no unpaid collections found
        }

        // ════════════════════════════════════════════════════════
        // CREATE FARMER PAYMENT — calls SP 8.4
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
        // RECORD STRIPE TRANSACTION — calls SP 8.7
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
        // PRIVATE HELPER
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
    }
}