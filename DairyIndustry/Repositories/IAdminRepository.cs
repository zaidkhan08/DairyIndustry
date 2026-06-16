using DairyIndustry.Models.Admin;
using System;
using System.Data;

namespace DairyIndustry.Repositories
{

    public interface IAdminRepository
    {
        // ── ROLES ──────────────────────────────────────────────
        int CreateRole(string roleName);
        List<RoleModel> GetAllRoles();
        UserProfileVM GetUserProfile(int userId);
        // ── USERS ──────────────────────────────────────────────
        int RegisterUser(string username, string passwordHash, int roleId, int? staffId);
        User GetUserByUsername(string username);
        List<User> GetAllUsers();
        void UpdateUserStatus(int userId, bool isActive);

        // ── AUDIT LOG ──────────────────────────────────────────
        void WriteAuditLog(int userId, string action, string entityName);
        List<AuditLogModel> GetAuditLogs(int? userId, string? entityName, DateTime? fromDate, DateTime? toDate);

        //Added By Zaid
        int? GetPlantIdByUser(int userId);

        int? GetCenterIdByUser(int userId);
        // ── AUDIT LOG ──────────────────────────────────────────


        // ════════════════════════════════════════════════════════
        // LOCATION — STATE
        // ════════════════════════════════════════════════════════
        int AddState(string stateName);
        List<StateModel> GetAllStates();

        // ════════════════════════════════════════════════════════
        // LOCATION — CITY
        // ════════════════════════════════════════════════════════
        int AddCity(string cityName, int stateId);
        List<CityModel> GetAllCities();
        List<CityModel> GetCitiesByState(int stateId);
    

        // ════════════════════════════════════════════════════════
        // LOCATION — VILLAGE
        // ════════════════════════════════════════════════════════
        int AddVillage(string villageName, int cityId);
        List<VillageModel> GetAllVillages();
        List<VillageModel> GetVillagesByCity(int cityId);

        // ════════════════════════════════════════════════════════
        // MILK TYPES
        // ════════════════════════════════════════════════════════
        int AddMilkType(string milkTypeName);
        List<MilkTypeModel> GetAllMilkTypes();

        // ════════════════════════════════════════════════════════
        // RATE CHART
        // ════════════════════════════════════════════════════════
        int AddRateChart(int milkTypeId, decimal fatFrom, decimal fatTo, decimal clrFrom, decimal clrTo, decimal ratePerLiter, DateTime effectiveFrom);
        List<RateChartModel> GetAllRateCharts();
        RateChartModel GetActiveRate(int milkTypeId, decimal fat, decimal clr, DateTime? asOfDate);

        // ════════════════════════════════════════════════════════
        // STAFF
        // ════════════════════════════════════════════════════════
        Task<int> AddStaffAsync(string firstName, string lastName, string phone, string email,
        int roleId, DateTime? doj,
        string bankName, string accountNumber, string ifscCode,
        decimal salary,
        string profilePhoto = null,
        int? centerId = null,
        int? plantId = null);
        Task<int> AddStaffWithUserAsync(string firstName, string lastName, string phone, string email,
     int roleId, DateTime? doj,
     string bankName, string accountNumber, string ifscCode,
     decimal salary,
     string profilePhoto = null,
     int? centerId = null,
     int? plantId = null,
     string username = null,
     string passwordHash = null);
        Task UpdateStaffAsync(int staffId, string firstName, string lastName,
                        string phone, string email, int roleId, DateTime? doj,
                        string bankName, string accountNumber, string ifscCode,
                        decimal salary, string profilePhoto,
                        int? centerId, int? plantId);
        Task UpdateStaffWithUserAsync(
    int staffId,
    string firstName,
    string lastName,
    string phone,
    string email,
    int roleId,
    DateTime? doj,
    string bankName,
    string accountNumber,
    string ifscCode,
    decimal salary,
    string profilePhoto,
    int? centerId,
    int? plantId,
    string username,
    string passwordHash);
        List<StaffModel> GetAllStaff(int? roleId = null, bool? isActive = null);

        void ToggleStaffActive(int staffId, bool isActive);
        List<StaffModel> GetUnlinkedStaff();
        StaffModel GetStaffById(int staffId);
        void AssignUserToPlant(int userId, int plantId);

