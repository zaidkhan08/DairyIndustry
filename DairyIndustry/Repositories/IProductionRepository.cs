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
                                  decimal milkUsedQuantity, DateTime productionDate, int MilkTypeId);
        void UpdateBatchStatus(int productionBatchId, string batchStatus);
        List<ProductionBatchModel> GetAllProductionBatches(int? plantId = null);
        ProductionBatchModel GetProductionBatchById(int productionBatchId);

        // ════════════════════════════════════════════════════════
        // PRODUCT WASTAGE
        // ════════════════════════════════════════════════════════
        List<BatchForWastageModel> GetBatchesForWastage();   // InProgress + Completed
        int AddProductWastage(int batchId, int productId, decimal quantity, string reason);
        List<ProductWastageModel> GetAllProductWastage(int? plantId = null);
        List<ProductWastageModel> GetWastageByBatch(int batchId);

        // ════════════════════════════════════════════════════════
        // TRANSFER QUALITY TESTS
        // ════════════════════════════════════════════════════════

        /// <summary>Returns all quality tests, optionally filtered by plant.</summary>
        List<QualityTestModel> GetAllQualityTests(int? plantId = null);

        /// <summary>Returns the quality test for a single transfer (null if not yet tested).</summary>
        QualityTestModel GetQualityTestByTransfer(int transferId);

        /// <summary>
        /// Inserts a new quality test record.
        /// Returns the newly created TestId.
        /// </summary>
        int AddQualityTest(int transferId, decimal testedFat, decimal testedCLR, DateTime testDate);
    }
}