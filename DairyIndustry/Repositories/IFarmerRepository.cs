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

        Task<RegByCenterModel> AddFarmerAsync(RegByCenterModel model, int staffId);
        Task<bool> IsEmailAlreadyRegisteredAsync(string email);
        Task<string> GenerateOtpAsync(string email);
        Task<bool> VerifyOtpAsync(string email, string otp);

        List<FarmerViewModel> GetAllFarmers(int staffId);

        void ToggleFarmerStatus(int staffId, int farmerId, bool isActive);
        List<CenterRejectedFarmerModel> GetRejectedFarmersByCenter(int staffId);
        Task<FarmerEditModel> GetFarmerByIdAsync(int farmerId, int staffId);
        Task<int> UpdateFarmerAsync(FarmerEditModel model, int staffId);
        Task<int> UpdateFarmerDocumentAsync(int staffId, int farmerId, string documentType, string filePath);
      
        //Login
        FarmerViewModel FarmerLogin(string farmerCode, string password);
        
        //Forgot Password
        (int FarmerId, string Email, string FarmerName)? GetFarmerEmailByCode(string farmerCode);
        // Resets password 
        bool ResetFarmerPassword(string farmerCode, string newPassword);
        
        // Profile
        FarmerProfileModel GetFarmerProfile(int farmerId);

        //today milk entries
        List<MilkCollectionModel> GetTodayMilkEntries(int farmerId);

        List<AllMilkHistoryModel> GetAllMilkEntriesFarmer(int farmerId);
        FarmerMilkReceiptModel GetReceiptByCollectionId(int collectionId);
        //dashboard
        FarmerDashboardViewModel GetDashboard(int farmerId);


        // Self-registration 
        Task<bool> IsPhoneAlreadyRegisteredAsync(string phone);
        Task<bool> IsAadhaarAlreadyRegisteredAsync(string aadhaar);
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

        string ChangePassword(int farmerId, string currentPassword, string newPassword);

        //farmer payment history
        List<FarmerPaymentHistoryModel> GetPaymentHistory(int farmerId);
    }
}