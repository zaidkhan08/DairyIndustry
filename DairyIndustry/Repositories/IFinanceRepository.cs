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
        bool FarmerHasBankAccount(int farmerId);
        void RecordPaymentTransaction(string paymentType, int paymentId,
                                      string bankStatus, string transactionReference);

        // ════════════════════════════════════════════════════════
        // CENTER PAYMENTS
        // ════════════════════════════════════════════════════════

        // Get eligible transfers: ReceivedDate IS NOT NULL, no existing CenterPayment
        List<TransferForPaymentModel> GetEligibleTransfers();

        // Look up rate from Finance.RateCharts via SP
        decimal GetActiveRate(int milkTypeId, decimal fat, decimal clr);

        // Create center payment record (status = Pending)
        int CreateCenterPayment(int transferId, decimal ratePerLiter, DateTime paymentDate);

        // List all center payments (for index page)
        List<CenterPaymentModel> GetAllCenterPayments();

        // Get single center payment by ID (for detail/pay page)
        CenterPaymentModel GetCenterPaymentById(int centerPaymentId);
    }
}