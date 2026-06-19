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

        /// <summary>
        /// Dispatches milk. MilkTypeId is now a required parameter so the
        /// correct milk type is stored on MilkTransfers and used on receipt.
        /// </summary>
        int DispatchMilkTransfer(int batchId, int milkTypeId, int vehicleId, int plantId,
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

        /// <summary>
        /// MilkTypeId is stored on ProductionBatches so wastage logging
        /// never has to guess or fall back to a default.
        /// </summary>
        int StartProductionBatch(int plantId, int productId,
                                  decimal milkUsedQuantity, DateTime productionDate, int milkTypeId);

        /// <summary>
        /// Updates batch status. If newStatus == "QCFailed", automatically logs the
        /// full MilkUsedQuantity into Production.MilkProcessWastage (WastageType = 'QCFailed').
        /// MilkTypeId is read directly from ProductionBatches — no fallback guessing.
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
        int AddMilkProcessWastage(int productionBatchId, int plantId, int milkTypeId,
                                   decimal wastageQuantity, string reason);
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