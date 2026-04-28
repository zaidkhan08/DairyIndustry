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
        //  REGISTER DISTRIBUTOR — SP: usp_Sales_RegisterDistributor
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
        //  LOGIN LOOKUP — inline query
        // ═══════════════════════════════════════════════════════════════════
        public DistributorLoginResultModel? GetDistributorForLogin(string username)
        {
            const string sql = @"
                SELECT u.UserId, u.Username, u.PasswordHash, u.IsActive,
                       d.DistributorId, d.DistributorName, d.Location,
                       d.ContactNumber, d.Email, d.GSTIN, d.Status
                FROM   Admin.Users        u
                INNER JOIN Admin.Roles         r ON r.RoleId       = u.RoleId
                INNER JOIN Sales.Distributors  d ON d.DistributorId = u.StaffId
                WHERE  u.Username = @Username
                  AND  r.RoleName = 'Distributor'";
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
        //  SUSPEND / REINSTATE — inline queries
        // ═══════════════════════════════════════════════════════════════════
        public bool SuspendDistributor(int distributorId)
            => SetDistributorState(distributorId, "Suspended", isActive: false);

        public bool ReinstateDistributor(int distributorId)
            => SetDistributorState(distributorId, "Approved", isActive: true);

        private bool SetDistributorState(int distributorId, string status, bool isActive)
        {
            using var con = _db.GetConnection();
            con.Open();
            using var tx = con.BeginTransaction();
            try
            {
                using (var cmd = new SqlCommand(
                    "UPDATE Sales.Distributors SET Status=@Status WHERE DistributorId=@Id", con, tx))
                {
                    cmd.Parameters.AddWithValue("@Status", status);
                    cmd.Parameters.AddWithValue("@Id", distributorId);
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = new SqlCommand(@"
                    UPDATE Admin.Users SET IsActive=@IsActive
                    WHERE  StaffId=@DistributorId
                      AND  RoleId=(SELECT RoleId FROM Admin.Roles WHERE RoleName='Distributor')",
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
        //  GET ORDERS — inline query (NOT the SP)
        //
        //  WHY inline instead of usp_Sales_GetOrders?
        //  The SP's SELECT does NOT include so.DistributorId.
        //  Without DistributorId in the result set, MapOrder() sets it to 0,
        //  which breaks:
        //    • MyOrders page  (distributor sees an empty list)
        //    • Details access check (order.DistributorId != myId → AccessDenied)
        //  This inline query is identical in filtering logic to the SP but
        //  adds so.DistributorId and so.PlantId to the SELECT so both work.
        // ═══════════════════════════════════════════════════════════════════
        public List<SalesOrderModel> GetOrders(int? distributorId, string? status,
                                               DateTime? fromDate, DateTime? toDate)
        {
            const string sql = @"
                SELECT so.OrderId,
                       so.DistributorId,
                       so.PlantId,
                       so.OrderDate,
                       so.OrderStatus,
                       so.TotalAmount,
                       d.DistributorName,
                       d.Location,
                       d.ContactNumber
                FROM   Sales.SalesOrders   so
                INNER JOIN Sales.Distributors d ON d.DistributorId = so.DistributorId
                WHERE  (@DistributorId IS NULL OR so.DistributorId = @DistributorId)
                  AND  (@OrderStatus   IS NULL OR so.OrderStatus   = @OrderStatus)
                  AND  (@FromDate      IS NULL OR so.OrderDate    >= @FromDate)
                  AND  (@ToDate        IS NULL OR so.OrderDate    <= @ToDate)
                ORDER  BY so.OrderDate DESC";

            var list = new List<SalesOrderModel>();
            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@DistributorId", (object?)distributorId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@OrderStatus", (object?)status ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FromDate", (object?)fromDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ToDate", (object?)toDate ?? DBNull.Value);
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) list.Add(MapOrder(reader));
            return list;
        }


        // ═══════════════════════════════════════════════════════════════════
        //  GET ORDER BY ID — inline (includes DistributorId + PlantName)
        // ═══════════════════════════════════════════════════════════════════
        public SalesOrderModel? GetOrderById(int orderId)
        {
            const string sql = @"
                SELECT so.OrderId, so.DistributorId, so.PlantId,
                       so.OrderDate, so.TotalAmount, so.OrderStatus,
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
        //  CREATE ORDER — SP (Admin only path)
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
                    "UPDATE Sales.SalesOrders SET PlantId=@PlantId WHERE OrderId=@OrderId", con);
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
        //  ADD OR MERGE ORDER DETAIL — inline (replaces SP for all callers)
        //
        //  Used by BOTH Admin (Details page) and Distributor (Details page).
        //  Rule: if the same ProductId already exists in this order, ADD
        //  the new quantity to the existing row and update UnitPrice to the
        //  latest MRP. Otherwise insert a new row.
        //  TotalAmount on the parent order is recalculated every time.
        //  UnitPrice must already be set to MRP by the controller before calling.
        // ═══════════════════════════════════════════════════════════════════
        public bool AddOrMergeOrderDetail(AddOrderDetailFormModel model)
        {
            using var con = _db.GetConnection();
            con.Open();
            using var tx = con.BeginTransaction();
            try
            {
                // Check if same product already on this order
                int existingDetailId = 0;
                using (var cmd = new SqlCommand(@"
                    SELECT OrderDetailId FROM Sales.SalesOrderDetails
                    WHERE  OrderId=@OrderId AND ProductId=@ProductId", con, tx))
                {
                    cmd.Parameters.AddWithValue("@OrderId", model.OrderId);
                    cmd.Parameters.AddWithValue("@ProductId", model.ProductId);
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                        existingDetailId = Convert.ToInt32(result);
                }

                if (existingDetailId > 0)
                {
                    // Merge — add quantity to existing row, refresh price
                    using var cmd = new SqlCommand(@"
                        UPDATE Sales.SalesOrderDetails
                        SET    Quantity  = Quantity + @Quantity,
                               UnitPrice = @UnitPrice
                        WHERE  OrderDetailId = @DetailId", con, tx);
                    cmd.Parameters.AddWithValue("@Quantity", model.Quantity);
                    cmd.Parameters.AddWithValue("@UnitPrice", model.UnitPrice);
                    cmd.Parameters.AddWithValue("@DetailId", existingDetailId);
                    cmd.ExecuteNonQuery();
                }
                else
                {
                    // New product on this order — insert
                    using var cmd = new SqlCommand(@"
                        INSERT INTO Sales.SalesOrderDetails
                            (OrderId, ProductId, Quantity, UnitPrice)
                        VALUES (@OrderId, @ProductId, @Quantity, @UnitPrice)", con, tx);
                    cmd.Parameters.AddWithValue("@OrderId", model.OrderId);
                    cmd.Parameters.AddWithValue("@ProductId", model.ProductId);
                    cmd.Parameters.AddWithValue("@Quantity", model.Quantity);
                    cmd.Parameters.AddWithValue("@UnitPrice", model.UnitPrice);
                    cmd.ExecuteNonQuery();
                }

                // Recalculate order total
                using (var cmd = new SqlCommand(@"
                    UPDATE Sales.SalesOrders
                    SET    TotalAmount = (
                        SELECT ISNULL(SUM(Quantity * UnitPrice), 0)
                        FROM   Sales.SalesOrderDetails
                        WHERE  OrderId = @OrderId)
                    WHERE  OrderId = @OrderId", con, tx))
                {
                    cmd.Parameters.AddWithValue("@OrderId", model.OrderId);
                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
                return true;
            }
            catch { tx.Rollback(); throw; }
        }


        // ═══════════════════════════════════════════════════════════════════
        //  ADD ORDER DETAIL — SP: usp_Sales_AddOrderDetail
        //  Kept for any legacy callers; new code uses AddOrMergeOrderDetail.
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
        //  PLACE DISTRIBUTOR ORDER — inline (smart merge, auto-MRP)
        //
        //  Called for ALL distributor order placements (from Create page
        //  and from MyOrders quick-order panel).
        //  NEVER calls usp_Sales_CreateOrder — that SP has an Approved check
        //  that throws "Distributor not found or not yet approved".
        //
        //  Logic (all in one transaction):
        //  1. Fetch MRP from Production.Products
        //  2. Find existing Pending order for this distributor on TODAY
        //  3. If none → inline INSERT a new order
        //  4. If same ProductId already in that order → UPDATE qty (merge)
        //     Else → INSERT new detail row
        //  5. Recalculate TotalAmount on the order
        // ═══════════════════════════════════════════════════════════════════
        public int PlaceDistributorOrder(int distributorId, int plantId, int productId, decimal quantity)
        {
            using var con = _db.GetConnection();
            con.Open();
            using var tx = con.BeginTransaction();
            try
            {
                // Step 1 — fetch MRP
                decimal unitPrice;
                using (var cmd = new SqlCommand(
                    "SELECT MRP FROM Production.Products WHERE ProductId=@ProductId AND IsActive=1",
                    con, tx))
                {
                    cmd.Parameters.AddWithValue("@ProductId", productId);
                    var result = cmd.ExecuteScalar();
                    if (result == null || result == DBNull.Value)
                        throw new InvalidOperationException("Product not found or is inactive.");
                    unitPrice = Convert.ToDecimal(result);
                }

                // Step 2 — find today's Pending order for this distributor
                int orderId = 0;
                using (var cmd = new SqlCommand(@"
                    SELECT TOP 1 OrderId FROM Sales.SalesOrders
                    WHERE  DistributorId = @DistributorId
                      AND  OrderStatus   = 'Pending'
                      AND  CAST(OrderDate AS DATE) = CAST(GETDATE() AS DATE)
                    ORDER  BY OrderId DESC", con, tx))
                {
                    cmd.Parameters.AddWithValue("@DistributorId", distributorId);
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                        orderId = Convert.ToInt32(result);
                }

                // Step 3 — no order today → create one (inline INSERT, no SP)
                if (orderId == 0)
                {
                    using var cmd = new SqlCommand(@"
                        INSERT INTO Sales.SalesOrders
                            (DistributorId, PlantId, OrderDate, TotalAmount, OrderStatus)
                        VALUES (@DistributorId, @PlantId, CAST(GETDATE() AS DATE), 0, 'Pending');
                        SELECT SCOPE_IDENTITY();", con, tx);
                    cmd.Parameters.AddWithValue("@DistributorId", distributorId);
                    cmd.Parameters.AddWithValue("@PlantId", plantId);
                    orderId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                // Step 4 — check if product already in this order
                int existingDetailId = 0;
                using (var cmd = new SqlCommand(@"
                    SELECT OrderDetailId FROM Sales.SalesOrderDetails
                    WHERE  OrderId=@OrderId AND ProductId=@ProductId", con, tx))
                {
                    cmd.Parameters.AddWithValue("@OrderId", orderId);
                    cmd.Parameters.AddWithValue("@ProductId", productId);
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                        existingDetailId = Convert.ToInt32(result);
                }

                // Step 4 cont. — merge or insert
                if (existingDetailId > 0)
                {
                    using var cmd = new SqlCommand(@"
                        UPDATE Sales.SalesOrderDetails
                        SET    Quantity  = Quantity + @Quantity,
                               UnitPrice = @UnitPrice
                        WHERE  OrderDetailId = @DetailId", con, tx);
                    cmd.Parameters.AddWithValue("@Quantity", quantity);
                    cmd.Parameters.AddWithValue("@UnitPrice", unitPrice);
                    cmd.Parameters.AddWithValue("@DetailId", existingDetailId);
                    cmd.ExecuteNonQuery();
                }
                else
                {
                    using var cmd = new SqlCommand(@"
                        INSERT INTO Sales.SalesOrderDetails
                            (OrderId, ProductId, Quantity, UnitPrice)
                        VALUES (@OrderId, @ProductId, @Quantity, @UnitPrice)", con, tx);
                    cmd.Parameters.AddWithValue("@OrderId", orderId);
                    cmd.Parameters.AddWithValue("@ProductId", productId);
                    cmd.Parameters.AddWithValue("@Quantity", quantity);
                    cmd.Parameters.AddWithValue("@UnitPrice", unitPrice);
                    cmd.ExecuteNonQuery();
                }

                // Step 5 — recalculate total
                using (var cmd = new SqlCommand(@"
                    UPDATE Sales.SalesOrders
                    SET    TotalAmount = (
                        SELECT ISNULL(SUM(Quantity * UnitPrice), 0)
                        FROM   Sales.SalesOrderDetails
                        WHERE  OrderId = @OrderId)
                    WHERE  OrderId = @OrderId", con, tx))
                {
                    cmd.Parameters.AddWithValue("@OrderId", orderId);
                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
                return orderId;
            }
            catch { tx.Rollback(); throw; }
        }


        // ═══════════════════════════════════════════════════════════════════
        //  GET PRODUCT BY ID — inline (for MRP auto-fill)
        // ═══════════════════════════════════════════════════════════════════
        public ProductSalesModel? GetProductById(int productId)
        {
            const string sql = @"
                SELECT ProductId, ProductName, ProductType, Unit, MRP
                FROM Production.Products
                WHERE ProductId=@ProductId AND IsActive=1";
            using var con = _db.GetConnection();
            con.Open();
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@ProductId", productId);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;
            return new ProductSalesModel
            {
                ProductId = Convert.ToInt32(reader["ProductId"]),
                ProductName = reader["ProductName"].ToString(),
                ProductType = reader["ProductType"].ToString(),
                Unit = reader["Unit"].ToString(),
                MRP = Convert.ToDecimal(reader["MRP"])
            };
        }


        // ═══════════════════════════════════════════════════════════════════
        //  DASHBOARD SUMMARY — inline
        // ═══════════════════════════════════════════════════════════════════
        public SalesDashboardSummaryModel GetDashboardSummary()
        {
            var model = new SalesDashboardSummaryModel();
            const string orderSql = @"
                SELECT
                    COUNT(*)                                                                AS TotalOrders,
                    SUM(CASE WHEN OrderStatus='Pending'   THEN 1 ELSE 0 END)               AS PendingOrders,
                    SUM(CASE WHEN OrderStatus='Delivered' THEN 1 ELSE 0 END)               AS DeliveredOrders,
                    SUM(CASE WHEN OrderStatus='Cancelled' THEN 1 ELSE 0 END)               AS CancelledOrders,
                    ISNULL(SUM(CASE WHEN OrderStatus='Delivered' THEN TotalAmount END), 0) AS TotalRevenue,
                    ISNULL(SUM(CASE WHEN OrderStatus='Delivered'
                                    AND CAST(OrderDate AS DATE)=CAST(GETDATE() AS DATE)
                                    THEN TotalAmount END), 0)                              AS TodayRevenue
                FROM Sales.SalesOrders";
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
            using (var cmd = new SqlCommand(
                "SELECT COUNT(*) AS TotalDistributors FROM Sales.Distributors", con))
            using (var r = cmd.ExecuteReader())
            {
                if (r.Read())
                    model.TotalDistributors = r["TotalDistributors"] == DBNull.Value ? 0
                        : Convert.ToInt32(r["TotalDistributors"]);
            }
            return model;
        }


        // ═══════════════════════════════════════════════════════════════════
        //  ORDERS BY STATUS — inline
        // ═══════════════════════════════════════════════════════════════════
        public List<OrderStatusCountModel> GetOrdersByStatus()
        {
            var list = new List<OrderStatusCountModel>();
            const string sql = @"
                SELECT OrderStatus,
                       COUNT(*)                    AS Count,
                       ISNULL(SUM(TotalAmount), 0) AS TotalAmount
                FROM Sales.SalesOrders
                GROUP BY OrderStatus ORDER BY Count DESC";
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
        //  DISTRIBUTOR SALES — inline
        // ═══════════════════════════════════════════════════════════════════
        public List<DistributorSalesModel> GetDistributorSales()
        {
            var list = new List<DistributorSalesModel>();
            const string sql = @"
                SELECT d.DistributorId, d.DistributorName, d.Location,
                       COUNT(so.OrderId)                                              AS TotalOrders,
                       ISNULL(SUM(so.TotalAmount), 0)                                 AS TotalRevenue,
                       SUM(CASE WHEN so.OrderStatus='Delivered' THEN 1 ELSE 0 END)    AS DeliveredOrders,
                       SUM(CASE WHEN so.OrderStatus='Pending'   THEN 1 ELSE 0 END)    AS PendingOrders
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
        //  GET PRODUCTS — inline (active only)
        // ═══════════════════════════════════════════════════════════════════
        public List<ProductSalesModel> GetProducts()
        {
            var list = new List<ProductSalesModel>();
            const string sql = @"
                SELECT ProductId, ProductName, ProductType, Unit, MRP
                FROM Production.Products WHERE IsActive=1 ORDER BY ProductName";
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
        //  GET PLANTS — inline (active only)
        // ═══════════════════════════════════════════════════════════════════
        public List<PlantModel> GetPlants()
        {
            var list = new List<PlantModel>();
            const string sql = @"
                SELECT PlantId, PlantName, Location
                FROM Production.ProcessingPlants WHERE IsActive=1 ORDER BY PlantName";
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
        //  HASH PASSWORD — SHA-256 hex, lowercase
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
            // These columns are present in all our inline queries but not in the old SP —
            // safe to read because we no longer call the SP for GetOrders / GetOrderById.
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