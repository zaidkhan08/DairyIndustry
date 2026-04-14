using DairyIndustry.Models.Reports;

namespace DairyIndustry.Repositories
{
    public interface IReportRepository
    {
        // kept
        List<DailySummaryModel> GetDailySummaryByCenter(int? centerId, DateTime? fromDate, DateTime? toDate);
        List<FarmerCollectionModel> GetCollectionByFarmer(int? centerId, DateTime? fromDate, DateTime? toDate);

        // admin reports
        List<DashboardMetricModel> GetDashboardOverview();
        List<SalesReportModel> GetSalesReport(int? distributorId, DateTime? fromDate, DateTime? toDate);
        List<ProductionSummaryModel> GetPlantProductionSummary(int? plantId, DateTime? fromDate, DateTime? toDate);
        List<WastageModel> GetWastageSummary(int? plantId, DateTime? fromDate, DateTime? toDate);
        List<MilkTransferReportModel> GetMilkTransfers(int? plantId, DateTime? fromDate, DateTime? toDate);
        List<StaffPaymentReportModel> GetStaffPayments(DateTime? fromDate, DateTime? toDate);
        List<CenterPaymentReportModel> GetCenterPayments(DateTime? fromDate, DateTime? toDate);
        List<PendingPaymentModel> GetPendingPayments();
    }
}