using DairyIndustry.Data;
using DairyIndustry.Models;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace DairyIndustry.Repositories
{
    public class SalesRepository : ISalesRepository
    {
        private readonly DbHelper _db;

        public SalesRepository(DbHelper db) => _db = db;


        // ═══════════════════════════════════════════════════════════════════
        //  REGISTRATION — SP: usp_Sales_RegisterDistributor
        //  Hashes password (SHA-256) here, passes hash to SP.
        //  SP inserts Sales.Distributors (Status=Pending) and
        //  Admin.Users (IsActive=0, StaffId=DistributorId).
        //  No DB change required — StaffId column already exists.
        // ═══════════════════════════════════════════════════════════════════
        public int RegisterDistributor(DistributorRegisterModel model)
        {
            string passwordHash = HashPassword(model.Password!);

            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand("Sales.usp_Sales_RegisterDistributor", con);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@DistributorName", model.DistributorName!);
            cmd.Parameters.AddWithValue("@Location", (object?)model.Location ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ContactNumber", (object?)model.ContactNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Email", (object?)model.Email ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Address", (object?)model.Address ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@GSTIN", (object?)model.GSTIN ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Username", model.Username!);
            cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);

            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Convert.ToInt32(reader["NewDistributorId"]) : 0;
        }


        // ═══════════════════════════════════════════════════════════════════
        //  LOGIN LOOKUP — inline query (no SP needed)
        //  Called by the common login controller.
        //  Joins Admin.Users → Admin.Roles → Sales.Distributors via StaffId.
        //  Common login controller then:
        //    1. Calls this to get the row
        //    2. Compares HashPassword(enteredPassword) == row.PasswordHash
        //    3. Checks row.IsActive == true (admin has approved)
        //    4. Sets session: "UserId", "Username", "RoleName",
        //                     "DistributorId", "DistributorName"
        // ═══════════════════════════════════════════════════════════════════
        public DistributorLoginResultModel? GetDistributorForLogin(string username)
        {
            const string sql = @"
                SELECT
                    u.UserId,
                    u.Username,
                    u.PasswordHash,
                    u.IsActive,
                    d.DistributorId,
                    d.DistributorName,
                    d.Location,
                    d.ContactNumber,
                    d.Email,
                    d.GSTIN,
                    d.Status
                FROM  Admin.Users        u
                INNER JOIN Admin.Roles         r ON r.RoleId       = u.RoleId
                INNER JOIN Sales.Distributors  d ON d.DistributorId = u.StaffId
                WHERE u.Username  = @Username
                  AND r.RoleName  = 'Distributor'";

            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Username", username);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            return new DistributorLoginResultModel
            {
                UserId = Convert.ToInt32(reader["UserId"]),
                Username = reader["Username"].ToString(),
                PasswordHash = reader["PasswordHash"].ToString(),
                IsActive = Convert.ToBoolean(reader["IsActive"]),
                DistributorId = Convert.ToInt32(reader["DistributorId"]),
                DistributorName = reader["DistributorName"].ToString(),
                Location = reader["Location"] == DBNull.Value ? null : reader["Location"].ToString(),
                ContactNumber = reader["ContactNumber"] == DBNull.Value ? null : reader["ContactNumber"].ToString(),
                Email = reader["Email"] == DBNull.Value ? null : reader["Email"].ToString(),
                GSTIN = reader["GSTIN"] == DBNull.Value ? null : reader["GSTIN"].ToString(),
                Status = reader["Status"].ToString()
            };
        }


        // ═══════════════════════════════════════════════════════════════════
        //  APPROVE / REJECT — SP: usp_Sales_ApproveDistributor
        //  action = "Approve" → Status=Approved, IsActive=1
        //  action = "Reject"  → Status=Rejected, IsActive=0
        // ═══════════════════════════════════════════════════════════════════
        public bool ApproveOrRejectDistributor(int distributorId, string action)
        {
            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand("Sales.usp_Sales_ApproveDistributor", con);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@DistributorId", distributorId);
            cmd.Parameters.AddWithValue("@Action", action);
            cmd.ExecuteNonQuery();
            return true;
        }


        // ═══════════════════════════════════════════════════════════════════
        //  SUSPEND — inline query
        //  Sets Status=Suspended + IsActive=0 on linked user (via StaffId)
        // ═══════════════════════════════════════════════════════════════════
        public bool SuspendDistributor(int distributorId)
            => SetDistributorState(distributorId, "Suspended", isActive: false);


        // ═══════════════════════════════════════════════════════════════════
        //  REINSTATE — inline query
        //  Sets Status=Approved + IsActive=1 on linked user (via StaffId)
        // ═══════════════════════════════════════════════════════════════════
        public bool ReinstateDistributor(int distributorId)
            => SetDistributorState(distributorId, "Approved", isActive: true);

        // Shared helper for Suspend / Reinstate
        private bool SetDistributorState(int distributorId, string status, bool isActive)
        {
            using var con = _db.GetConnection();
            con.Open();
            using var tx = con.BeginTransaction();
            try
            {
                using (var cmd = new SqlCommand(
                    "UPDATE Sales.Distributors SET Status = @Status WHERE DistributorId = @Id",
                    con, tx))
                {
                    cmd.Parameters.AddWithValue("@Status", status);
                    cmd.Parameters.AddWithValue("@Id", distributorId);
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = new SqlCommand(@"
                    UPDATE Admin.Users
                    SET    IsActive = @IsActive
                    WHERE  StaffId  = @DistributorId
                      AND  RoleId   = (SELECT RoleId FROM Admin.Roles WHERE RoleName = 'Distributor')",
                    con, tx))
                {
                    cmd.Parameters.AddWithValue("@IsActive", isActive ? 1 : 0);
                    cmd.Parameters.AddWithValue("@DistributorId", distributorId);
                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
                return true;
            }
            catch { tx.Rollback(); throw; }
        }


        // ═══════════════════════════════════════════════════════════════════
        //  GET DISTRIBUTORS — SP: usp_Sales_GetDistributors
        // ═══════════════════════════════════════════════════════════════════
        public List<DistributorModel> GetDistributors()
        {
            var list = new List<DistributorModel>();
            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand("Sales.usp_Sales_GetDistributors", con);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) list.Add(MapDistributor(reader));
            return list;
        }


        // ═══════════════════════════════════════════════════════════════════
        //  GET DISTRIBUTOR BY ID — SP: usp_Sales_GetDistributorById
        // ═══════════════════════════════════════════════════════════════════
        public DistributorModel? GetDistributorById(int distributorId)
        {
            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand("Sales.usp_Sales_GetDistributorById", con);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@DistributorId", distributorId);
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? MapDistributor(reader) : null;
        }


        // ═══════════════════════════════════════════════════════════════════
        //  ADD DISTRIBUTOR (admin direct-add) — SP: usp_Sales_AddDistributor
        //  Admin-added distributors start as Pending, no user account created.
        // ═══════════════════════════════════════════════════════════════════
        public int AddDistributor(DistributorFormModel model)
        {
            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand("Sales.usp_Sales_AddDistributor", con);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@DistributorName", model.DistributorName!);
            cmd.Parameters.AddWithValue("@Location", (object?)model.Location ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ContactNumber", (object?)model.ContactNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Email", (object?)model.Email ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Address", (object?)model.Address ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@GSTIN", (object?)model.GSTIN ?? DBNull.Value);

            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Convert.ToInt32(reader["NewDistributorId"]) : 0;
        }


        // ═══════════════════════════════════════════════════════════════════
        //  UPDATE DISTRIBUTOR — SP: usp_Sales_UpdateDistributor
        // ═══════════════════════════════════════════════════════════════════
        public bool UpdateDistributor(DistributorFormModel model)
        {
            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand("Sales.usp_Sales_UpdateDistributor", con);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@DistributorId", model.DistributorId);
            cmd.Parameters.AddWithValue("@DistributorName", model.DistributorName!);
            cmd.Parameters.AddWithValue("@Location", (object?)model.Location ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ContactNumber", (object?)model.ContactNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Email", (object?)model.Email ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Address", (object?)model.Address ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@GSTIN", (object?)model.GSTIN ?? DBNull.Value);

            cmd.ExecuteNonQuery();
            return true;
        }


        // ═══════════════════════════════════════════════════════════════════
        //  GET ORDERS — SP: usp_Sales_GetOrders
        // ═══════════════════════════════════════════════════════════════════
        public List<SalesOrderModel> GetOrders(int? distributorId, string? status,
                                               DateTime? fromDate, DateTime? toDate)
        {
            var list = new List<SalesOrderModel>();
            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand("Sales.usp_Sales_GetOrders", con);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@DistributorId", (object?)distributorId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@OrderStatus", (object?)status ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FromDate", (object?)fromDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ToDate", (object?)toDate ?? DBNull.Value);

            using var reader = cmd.ExecuteReader();
            while (reader.Read()) list.Add(MapOrder(reader));
            return list;
        }


        // ═══════════════════════════════════════════════════════════════════
        //  GET ORDER BY ID — inline query (includes PlantName)
        // ═══════════════════════════════════════════════════════════════════
        public SalesOrderModel? GetOrderById(int orderId)
        {
            const string sql = @"
                SELECT so.OrderId, so.DistributorId, so.OrderDate,
                       so.TotalAmount, so.OrderStatus, so.PlantId,
                       pp.PlantName,
                       d.DistributorName, d.Location, d.ContactNumber
                FROM   Sales.SalesOrders               so
                INNER JOIN Sales.Distributors          d  ON d.DistributorId = so.DistributorId
                LEFT  JOIN Production.ProcessingPlants pp ON pp.PlantId      = so.PlantId
                WHERE  so.OrderId = @OrderId";

            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@OrderId", orderId);
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? MapOrder(reader) : null;
        }


        // ═══════════════════════════════════════════════════════════════════
        //  CREATE ORDER — SP: usp_Sales_CreateOrder + inline UPDATE for PlantId
        // ═══════════════════════════════════════════════════════════════════
        public int CreateOrder(SalesOrderFormModel model)
        {
            using var con = _db.GetConnection();
            con.Open();

            int newOrderId;
            using (var cmd = new SqlCommand("Sales.usp_Sales_CreateOrder", con))
            {
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@DistributorId", model.DistributorId);
                cmd.Parameters.AddWithValue("@PlantId", model.PlantId);
                cmd.Parameters.AddWithValue("@OrderDate", model.OrderDate);
                using var reader = cmd.ExecuteReader();
                if (!reader.Read()) return 0;
                newOrderId = Convert.ToInt32(reader["NewOrderId"]);
            }

            if (newOrderId > 0 && model.PlantId > 0)
            {
                using var cmd = new SqlCommand(
                    "UPDATE Sales.SalesOrders SET PlantId = @PlantId WHERE OrderId = @OrderId", con);
                cmd.Parameters.AddWithValue("@PlantId", model.PlantId);
                cmd.Parameters.AddWithValue("@OrderId", newOrderId);
                cmd.ExecuteNonQuery();
            }

            return newOrderId;
        }


        // ═══════════════════════════════════════════════════════════════════
        //  UPDATE ORDER STATUS — SP: usp_Sales_UpdateOrderStatus
        // ═══════════════════════════════════════════════════════════════════
        public bool UpdateOrderStatus(int orderId, string status)
        {
            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand("Sales.usp_Sales_UpdateOrderStatus", con);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@OrderId", orderId);
            cmd.Parameters.AddWithValue("@OrderStatus", status);
            cmd.ExecuteNonQuery();
            return true;
        }


        // ═══════════════════════════════════════════════════════════════════
        //  GET ORDER DETAILS — SP: usp_Sales_GetOrderDetails
        // ═══════════════════════════════════════════════════════════════════
        public List<SalesOrderDetailModel> GetOrderDetails(int orderId)
        {
            var list = new List<SalesOrderDetailModel>();
            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand("Sales.usp_Sales_GetOrderDetails", con);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@OrderId", orderId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(new SalesOrderDetailModel
                {
                    OrderDetailId = Convert.ToInt32(reader["OrderDetailId"]),
                    ProductId = Convert.ToInt32(reader["ProductId"]),
                    ProductName = reader["ProductName"].ToString(),
                    ProductType = reader["ProductType"].ToString(),
                    Unit = reader["Unit"].ToString(),
                    Quantity = Convert.ToDecimal(reader["Quantity"]),
                    UnitPrice = Convert.ToDecimal(reader["UnitPrice"]),
                    LineTotal = Convert.ToDecimal(reader["LineTotal"])
                });
            return list;
        }


        // ═══════════════════════════════════════════════════════════════════
        //  ADD ORDER DETAIL — SP: usp_Sales_AddOrderDetail
        // ═══════════════════════════════════════════════════════════════════
        public bool AddOrderDetail(AddOrderDetailFormModel model)
        {
            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand("Sales.usp_Sales_AddOrderDetail", con);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@OrderId", model.OrderId);
            cmd.Parameters.AddWithValue("@ProductId", model.ProductId);
            cmd.Parameters.AddWithValue("@Quantity", model.Quantity);
            cmd.Parameters.AddWithValue("@UnitPrice", model.UnitPrice);
            cmd.ExecuteNonQuery();
            return true;
        }


        // ═══════════════════════════════════════════════════════════════════
        //  DASHBOARD SUMMARY — inline query
        // ═══════════════════════════════════════════════════════════════════
        public SalesDashboardSummaryModel GetDashboardSummary()
        {
            var model = new SalesDashboardSummaryModel();

            const string orderSql = @"
                SELECT
                    COUNT(*)                                                               AS TotalOrders,
                    SUM(CASE WHEN OrderStatus = 'Pending'   THEN 1 ELSE 0 END)            AS PendingOrders,
                    SUM(CASE WHEN OrderStatus = 'Delivered' THEN 1 ELSE 0 END)            AS DeliveredOrders,
                    SUM(CASE WHEN OrderStatus = 'Cancelled' THEN 1 ELSE 0 END)            AS CancelledOrders,
                    ISNULL(SUM(CASE WHEN OrderStatus = 'Delivered' THEN TotalAmount END),0) AS TotalRevenue,
                    ISNULL(SUM(CASE WHEN OrderStatus = 'Delivered'
                                    AND CAST(OrderDate AS DATE) = CAST(GETDATE() AS DATE)
                                    THEN TotalAmount END),0)                               AS TodayRevenue
                FROM Sales.SalesOrders";

            const string distSql =
                "SELECT COUNT(*) AS TotalDistributors FROM Sales.Distributors";

            using var con = _db.GetConnection();
            con.Open();

            using (var cmd = new SqlCommand(orderSql, con))
            using (var r = cmd.ExecuteReader())
            {
                if (r.Read())
                {
                    model.TotalOrders = r["TotalOrders"] == DBNull.Value ? 0 : Convert.ToInt32(r["TotalOrders"]);
                    model.PendingOrders = r["PendingOrders"] == DBNull.Value ? 0 : Convert.ToInt32(r["PendingOrders"]);
                    model.DeliveredOrders = r["DeliveredOrders"] == DBNull.Value ? 0 : Convert.ToInt32(r["DeliveredOrders"]);
                    model.CancelledOrders = r["CancelledOrders"] == DBNull.Value ? 0 : Convert.ToInt32(r["CancelledOrders"]);
                    model.TotalRevenue = r["TotalRevenue"] == DBNull.Value ? 0 : Convert.ToDecimal(r["TotalRevenue"]);
                    model.TodayRevenue = r["TodayRevenue"] == DBNull.Value ? 0 : Convert.ToDecimal(r["TodayRevenue"]);
                }
            }

            using (var cmd = new SqlCommand(distSql, con))
            using (var r = cmd.ExecuteReader())
            {
                if (r.Read())
                    model.TotalDistributors = r["TotalDistributors"] == DBNull.Value ? 0
                        : Convert.ToInt32(r["TotalDistributors"]);
            }

            return model;
        }


        // ═══════════════════════════════════════════════════════════════════
        //  ORDERS BY STATUS — inline query
        // ═══════════════════════════════════════════════════════════════════
        public List<OrderStatusCountModel> GetOrdersByStatus()
        {
            var list = new List<OrderStatusCountModel>();
            const string sql = @"
                SELECT OrderStatus,
                       COUNT(*)                    AS Count,
                       ISNULL(SUM(TotalAmount), 0) AS TotalAmount
                FROM Sales.SalesOrders
                GROUP BY OrderStatus
                ORDER BY Count DESC";

            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(sql, con);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(new OrderStatusCountModel
                {
                    OrderStatus = reader["OrderStatus"].ToString(),
                    Count = Convert.ToInt32(reader["Count"]),
                    TotalAmount = Convert.ToDecimal(reader["TotalAmount"])
                });
            return list;
        }


        // ═══════════════════════════════════════════════════════════════════
        //  DISTRIBUTOR SALES — inline query
        // ═══════════════════════════════════════════════════════════════════
        public List<DistributorSalesModel> GetDistributorSales()
        {
            var list = new List<DistributorSalesModel>();
            const string sql = @"
                SELECT d.DistributorId, d.DistributorName, d.Location,
                       COUNT(so.OrderId)                                              AS TotalOrders,
                       ISNULL(SUM(so.TotalAmount), 0)                                 AS TotalRevenue,
                       SUM(CASE WHEN so.OrderStatus = 'Delivered' THEN 1 ELSE 0 END)  AS DeliveredOrders,
                       SUM(CASE WHEN so.OrderStatus = 'Pending'   THEN 1 ELSE 0 END)  AS PendingOrders
                FROM Sales.Distributors d
                LEFT JOIN Sales.SalesOrders so ON so.DistributorId = d.DistributorId
                GROUP BY d.DistributorId, d.DistributorName, d.Location
                ORDER BY TotalRevenue DESC";

            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(sql, con);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(new DistributorSalesModel
                {
                    DistributorId = Convert.ToInt32(reader["DistributorId"]),
                    DistributorName = reader["DistributorName"].ToString(),
                    Location = reader["Location"] == DBNull.Value ? null : reader["Location"].ToString(),
                    TotalOrders = Convert.ToInt32(reader["TotalOrders"]),
                    TotalRevenue = Convert.ToDecimal(reader["TotalRevenue"]),
                    DeliveredOrders = Convert.ToInt32(reader["DeliveredOrders"]),
                    PendingOrders = Convert.ToInt32(reader["PendingOrders"])
                });
            return list;
        }


        // ═══════════════════════════════════════════════════════════════════
        //  GET PRODUCTS — inline query (active only)
        // ═══════════════════════════════════════════════════════════════════
        public List<ProductSalesModel> GetProducts()
        {
            var list = new List<ProductSalesModel>();
            const string sql = @"
                SELECT ProductId, ProductName, ProductType, Unit, MRP
                FROM Production.Products
                WHERE IsActive = 1
                ORDER BY ProductName";

            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(sql, con);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(new ProductSalesModel
                {
                    ProductId = Convert.ToInt32(reader["ProductId"]),
                    ProductName = reader["ProductName"].ToString(),
                    ProductType = reader["ProductType"].ToString(),
                    Unit = reader["Unit"].ToString(),
                    MRP = Convert.ToDecimal(reader["MRP"])
                });
            return list;
        }


        // ═══════════════════════════════════════════════════════════════════
        //  GET PLANTS — inline query (active only)
        // ═══════════════════════════════════════════════════════════════════
        public List<PlantModel> GetPlants()
        {
            var list = new List<PlantModel>();
            const string sql = @"
                SELECT PlantId, PlantName, Location
                FROM Production.ProcessingPlants
                WHERE IsActive = 1
                ORDER BY PlantName";

            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(sql, con);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(new PlantModel
                {
                    PlantId = Convert.ToInt32(reader["PlantId"]),
                    PlantName = reader["PlantName"].ToString(),
                    Location = reader["Location"] == DBNull.Value ? null : reader["Location"].ToString()
                });
            return list;
        }


        // ═══════════════════════════════════════════════════════════════════
        //  PASSWORD HASH — SHA-256 hex (public so common login controller
        //  can call SalesRepository.HashPassword() for verification)
        // ═══════════════════════════════════════════════════════════════════
        public static string HashPassword(string password)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(hash).ToLower();
        }


        // ═══════════════════════════════════════════════════════════════════
        //  PRIVATE MAPPERS
        // ═══════════════════════════════════════════════════════════════════
        private static SalesOrderModel MapOrder(SqlDataReader r)
        {
            var m = new SalesOrderModel
            {
                OrderId = Convert.ToInt32(r["OrderId"]),
                DistributorName = r["DistributorName"].ToString(),
                Location = r["Location"] == DBNull.Value ? null : r["Location"].ToString(),
                ContactNumber = r["ContactNumber"] == DBNull.Value ? null : r["ContactNumber"].ToString(),
                OrderDate = Convert.ToDateTime(r["OrderDate"]),
                TotalAmount = r["TotalAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(r["TotalAmount"]),
                OrderStatus = r["OrderStatus"].ToString()
            };
            if (HasColumn(r, "DistributorId"))
                m.DistributorId = Convert.ToInt32(r["DistributorId"]);
            if (HasColumn(r, "PlantId") && r["PlantId"] != DBNull.Value)
                m.PlantId = Convert.ToInt32(r["PlantId"]);
            if (HasColumn(r, "PlantName") && r["PlantName"] != DBNull.Value)
                m.PlantName = r["PlantName"].ToString();
            return m;
        }

        private static DistributorModel MapDistributor(SqlDataReader r) => new()
        {
            DistributorId = Convert.ToInt32(r["DistributorId"]),
            DistributorName = r["DistributorName"].ToString(),
            Location = r["Location"] == DBNull.Value ? null : r["Location"].ToString(),
            ContactNumber = r["ContactNumber"] == DBNull.Value ? null : r["ContactNumber"].ToString(),
            Email = r["Email"] == DBNull.Value ? null : r["Email"].ToString(),
            Address = r["Address"] == DBNull.Value ? null : r["Address"].ToString(),
            GSTIN = r["GSTIN"] == DBNull.Value ? null : r["GSTIN"].ToString(),
            Status = r["Status"] == DBNull.Value ? null : r["Status"].ToString(),
            RegisteredOn = r["RegisteredOn"] == DBNull.Value ? null : Convert.ToDateTime(r["RegisteredOn"])
        };

        private static bool HasColumn(SqlDataReader r, string name)
        {
            for (int i = 0; i < r.FieldCount; i++)
                if (string.Equals(r.GetName(i), name, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }
    }
}