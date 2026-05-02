using DairyIndustry.Models;

namespace DairyIndustry.Repositories
{
    public interface ISalesRepository
    {
        // ── REGISTRATION ──────────────────────────────────────────────────
        // SP: usp_Sales_RegisterDistributor
        // Hashes password in C# (SHA-256), passes hash to SP.
        // SP creates Sales.Distributors (Pending) + Admin.Users (IsActive=0).
        // Returns new DistributorId, throws on duplicate username/GSTIN.
        int RegisterDistributor(DistributorRegisterModel model);

        // ── LOGIN LOOKUP (for common login controller) ────────────────────
        // SP: usp_Sales_DistributorLogin
        // Returns joined user+distributor row by username.
        // Common login controller verifies PasswordHash then sets session.
        DistributorLoginResultModel? GetDistributorForLogin(string username);

        // ── APPROVE / REJECT (SP: usp_Sales_ApproveDistributor) ──────────
        // action = "Approve" → Status=Approved, IsActive=1
        // action = "Reject"  → Status=Rejected, IsActive=0
        bool ApproveOrRejectDistributor(int distributorId, string action);

        // ── SUSPEND / REINSTATE (inline — no new SP needed) ───────────────
        // Suspend: Status=Suspended, IsActive=0
        // Reinstate: Status=Approved, IsActive=1
        bool SuspendDistributor(int distributorId);
        bool ReinstateDistributor(int distributorId);

        // ── DISTRIBUTORS (existing SPs) ───────────────────────────────────
        List<DistributorModel> GetDistributors();
        DistributorModel? GetDistributorById(int distributorId);
        int AddDistributor(DistributorFormModel model);       // admin direct-add (SP)
        bool UpdateDistributor(DistributorFormModel model);   // SP

        // ── ORDERS (existing SPs + inline) ────────────────────────────────
        List<SalesOrderModel> GetOrders(int? distributorId, string? status,
                                        DateTime? fromDate, DateTime? toDate);
        SalesOrderModel? GetOrderById(int orderId);
        int CreateOrder(SalesOrderFormModel model);
        bool UpdateOrderStatus(int orderId, string status);

        // ── ORDER LINE ITEMS ───────────────────────────────────────────────
        List<SalesOrderDetailModel> GetOrderDetails(int orderId);

        // Merge-aware insert: if same ProductId already in order → adds qty.
        // Used by both Admin and Distributor from the Details page.
        // UnitPrice must be set to MRP by the caller before invoking.
        bool AddOrMergeOrderDetail(AddOrderDetailFormModel model);

        // Legacy SP wrapper — kept so existing call sites still compile.
        bool AddOrderDetail(AddOrderDetailFormModel model);

        // ── DISTRIBUTOR ORDER PLACEMENT (smart merge logic) ───────────────
        // Used when a Distributor places an order from the portal.
        // • Finds or creates a Pending order for this distributor on today's date.
        // • If a detail row for the same ProductId already exists on that order,
        //   adds the new quantity to it (merge). Otherwise inserts a new row.
        // • UnitPrice is always auto-fetched from Production.Products.MRP —
        //   the distributor never supplies a price.
        // Returns the OrderId (existing or newly created).
        int PlaceDistributorOrder(int distributorId, int plantId, int productId, decimal quantity);

        // ── PRODUCTS ──────────────────────────────────────────────────────
        // Returns a single product (used for MRP auto-fill).
        ProductSalesModel? GetProductById(int productId);

        // ── DASHBOARD (inline queries) ─────────────────────────────────────
        SalesDashboardSummaryModel GetDashboardSummary();
        List<OrderStatusCountModel> GetOrdersByStatus();
        List<DistributorSalesModel> GetDistributorSales();

        // ── DROPDOWNS (inline queries) ─────────────────────────────────────
        List<ProductSalesModel> GetProducts();
        List<PlantModel> GetPlants();

        // ── DISTRIBUTOR ANALYTICS ─────────────────────────────────────────
        DistributorAnalyticsModel GetDistributorAnalytics(int distributorId);

        // ── NOTIFICATION SEEN (DB-persisted across logins) ────────────────
        // Returns (OrderId, Status) pairs the distributor has already seen.
        // Keyed as "OrderId_Status" so re-seeing after status change works.
        HashSet<string> GetSeenOrderKeys(int distributorId);
        // Marks given orders as seen at their current status.
        void MarkOrdersSeen(int distributorId, IEnumerable<(int OrderId, string Status)> orders);
    }
}