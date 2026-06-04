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

        // NEW: inline INSERT that includes Shift column
        // Called instead of StoreItem SP when Shift is provided
        int InsertWithShift(ChillingStoreItemModel model);

        // ── ALERTS & MONITORING ────────────────────────────────
        List<ChillingStorageModel> GetTemperatureAlerts(DateTime? fromDate, DateTime? toDate);

        // ── DASHBOARD ──────────────────────────────────────────
        ChillingDashboardSummaryModel GetDashboardSummary();
        List<ChillingPlantCapacityModel> GetPlantCapacitySummary();

        // ── DROPDOWNS ──────────────────────────────────────────
        List<PlantDropdownModel> GetPlants();
        List<ProductDropdownModel> GetProducts();

        // ── SEARCH (Feature: Search by Item Name) ─────────────
        List<ChillingStorageModel> GetByPlantFiltered(int plantId, DateTime? fromDate, DateTime? toDate, string? search);
        List<ChillingStorageModel> GetAllFiltered(DateTime? fromDate, DateTime? toDate, string? search);

        // ── QUICK EDIT (Feature: Quick Edit inline) ────────────
        // Updates only MilkQuantity and Temperature — used by inline popover on Index
        bool QuickUpdateEntry(int storageId, decimal milkQuantity, decimal? temperature);

        // ── DAILY REPORT (Feature: Daily Report) ──────────────
        List<ChillingDailyReportModel> GetDailyReport(int? plantId, DateTime? fromDate, DateTime? toDate);

        // ── NEW: PRODUCT REPORT (Feature 2) ───────────────────
        // Groups entries by ItemName — used for By Product tab on Report page
        List<ChillingProductReportModel> GetProductReport(int? plantId, DateTime? fromDate, DateTime? toDate);

        // ── NEW: WEEKLY TREND (Feature 3) ─────────────────────
        // Returns last 7 days quantity per day per plant — used for Dashboard sparklines
        List<ChillingWeeklyTrendModel> GetWeeklyTrend(int? plantId);
    }
}