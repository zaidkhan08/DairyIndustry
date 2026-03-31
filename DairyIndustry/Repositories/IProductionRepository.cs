using DairyIndustry.Models.Production;
using DairyIndustry.Models.Logistics;

namespace DairyIndustry.Repositories
{
    public interface IProductionRepository
    {
        // ════════════════════════════════════════════════════════
        // DROPDOWNS
        // ════════════════════════════════════════════════════════
        List<BatchDropdownModel> GetClosedBatches();
        List<VehiclesModel> GetAllVehicles();

        // ════════════════════════════════════════════════════════
        // MILK TRANSFERS
        // ════════════════════════════════════════════════════════
        int DispatchMilkTransfer(int batchId, int vehicleId, int plantId,
                                  decimal dispatchQty, DateTime dispatchDate);
        void ReceiveMilkTransfer(int transferId, decimal receivedQty, DateTime receivedDate);
        List<MilkTransferModel> GetAllTransfers();
        MilkTransferModel GetTransferById(int transferId);

        // ════════════════════════════════════════════════════════
        // PRODUCTS
        // ════════════════════════════════════════════════════════
        int AddProduct(string productName, string productType,
                                    decimal mrp, string unit, int? shelfLifeDays);
        List<ProductModel> GetAllProducts();
        ProductModel GetProductById(int productId);
        void UpdateProduct(ProductModel product);
        void DeleteProduct(int productId);

        // ════════════════════════════════════════════════════════
        // RAW MILK INVENTORY
        // ════════════════════════════════════════════════════════
        List<RawMilkInventoryModel> GetRawMilkInventory();

        // ════════════════════════════════════════════════════════
        // PRODUCTION BATCHES
        // ════════════════════════════════════════════════════════
        int StartProductionBatch(int plantId, int productId,
                                  decimal milkUsedQuantity, DateTime productionDate, int MilkTypeId);
        void UpdateBatchStatus(int productionBatchId, string batchStatus);
        List<ProductionBatchModel> GetAllProductionBatches();
        ProductionBatchModel GetProductionBatchById(int productionBatchId);

        // ════════════════════════════════════════════════════════
        // PRODUCT WASTAGE
        // ════════════════════════════════════════════════════════
        List<BatchForWastageModel> GetBatchesForWastage();   // InProgress + Completed
        int AddProductWastage(int batchId, int productId, decimal quantity, string reason);
        List<ProductWastageModel> GetAllProductWastage();
        List<ProductWastageModel> GetWastageByBatch(int batchId);
    }
}