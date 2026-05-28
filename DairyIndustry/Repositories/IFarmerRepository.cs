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

        //Registration of farmer will be done by Collection Center
        //  RegByCenterModel AddFarmer(RegByCenterModel model, int staffId);


        Task<RegByCenterModel> AddFarmerAsync(RegByCenterModel model, int staffId);
        Task<string> GenerateOtpAsync(string email);
        Task<bool> VerifyOtpAsync(string email, string otp);

        List<FarmerViewModel> GetAllFarmers(int staffId, bool? isActive = null, string search = null);

        void ToggleFarmerStatus(int staffId, int farmerId, bool isActive);

        List<CenterRejectedFarmerModel> GetRejectedFarmersByCenter(int staffId);

        // Update basic farmer details
        //int UpdateFarmer(FarmerViewModel model, int staffId);

        //// Update farmer documents
        //int UpdateFarmerDocuments(int staffId,int farmerId,string documentType,string filePath);
        //FarmerViewModel GetFarmerById(int farmerId, int staffId);

        // //FarmerEditModel GetFarmerById(int farmerId, int staffId);
        Task<FarmerEditModel> GetFarmerByIdAsync(int farmerId, int staffId);

        Task<int> UpdateFarmerAsync(FarmerEditModel model, int staffId);
        Task<int> UpdateFarmerDocumentAsync(int staffId, int farmerId, string documentType, string filePath);

        //int UpdateFarmer(FarmerEditModel model, int staffId);

        //int UpdateFarmerDocument(int staffId, int farmerId, string documentType, string filePath);

        //Farmer Module

        //Login
        FarmerViewModel FarmerLogin(string farmerCode, string password);

        // Profile
        FarmerProfileModel GetFarmerProfile(int farmerId);

        //today milk entries

        List<MilkCollectionModel> GetTodayMilkEntries(int farmerId);

        List<AllMilkHistoryModel> GetAllMilkEntriesFarmer(int farmerId);
        FarmerMilkReceiptModel GetReceiptByCollectionId(int collectionId);
        //dashboard
        FarmerDashboardViewModel GetDashboard(int farmerId);


        //// Self-registration (public page, no login)
        //void SelfRegisterFarmer(SelfRegisterViewModel model);
        Task SelfRegisterFarmerAsync(SelfRegisterViewModel model);

        // Status check by phone (public page)
        FarmerStatusViewModel GetFarmerStatusByPhone(string phone);

        // Pending approvals list for a staff member's center
        List<PendingApprovalModel> GetPendingApprovals(int staffId);

        // Approve a pending farmer — returns FarmerCode + DefaultPassword
        ApprovalResultModel ApproveFarmer(int staffId, int farmerId);

        // Reject a pending farmer with a remark
        void RejectFarmer(int staffId, int farmerId, string remark);


        //milk rejection entries (history) for farmer
        List<FarmerRejectionViewModel> GetRejectionHistory(
        int farmerId, DateTime? fromDate = null, DateTime? toDate = null);

        //string GenerateOtp(string email);
        //bool VerifyOtp(string email, string otp);
        string ChangePassword(int farmerId, string currentPassword, string newPassword);
    }
}