using DairyIndustry.Models.Reports;

namespace DairyIndustry.Repositories
{
    public interface IReportRepository
    {
        List<DailySummaryModel> GetDailySummaryByCenter(int? centerId, DateTime? fromDate, DateTime? toDate);
        List<FarmerCollectionModel> GetCollectionByFarmer(int? centerId, DateTime? fromDate, DateTime? toDate);
        List<ProductionSummaryModel> GetPlantProductionSummary(int? plantId, DateTime? fromDate, DateTime? toDate);
        List<WastageModel> GetWastageSummary(int? plantId, DateTime? fromDate, DateTime? toDate);
        List<SalesReportModel> GetSalesReport(int? distributorId, DateTime? fromDate, DateTime? toDate);
    }
}
