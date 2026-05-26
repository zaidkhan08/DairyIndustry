using DairyIndustry.Models.Admin;
using DairyIndustry.Models.Collection;
using DairyIndustry.Models.FarmerModel;
using DairyIndustry.Models.Logistics;
using CenterDropdownModel = DairyIndustry.Models.FarmerModel.CenterDropdownModel;

namespace DairyIndustry.Repositories
{
    public interface ICollectionCenterRepository
    {

        // Dashboard — calls Collection.usp_Staff_Dashboard (3 result sets)
        //StaffDashboardViewModel GetStaffDashboard(int staffId);
        StaffCenterModel GetStaffCenter(int staffId);
        TodaySummaryModel GetTodaySummary(int staffId);
        List<ShiftStatusModel> GetShiftStatus(int staffId);
        List<InventoryModel> GetInventory(int staffId);
        FarmerStatsModel GetFarmerStats(int staffId);


        int AddMilkCollection(int staffId, int farmerId, int milkTypeId,
                              decimal quantity, decimal appliedFat, decimal appliedCLR);

        List<MilkCollectionModel> GetTodayMilkEntries(int staffId, string shift = null);

 

        // Save rejection
        int RejectMilkEntry(MilkRejectionModel model, int staffId);



        // Get rejection history for a farmer
        List<MilkRejectionModel> GetRejectionsByFarmer(int farmerId);



        List<BatchStatusViewModel> GetTodayBatchStatus(int staffId);

        List<AllBatchsModel> GetAllBatchDetails(int staffId);
        //Dropdown to Select Farmer Farmer.usp_Farmer_GetByCenter
        List<FarmerViewModel> GetFarmers(int centerId);

        //It gives morining or evening shift from server time
        string GetCurrentShift();


        List<MilkTypes> GetMilkTypes();
        int GetCenterIdByStaffId(int staffId);

        //All Entries

        List<AllMilkEntriesModel> GetAllEntries(int centerId);

        //date wise milk entries 
        //   public List<DateWiseMilkEntryViewModel> GetEntriesByDate(DateTime date, int centerId);

        //inventory
        List<CenterInventoryViewModel> GetInventoryByCenter(int centerId);

        //farmer receipt
       // FarmerReceiptViewModel GetReceiptByCollectionId(int id);

        //milkreceipt to farmer
        FarmerMilkReceiptModel GetReceiptByCollectionId(int id);

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

        (decimal totalQty, decimal availableQty)GetMilkTypeBatchDetails(int batchId, int milkTypeId);

    }
}
