using DairyIndustry.Models.Collection;
using DairyIndustry.Models.FarmerModel;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DairyIndustry.Repositories.Interfaces
{
    public interface ICollectionCenterRepository
    {
       // CollectionCenter GetCollectionCenterByStaff(int staffId);

        int OpenBatch(int centerId, string shift, DateTime batchDate);
        bool CloseBatch(int batchId);
        (int collectionId, decimal rate, decimal amount) RecordMilk(
       int farmerId,
       int centerId,
       int milkTypeId,
       int batchId,
       decimal quantity,
       string shift,
       DateTime collectionDate,
       decimal fat,
       decimal clr
   );

        List<Farmer> GetFarmers();

        List<BatchCollectionView> GetBatchCollections(int batchId);
        int GetCurrentBatchId(int centerId);

        List<BatchViewModel> GetBatchesByCenter(int centerId);

        List<MilkTypes> GetMilkTypes();
        List<BatchViewModel> GetOpenBatches(int centerId);


        List<CenterInventoryViewModel> GetCenterInventory(int? centerId);


        DashboardViewModel GetCollectionCenterByStaff(int staffId);
    }
      
}