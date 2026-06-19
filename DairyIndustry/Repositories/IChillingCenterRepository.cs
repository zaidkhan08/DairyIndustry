using DairyIndustry.Models.ChillingStorage;

namespace DairyIndustry.Repositories
{
    public interface IChillingCenterRepository
    {
        // ── PLANT LOOKUP ───────────────────────────────────────
        PlantDropdownModel? GetPlantByUserId(int userId);

        // ── STORAGE CRUD ───────────────────────────────────────
        int StoreItem(ChillingStoreItemModel model);
        List<ChillingStorageModel> GetByPlant(int plantId, DateTime? fromDate, DateTime? toDate);
        List<ChillingStorageModel> GetAll(DateTime? fromDate, DateTime? toDate);
        ChillingStorageModel? GetById(int storageId);
        bool UpdateEntry(ChillingStoreItemModel model);
        bool DeleteEntry(int storageId);
        int InsertWithShift(ChillingStoreItemModel model);

        // ── ALERTS & MONITORING ────────────────────────────────
        // Fix #2 — plantId added so DB filters instead of C# filtering all records
        List<ChillingStorageModel> GetTemperatureAlerts(int? plantId,
                                                        DateTime? fromDate,
                                                        DateTime? toDate);

        // ── DASHBOARD ──────────────────────────────────────────
        ChillingDashboardSummaryModel GetDashboardSummary(int? plantId);   // Fix #3 — scoped to plant
        List<ChillingPlantCapacityModel> GetPlantCapacitySummary();

        // ── DROPDOWNS ──────────────────────────────────────────
        List<PlantDropdownModel> GetPlants();
        List<ProductDropdownModel> GetProducts();

        // ── SEARCH ─────────────────────────────────────────────
        List<ChillingStorageModel> GetByPlantFiltered(int plantId, DateTime? fromDate, DateTime? toDate, string? search);
        List<ChillingStorageModel> GetAllFiltered(DateTime? fromDate, DateTime? toDate, string? search);

        // ── QUICK EDIT ─────────────────────────────────────────
        bool QuickUpdateEntry(int storageId, decimal milkQuantity, decimal? temperature);

        // ── REPORTS ────────────────────────────────────────────
        List<ChillingDailyReportModel> GetDailyReport(int? plantId, DateTime? fromDate, DateTime? toDate);
        List<ChillingProductReportModel> GetProductReport(int? plantId, DateTime? fromDate, DateTime? toDate);

        // ── WEEKLY TREND ───────────────────────────────────────
        List<ChillingWeeklyTrendModel> GetWeeklyTrend(int? plantId);
    }
}