using DairyIndustry.Models.Admin;
using DairyIndustry.Models.Collection;
using DairyIndustry.Models.FarmerModel;
using DairyIndustry.Models.Logistics;

namespace DairyIndustry.Repositories
{
    public interface ICollectionCenterRepository
    {

        // Dashboard — calls Collection.usp_Staff_Dashboard (3 result sets)
        StaffDashboardViewModel GetStaffDashboard(int staffId);

        // Milk Entry — calls Collection.usp_AddMilkEntry
        // NOTE: SP auto-detects CenterId, Shift, BatchId, Rate, Amount from StaffId + time
        int AddMilkCollection(int staffId, int farmerId, int milkTypeId,
                              decimal quantity, decimal appliedFat, decimal appliedCLR);

        // Today's entries — calls Collection.usp_GetTodayEntries
        // @StaffId + optional @Shift. Returns both shifts when shift is null.
        List<MilkCollectionViewModel> GetTodayMilkEntries(int staffId, string shift = null);

        // Single entry detail — calls Collection.usp_GetMilkEntryById
        // Staff can only view entries from their own center (enforced in SP)
        MilkCollectionViewModel GetMilkEntryById(int staffId, int collectionId);




        // Save rejection
        int RejectMilkEntry(MilkRejectionViewModel model, int staffId);

        // Get rejection history for a Center
        List<MilkRejectionViewModel> GetRejectionsByCenter(int centerId);

        // Get rejection history for a farmer
        List<MilkRejectionViewModel> GetRejectionsByFarmer(int farmerId);





        // Batch status (Morning + Evening) — calls Collection.usp_GetTodayBatchStatus
        // Returns exactly 2 rows. Batches open/close automatically via SQL Agent.
        // Staff CANNOT manually open or close batches.
        List<BatchStatusViewModel> GetTodayBatchStatus(int staffId);

        // Entries for a specific shift batch — calls Collection.usp_GetTodayEntries
        // Used by the ViewCollectionBatch page when user clicks a batch row
        List<MilkCollectionViewModel> GetBatchEntries(int staffId, string shift);

       //Dropdown to Select Farmer Farmer.usp_Farmer_GetByCenter
        List<FarmerViewModel> GetFarmers(int centerId);

        //It gives morining or evening shift from server time
        string GetCurrentShift();


        List<MilkTypes> GetMilkTypes();
        int GetCenterIdByStaffId(int staffId);

        //All Entries
        List<DateWiseMilkEntryViewModel> GetAllEntries(int centerId);

        //date wise milk entries 
        //   public List<DateWiseMilkEntryViewModel> GetEntriesByDate(DateTime date, int centerId);

        //inventory
        List<CenterInventoryViewModel> GetInventoryByCenter(int centerId);

        //farmer receipt
        FarmerReceiptViewModel GetReceiptByCollectionId(int id);

        //ratechartmodel
        List<RateChartModel> GetRateCharts();

        // Centers filtered by village (for self-registration dropdown)
        List<CenterDropdownModel> GetCentersByVillage(int villageId);



        // SP: Collection.usp_GetClosedBatchesForDispatch

        List<ClosedBatchDropdownItem> GetClosedBatchesForDispatch(int centerId);
        List<VehicleDropdownItem> GetActiveVehicles();
        List<PlantDropdownItem> GetAllPlants();
        int DispatchMilkTransfer(int batchId,int milkTypeId, int vehicleId, int plantId,
                                                             decimal dispatchQty, DateTime dispatchDate);
        List<DispatchHistoryViewModel> GetDispatchHistory(int centerId);


       
    }
}
