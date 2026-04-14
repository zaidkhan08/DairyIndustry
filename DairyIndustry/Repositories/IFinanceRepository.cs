using DairyIndustry.Models.Finance;
using DairyIndustry.Models.Admin;

namespace DairyIndustry.Repositories
{
    public interface IFinanceRepository
    {
        // ════════════════════════════════════════════════════════
        // DROPDOWNS
        // ════════════════════════════════════════════════════════
        List<FarmerDropdownModel> GetAllFarmers();
        List<CenterDropdownModel> GetAllCenters();

        // ════════════════════════════════════════════════════════
        // FARMER PAYMENTS
        // ════════════════════════════════════════════════════════

        FarmerPaymentPreviewModel PreviewFarmerPayment(int farmerId, int centerId,
                                                       DateTime fromDate, DateTime toDate);
        int CreateFarmerPayment(int centerId, int farmerId,
                                DateTime fromDate, DateTime toDate, DateTime paymentDate);
        List<FarmerPaymentModel> GetAllFarmerPayments();
        FarmerPaymentModel GetFarmerPaymentById(int paymentId);
        CenterDropdownModel GetFarmerDefaultCenter(int farmerId);
        void CancelCenterPayment(int centerPaymentId);
        bool FarmerHasBankAccount(int farmerId);
        void RecordPaymentTransaction(string paymentType, int paymentId,
                                      string bankStatus, string transactionReference);

        // ════════════════════════════════════════════════════════
        // CENTER PAYMENTS
        // ════════════════════════════════════════════════════════

        List<TransferForPaymentModel> GetEligibleTransfers(int? plantId = null);

        decimal GetActiveRate(int milkTypeId, decimal fat, decimal clr);

        int CreateCenterPayment(int transferId, decimal ratePerLiter, DateTime paymentDate);

        int ReactivateCenterPayment(int centerPaymentId, decimal ratePerLiter, DateTime paymentDate);

        List<CenterPaymentModel> GetAllCenterPayments(int? plantId = null);

        CenterPaymentModel GetCenterPaymentById(int centerPaymentId);
    }
}