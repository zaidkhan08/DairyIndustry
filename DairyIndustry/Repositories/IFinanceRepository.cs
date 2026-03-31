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

        // Preview — calculate totals before creating payment
        FarmerPaymentPreviewModel PreviewFarmerPayment(int farmerId, int centerId,
                                                       DateTime fromDate, DateTime toDate);

        // Create payment record (status = Pending) using SP 8.4
        int CreateFarmerPayment(int centerId, int farmerId,
                                DateTime fromDate, DateTime toDate, DateTime paymentDate);

        // List all farmer payments
        List<FarmerPaymentModel> GetAllFarmerPayments();

        // Get single payment by ID
        FarmerPaymentModel GetFarmerPaymentById(int paymentId);
        CenterDropdownModel GetFarmerDefaultCenter(int farmerId);

        // Check if farmer has bank account linked
        bool FarmerHasBankAccount(int farmerId);

        // Record Stripe transaction + update status using SP 8.7
        void RecordPaymentTransaction(string paymentType, int paymentId,
                                      string bankStatus, string transactionReference);
    }
}