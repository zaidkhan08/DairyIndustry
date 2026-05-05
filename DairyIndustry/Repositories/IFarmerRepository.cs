using DairyIndustry.Models.Admin;
using DairyIndustry.Models.Collection;
using DairyIndustry.Models.FarmerModel;

namespace DairyIndustry.Repositories
{
    public interface IFarmerRepository
    {
       
        List<StateModel> GetStates();
        List<CityModel> GetCitiesByState(int stateId);
        List<VillageModel> GetVillagesByCity(int cityId);

        FarmerViewModel AddFarmer(FarmerViewModel model, int staffId);

        List<FarmerViewModel> GetAllFarmers(int staffId, bool? isActive = null, string search = null);

        void ToggleFarmerStatus(int staffId, int farmerId, bool isActive);

        int UpdateFarmer(FarmerViewModel model, int staffId);

        FarmerViewModel GetFarmerById(int farmerId, int staffId);

        //Farmer Module

        //Login
        FarmerViewModel FarmerLogin(string farmerCode, string password);

        // Profile
        FarmerProfileModel GetFarmerProfile(int farmerId);

        //today milk entries

        List<MilkCollectionViewModel> GetTodayMilkEntries(int farmerId);

        //List<MilkCollectionViewModel> GetFarmerTodayMilkEntries(int farmerId);
        //all milk enties

        List<MilkCollectionViewModel> GetAllMilkEntries(int farmerId);
        //dashboard
        FarmerDashboardViewModel GetDashboard(int farmerId);


        // Self-registration (public page, no login)
        void SelfRegisterFarmer(SelfRegisterViewModel model);

        // Status check by phone (public page)
        FarmerStatusViewModel GetFarmerStatusByPhone(string phone);

        // Pending approvals list for a staff member's center
        List<PendingApprovalViewModel> GetPendingApprovals(int staffId);

        // Approve a pending farmer — returns FarmerCode + DefaultPassword
        ApprovalResultModel ApproveFarmer(int staffId, int farmerId);

        // Reject a pending farmer with a remark
        void RejectFarmer(int staffId, int farmerId, string remark);


        //milk rejection entries (history) for farmer
        List<FarmerRejectionViewModel> GetRejectionHistory(
        int farmerId, DateTime? fromDate = null, DateTime? toDate = null);
    }
}