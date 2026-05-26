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
        List<FarmerDropdownModel> GetFarmersByCenter(int centerId);
        void CancelFarmerPayment(int paymentId);
        void ReactivateFarmerPayment(int paymentId);
        FarmerDropdownModel GetFarmerByCode(string farmerCode);

        // ════════════════════════════════════════════════════════
        // FARMER PAYMENTS
        // ════════════════════════════════════════════════════════
        FarmerPaymentPreviewModel PreviewFarmerPayment(int farmerId, int centerId,
                                                       DateTime fromDate, DateTime toDate);
        int CreateFarmerPayment(int centerId, int farmerId,
                                DateTime fromDate, DateTime toDate, DateTime paymentDate,
                                int paidByUserId);
        List<FarmerPaymentModel> GetAllFarmerPayments(int? centerId = null);

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

        // ════════════════════════════════════════════════════════
        // CENTER WALLET
        // centerId — from session (Collection Agent) or passed explicitly (Admin)
        // If centerId is null → returns wallet for ALL centers (Admin overview)
        // ════════════════════════════════════════════════════════
        CenterWalletViewModel GetCenterWallet(int? centerId = null);

        // ── Bonus breakdown from Finance.CenterWallet table ─────
        /// <summary>
        /// Returns wallet entries from Finance.CenterWallet showing base vs bonus
        /// breakdown per center payment.
        /// Pass centerId to scope to one center (Collection Agent).
        /// Pass null to get all centers (Admin).
        /// </summary>
        List<CenterWalletEntry> GetCenterWalletEntries(int? centerId = null);

        // ════════════════════════════════════════════════════════
        // PLANT STAFF LIST
        // plantId — from session (Plant Manager) or passed explicitly (Admin)
        // If plantId is null → returns staff for ALL plants (Admin overview)
        // ════════════════════════════════════════════════════════
        /// <summary>
        /// Returns staff working at centers that supply the given plant.
        /// Plant Manager sees only their own plant's staff (plantId from session).
        /// Admin passes null to see all staff across all plants.
        /// </summary>
        PlantStaffViewModel GetPlantStaff(int? plantId = null);
    }
}