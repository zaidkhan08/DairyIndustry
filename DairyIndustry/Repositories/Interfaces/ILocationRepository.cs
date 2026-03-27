using DairyIndustry.Models.Location;

namespace DairyIndustry.Repositories.Interfaces
{
    public interface ILocationRepository
    {
        List<State> GetAllStates();
        List<City> GetCitiesByState(int stateId);
        List<Village> GetVillagesByCity(int cityId);
    }
}
