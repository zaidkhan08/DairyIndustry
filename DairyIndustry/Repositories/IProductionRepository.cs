using DairyIndustry.Models.Logistics;
using DairyIndustry.Models.Production;

namespace DairyIndustry.Repositories
{
    public interface IProductionRepository
    {
        // ── Dropdowns ──────────────────────────────────────────
        List<BatchDropdownModel> GetClosedBatches();   // for Create form
        List<VehiclesModel> GetAllVehicles();//to get vechical

        // ── Transfers ──────────────────────────────────────────
        int DispatchMilkTransfer(int batchId, int vehicleId, int plantId,
                                  decimal dispatchQty, DateTime dispatchDate);

        void ReceiveMilkTransfer(int transferId, decimal receivedQty, DateTime receivedDate);

        List<MilkTransferModel> GetAllTransfers();

        MilkTransferModel GetTransferById(int transferId);
    }
}