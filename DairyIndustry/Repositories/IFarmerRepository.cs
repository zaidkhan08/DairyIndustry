using DairyIndustry.Models.Admin;
using DairyIndustry.Models.FarmerModel;

namespace DairyIndustry.Repositories
{
    public interface IFarmerRepository
    {
       
        List<StateModel> GetStates();
        List<CityModel> GetCitiesByState(int stateId);
        List<VillageModel> GetVillagesByCity(int cityId);

        void AddFarmer(FarmerViewModel model, int staffId);
       
    }
}