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
        List<MilkTransferModel> GetAllTransfers(int? plantId = null);
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
        List<RawMilkInventoryModel> GetRawMilkInventory(int? plantId = null);

        // ════════════════════════════════════════════════════════
        // PRODUCTION BATCHES
        // ════════════════════════════════════════════════════════
        int StartProductionBatch(int plantId, int productId,
                                  decimal milkUsedQuantity, DateTime productionDate, int milkTypeId);

        /// <summary>
        /// Updates batch status. If newStatus == "QCFailed", automatically logs the
        /// full MilkUsedQuantity into Production.MilkProcessWastage with WastageType = 'QCFailed'.
        /// </summary>
        void UpdateBatchStatus(int productionBatchId, string batchStatus);

        List<ProductionBatchModel> GetAllProductionBatches(int? plantId = null);
        ProductionBatchModel GetProductionBatchById(int productionBatchId);

        // ════════════════════════════════════════════════════════
        // PRODUCT WASTAGE  (finished-goods wastage)
        // ════════════════════════════════════════════════════════
        List<BatchForWastageModel> GetBatchesForWastage();
        int AddProductWastage(int batchId, int productId, decimal quantity, string reason);
        List<ProductWastageModel> GetAllProductWastage(int? plantId = null);
        List<ProductWastageModel> GetWastageByBatch(int batchId);

        // ════════════════════════════════════════════════════════
        // MILK PROCESS WASTAGE  (raw-milk lost during production)
        // ════════════════════════════════════════════════════════

        /// <summary>
        /// Manually records a partial raw-milk process wastage for an InProgress batch.
        /// WastageType is always stored as 'ProcessWastage'.
        /// </summary>
        int AddMilkProcessWastage(int productionBatchId, int plantId, int milkTypeId,
                                   decimal wastageQuantity, string reason);

        /// <summary>
        /// Returns all milk process wastage records, optionally scoped to a plant.
        /// Includes both QCFailed (auto) and ProcessWastage (manual) entries.
        /// </summary>
        List<MilkProcessWastageModel> GetAllMilkProcessWastage(int? plantId = null);

        // ════════════════════════════════════════════════════════
        // TRANSFER QUALITY TESTS
        // ════════════════════════════════════════════════════════
        List<QualityTestModel> GetAllQualityTests(int? plantId = null);
        QualityTestModel GetQualityTestByTransfer(int transferId);
        int AddQualityTest(int transferId, decimal testedFat, decimal testedCLR, DateTime testDate);

        // ════════════════════════════════════════════════════════
        // TRANSFER LOSS LOG
        // ════════════════════════════════════════════════════════
        List<TransferLossLogModel> GetTransferLossLog(int? plantId = null);
        List<LossSummaryModel> GetLossSummary(int? plantId = null);
    }
}