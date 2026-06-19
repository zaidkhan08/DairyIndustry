using DairyIndustry.Data;
using DairyIndustry.Models.ViewModels;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DairyIndustry.Repositories
{
    public class HomeRepository : IHomeRepository
    {
        private readonly DbHelper _db;
        public HomeRepository(DbHelper db)
        {
            _db = db;
        }
        public LandingStatsModel GetLandingStats()
        {
            var stats = new LandingStatsModel();

            string query = @"
        -- Active Centers
        SELECT 'ActiveCenters'
              ,CAST(COUNT(*) AS DECIMAL)
        FROM Collection.CollectionCenters
        WHERE IsActive = 1
 
        UNION ALL
 
        -- Active Plants
        SELECT 'ActivePlants'
              ,CAST(COUNT(*) AS DECIMAL)
        FROM Production.ProcessingPlants
        WHERE IsActive = 1
 
        UNION ALL
 
        -- Active Farmers
        SELECT 'ActiveFarmers'
              ,CAST(COUNT(*) AS DECIMAL)
        FROM Farmer.Farmers
        WHERE IsActive = 1
 
        UNION ALL
 
        -- Active Staff
        SELECT 'ActiveStaff'
              ,CAST(COUNT(*) AS DECIMAL)
        FROM HR.Staffs
        WHERE IsActive = 1
 
        UNION ALL
 
        -- Active Drivers
        SELECT 'ActiveDrivers'
              ,CAST(COUNT(*) AS DECIMAL)
        FROM Logistics.DriversNew
        WHERE Status = 'Active'
 
        UNION ALL
 
        -- Active Distributors
        SELECT 'ActiveDistributors'
              ,CAST(COUNT(*) AS DECIMAL)
        FROM Sales.Distributors
        WHERE Status = 'Approved'
 
        UNION ALL
 
        -- Today Milk Collection (litres)
        SELECT 'TodayMilkCollection'
              ,ISNULL(SUM(Quantity), 0)
        FROM Collection.MilkCollection
        WHERE CollectionDate = CAST(GETDATE() AS DATE)
 
        UNION ALL
 
        -- This Month Milk Collection (litres)
        SELECT 'TotalMilkThisMonth'
              ,ISNULL(SUM(Quantity), 0)
        FROM Collection.MilkCollection
        WHERE MONTH(CollectionDate) = MONTH(GETDATE())
          AND YEAR(CollectionDate)  = YEAR(GETDATE())
 
        UNION ALL
 
        -- Open Batches
        SELECT 'OpenBatches'
              ,CAST(COUNT(*) AS DECIMAL)
        FROM Collection.CollectionBatches
        WHERE Status = 'Open'
 
        UNION ALL
 
        -- Pending Orders
        SELECT 'PendingOrders'
              ,CAST(COUNT(*) AS DECIMAL)
        FROM Sales.SalesOrders
        WHERE OrderStatus = 'Pending'
 
        UNION ALL
 
        -- Revenue This Month (from confirmed/dispatched/delivered orders)
        SELECT 'RevenueThisMonth'
              ,ISNULL(SUM(TotalAmount), 0)
        FROM Sales.SalesOrders
        WHERE OrderStatus IN ('Confirmed','Dispatched','Delivered')
          AND MONTH(OrderDate) = MONTH(GETDATE())
          AND YEAR(OrderDate)  = YEAR(GETDATE())
 
        UNION ALL
 
        -- Pending Payments (farmer + center combined)
        SELECT 'PendingPayments',
       ISNULL(
           (SELECT SUM(TotalAmount)
            FROM Finance.FarmerPayments
            WHERE PaymentStatus = 'Pending'), 0
       )
       +
       ISNULL(
           (SELECT SUM(TotalAmount)
            FROM Finance.CenterPayments
            WHERE PaymentStatus = 'Pending'), 0
       )";

            using (SqlConnection con = _db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.CommandType = CommandType.Text;
                con.Open();

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string key = reader.GetString(0);
                    decimal val = reader.GetDecimal(1);

                    switch (key)
                    {
                        case "ActiveCenters": stats.ActiveCenters = (int)val; break;
                        case "ActivePlants": stats.ActivePlants = (int)val; break;
                        case "ActiveFarmers": stats.ActiveFarmers = (int)val; break;
                        case "ActiveStaff": stats.ActiveStaff = (int)val; break;
                        case "ActiveDrivers": stats.ActiveDrivers = (int)val; break;
                        case "ActiveDistributors": stats.ActiveDistributors = (int)val; break;
                        case "TodayMilkCollection": stats.TodayMilkCollection = val; break;
                        case "TotalMilkThisMonth": stats.TotalMilkThisMonth = val; break;
                        case "OpenBatches": stats.OpenBatches = (int)val; break;
                        case "PendingOrders": stats.PendingOrders = (int)val; break;
                        case "RevenueThisMonth": stats.RevenueThisMonth = val; break;
                        case "PendingPayments": stats.PendingPayments = val; break;
                    }
                }
            }

            return stats;
        }
    }
}
