using DairyIndustry.Models.Collection;
using DairyIndustry.Models.FarmerModel;

namespace DairyIndustry.Repositories.Interfaces
{
    public interface ICollectionCenterRepository
    {

        //Details
        CollectionCenter GetCollectionCenterById(int  id);

        bool AddFarmer(Farmer farmer, int value);
        List<Farmer> GetFarmersByCenterStaff(int centerId);
      //  bool DeleteFarmerById(int id);

    }
}
