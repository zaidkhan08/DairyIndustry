using DairyIndustry.Models.ChillingStorage;

namespace DairyIndustry.Repositories
{
    public interface IChillingCenterRepository
    {
        // ── PLANT LOOKUP FROM SESSION ──────────────────────────
        // Called at start of every action using Session UserId
        // Queries Admin.UserPlants via usp_GetPlantByUserId
        PlantDropdownModel? GetPlantByUserId(int userId);

        // ── STORAGE CRUD ───────────────────────────────────────
        int StoreItem(ChillingStoreItemModel model);                            // SP 10.1
        List<ChillingStorageModel> GetByPlant(int plantId,                      // SP 10.2
                                              DateTime? fromDate,
                                              DateTime? toDate);
        List<ChillingStorageModel> GetAll(DateTime? fromDate, DateTime? toDate); // inline
        ChillingStorageModel? GetById(int storageId);                            // inline
        bool UpdateEntry(ChillingStoreItemModel model);                          // inline
        bool DeleteEntry(int storageId);                                         // inline

        // ── ALERTS & MONITORING ────────────────────────────────
        List<ChillingStorageModel> GetTemperatureAlerts(DateTime? fromDate,      // inline
                                                        DateTime? toDate);
        // ── DASHBOARD ──────────────────────────────────────────
        ChillingDashboardSummaryModel GetDashboardSummary();                     // inline
        List<ChillingPlantCapacityModel> GetPlantCapacitySummary();              // inline

        // ── DROPDOWNS ──────────────────────────────────────────
        List<PlantDropdownModel> GetPlants();                                    // inline
        List<ProductDropdownModel> GetProducts();                                // inline
    }
}