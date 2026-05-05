using DairyIndustry.Models.Production;

namespace DairyIndustry.Repositories
{
    public interface IPlantRepository
    {
        //  Called by AdminController Login
        int GetPlantIdByStaffId(int? staffId);

        //  Called by PlantController Dashboard
        PlantStaffModel GetPlantByStaffId(int staffId);

        List<PlantTransferViewModel> GetTransfersByPlant(int plantId);
        int ReceiveMilkTransfer(int transferId, decimal receivedQty, DateTime receivedDate);
        PlantTransferViewModel GetTransferById(int transferId);

    }
}
