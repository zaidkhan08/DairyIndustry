using DairyIndustry.Models.Collection;
using DairyIndustry.Models.FarmerModel;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DairyIndustry.Repositories
{
    public interface ICollectionCenterRepository
    {
        // Dashboard
        DashboardViewModel GetCollectionCenterByStaff(int staffId);

        // Batch Status Page — shows all 3 shifts with Open/Closed/Not Started
        List<BatchStatusViewModel> GetBatchStatus(int centerId);

        // Milk Entry — BatchId & Shift resolved server-side, CollectionDate = today only
        public (int collectionId, decimal rate, decimal amount) RecordMilk(
        int farmerId,
        int centerId,
        int milkTypeId,
        string shift,   //  ADD
        decimal quantity,
        decimal fat,
        decimal clr);

        // Batch Collections (view entries per batch)
        List<BatchCollectionView> GetBatchCollections(int batchId);

        // Dropdowns
        List<FarmerViewModel> GetFarmers();
        List<MilkTypes> GetMilkTypes();

        // Inventory
        List<CenterInventoryViewModel> GetCenterInventory(int? centerId);
    }
      
}