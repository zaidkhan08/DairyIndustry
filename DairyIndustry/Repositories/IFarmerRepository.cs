using DairyIndustry.Models.Admin;
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




        //farmer portal

        //FarmerProfileModel GetFarmerProfile(int userId);

    }
}