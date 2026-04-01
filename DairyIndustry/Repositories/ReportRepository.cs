using DairyIndustry.Data;
using DairyIndustry.Models.Reports;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DairyIndustry.Repositories
{
    public class ReportRepository:IReportRepository
    {
        private readonly DbHelper _db;

        public ReportRepository(DbHelper db)
        {
            _db = db;
        }

        public List<DailySummaryModel> GetDailySummaryByCenter(int? centerId, DateTime? fromDate, DateTime? toDate)
        {
            var list = new List<DailySummaryModel>();

            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Reports.usp_Report_DailySummaryByCenter", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@CenterId", (object?)centerId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FromDate", (object?)fromDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ToDate", (object?)toDate ?? DBNull.Value);

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new DailySummaryModel
                            {
                                CenterId = Convert.ToInt32(reader["CenterId"]),
                                CenterName = reader["CenterName"].ToString(),
                                CollectionDate = Convert.ToDateTime(reader["CollectionDate"]),
                                Shift = reader["Shift"].ToString(),
                                MilkTypeName = reader["MilkTypeName"].ToString(),
                                TotalEntries = Convert.ToInt32(reader["TotalEntries"]),
                                TotalQuantityLtr = Convert.ToDecimal(reader["TotalQuantityLtr"]),
                                AvgFat = Convert.ToDecimal(reader["AvgFat"]),
                                AvgCLR = Convert.ToDecimal(reader["AvgCLR"]),
                                TotalAmount = Convert.ToDecimal(reader["TotalAmount"])
                            });
                        }
                    }
                }
            }

            return list;
        }
        public List<FarmerCollectionModel> GetCollectionByFarmer(int? centerId, DateTime? fromDate, DateTime? toDate)
        {
            var list = new List<FarmerCollectionModel>();

            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Reports.usp_Report_MilkCollectionByFarmer", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@CenterId", (object?)centerId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FromDate", (object?)fromDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ToDate", (object?)toDate ?? DBNull.Value);

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new FarmerCollectionModel
                            {
                                FarmerId = Convert.ToInt32(reader["FarmerId"]),
                                FarmerName = reader["FarmerName"].ToString(),
                                CenterName = reader["CenterName"].ToString(),
                                MilkTypeName = reader["MilkTypeName"].ToString(),
                                TotalEntries = Convert.ToInt32(reader["TotalEntries"]),
                                TotalQtyLtr = Convert.ToDecimal(reader["TotalQtyLtr"]),
                                AvgFat = Convert.ToDecimal(reader["AvgFat"]),
                                AvgCLR = Convert.ToDecimal(reader["AvgCLR"]),
                                TotalPayable = Convert.ToDecimal(reader["TotalPayable"])
                            });
                        }
                    }
                }
            }

            return list;
        }

        public List<ProductionSummaryModel> GetPlantProductionSummary(int? plantId, DateTime? fromDate, DateTime? toDate)
        {
            var list = new List<ProductionSummaryModel>();

            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Reports.usp_Report_PlantProductionSummary", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@PlantId", (object?)plantId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FromDate", (object?)fromDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ToDate", (object?)toDate ?? DBNull.Value);

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new ProductionSummaryModel
                            {
                                PlantName = reader["PlantName"].ToString(),
                                ProductName = reader["ProductName"].ToString(),
                                ProductType = reader["ProductType"].ToString(),
                                TotalBatches = Convert.ToInt32(reader["TotalBatches"]),
                                TotalMilkUsedLtr = Convert.ToDecimal(reader["TotalMilkUsedLtr"]),
                                BatchStatus = reader["BatchStatus"].ToString()
                            });
                        }
                    }
                }
            }

            return list;
        }

        public List<WastageModel> GetWastageSummary(int? plantId, DateTime? fromDate, DateTime? toDate)
        {
            var list = new List<WastageModel>();

            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Reports.usp_Report_WastageSummary", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@PlantId", (object?)plantId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FromDate", (object?)fromDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ToDate", (object?)toDate ?? DBNull.Value);

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new WastageModel
                            {
                                WastageCategory = reader["WastageCategory"].ToString(),
                                PlantName = reader["PlantName"].ToString(),
                                Item = reader["Item"].ToString(),
                                TotalWastage = Convert.ToDecimal(reader["TotalWastage"]),
                                Unit = reader["Unit"].ToString(),
                                WastageType = reader["WastageType"] == DBNull.Value ? null : reader["WastageType"].ToString()
                            });
                        }
                    }
                }
            }

            return list;
        }

        public List<SalesReportModel> GetSalesReport(int? distributorId, DateTime? fromDate, DateTime? toDate)
        {
            var list = new List<SalesReportModel>();

            using (SqlConnection con = _db.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand("Reports.usp_Report_SalesReport", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@DistributorId", (object?)distributorId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FromDate", (object?)fromDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ToDate", (object?)toDate ?? DBNull.Value);

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new SalesReportModel
                            {
                                DistributorName = reader["DistributorName"].ToString(),
                                OrderDate = Convert.ToDateTime(reader["OrderDate"]),
                                OrderStatus = reader["OrderStatus"].ToString(),
                                ProductName = reader["ProductName"].ToString(),
                                ProductType = reader["ProductType"].ToString(),
                                Quantity = Convert.ToDecimal(reader["Quantity"]),
                                UnitPrice = Convert.ToDecimal(reader["UnitPrice"]),
                                LineTotal = Convert.ToDecimal(reader["LineTotal"])
                            });
                        }
                    }
                }
            }

            return list;
        }
    }
}
