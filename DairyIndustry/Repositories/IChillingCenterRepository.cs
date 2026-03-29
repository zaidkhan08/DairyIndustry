using DairyIndustry.Models;

namespace DairyIndustry.Repositories
{
    public interface IChillingCenterRepository
    {
        // CRUD 
        List<ChillingStorageModel> GetAll(DateTime? fromDate, DateTime? toDate);
        ChillingStorageModel GetById(int storageId);
        int StoreItem(ChillingStoreItemModel model);        // returns new StorageId
        bool UpdateEntry(ChillingStoreItemModel model);
        bool DeleteEntry(int storageId);

        // FILTERS 
        List<ChillingStorageModel> GetByPlant(int plantId, DateTime? fromDate, DateTime? toDate);
        List<ChillingStorageModel> GetTemperatureAlerts(DateTime? fromDate, DateTime? toDate);

        // DASHBOARD 
        ChillingDashboardSummaryModel GetDashboardSummary();
        List<ChillingPlantCapacityModel> GetPlantCapacitySummary();

        // DROPDOWNS 
        List<PlantDropdownModel> GetPlants();
        List<ProductDropdownModel> GetProducts();
    }
}