        void AssignUserToCenter(int userId, int centerId);
        List<User> GetUsersByRole(string roleName);
        List<User> GetPlantManagers();
        List<User> GetCollectionAgents();
        // ════════════════════════════════════════════════════════
        // PLANT
        // ════════════════════════════════════════════════════════

        int AddPlant(string PlantName, string Location);

        List<PlantModel> GetAllPlants(bool? isActive = true);
        void TogglePlant(int id, bool isActive);
        void UpdatePlant(PlantModel plant);
        PlantModel getPlantById(int id);

        // ════════════════════════════════════════════════════════
        // COLLECTION
        // ════════════════════════════════════════════════════════

        int AddCollection(string CenterName, int VillageID,decimal Capacity,string Location);
        List<CollectionCenterModel> GetAllCollection(bool? isActive = true);
        
        void ToggleCollection(int id, bool isActive);
        void UpdateCollection(CollectionCenterModel collection);
        CollectionCenterModel getCollectionById(int id);


        // ════════════════════════════════════════════════════════
        // Production
        // ════════════════════════════════════════════════════════

        int AddProduct(string productName, string productType, decimal mrp,
                                    string unit, int? shelfLifeDays, string description,
                                    int createdBy);

        List<ProductModel> GetAllProducts(string productType = null, bool? isActive = true);
        ProductModel GetProductById(int productId);
        List<string> GetProductTypes();

        void UpdateProduct(int productId, string productName, string productType,
                           decimal mrp, string unit, int? shelfLifeDays,
                           string description, int modifiedBy);

        void ToggleProductStatus(int productId, bool isActive, int modifiedBy);

        List<ProductModel> GetActiveProducts();

        List<ProductionBatchModel> GetProductionBatches(int? plantId = null, int? productId = null,
                                                         string batchStatus = null,
                                                         DateTime? fromDate = null, DateTime? toDate = null);

        List<MilkTransferModel> GetMilkTransfers(int? plantId = null, int? centerId = null,
                                          DateTime? fromDate = null, DateTime? toDate = null);

        //collection center
        List<CollectionCenterModel> GetAllCenters();

        List<BatchModel> GetAllBatches(int? centerId = null, string status = null,
                               DateTime? fromDate = null, DateTime? toDate = null);

        BatchModel GetBatchById(int batchId);

        int OpenBatch(int centerId, string shift, DateTime batchDate);

        void CloseBatch(int batchId);

        List<BatchCollectionEntryModel> GetBatchCollections(int batchId);

        //Distributers
        List<Distributor> GetDistributors();
        int RegisterDistributor(Distributor distributor, string username, string passwordHash);
        bool UpdateDistributorStatus(int distributorId, string status);
        Distributor? GetDistributorById(int distributorId);
        List<Distributor> GetPendingDistributors();

        //Assign plant and collection to user
        List<UserAssignmentViewModel> GetAllUserPlantAssignments();
        List<UserAssignmentViewModel> GetAllUserCenterAssignments();

        //Order place behalf of distributor
        int CreateOrder(AdminOrderModel model);
        bool UpdateOrderStatus(int orderId, string status);
        List<OrderSummary> GetAllOrders(int? distributorId, string? status, DateTime? fromDate, DateTime? toDate);


        List<PlantModel> GetActivePlants();
        //Notification
        List<NotificationModel> GetNotifications();
        int GetNotificationCount();
        bool MarkNotificationRead(int notificationId);
        bool MarkAllNotificationsRead();

        //Email

        void SendOtpEmail(string toEmail, string toName, string otp, string purpose);
        void ChangePassword(int userId, string newPasswordHash);

        //Index

        List<ChartPoint> GetMilkCollectedLast7Days();

        /// <summary>
        /// Returns (TotalMilkCollected, TodayMilkCollected, TotalFarmers,
        ///          ActiveFarmers, OpenBatches, ClosedBatches, DispatchedBatches)
        /// </summary>
        DashboardCollectionSummary GetCollectionSummary();

        /// <summary>
        /// Returns aggregated finance totals for farmer / staff / center payments
        /// and total sales revenue.
        /// </summary>
        DashboardFinanceSummary GetFinanceSummary();

        /// <summary>
        /// Returns top 5 products ranked by total milk used in production.
        /// </summary>
        List<ChartPoint> GetTopProductsByMilkUsed(int top = 5);

        /// <summary>
        /// Returns count of sales orders grouped by status.
        /// </summary>
        List<ChartPoint> GetOrdersByStatus();
    }

}
