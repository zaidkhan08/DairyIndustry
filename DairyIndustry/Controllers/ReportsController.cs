using DairyIndustry.Filters;
using DairyIndustry.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace DairyIndustry.Controllers
{
    [ServiceFilter(typeof(ActionLogFilter))]
    public class ReportsController : Controller
    {
        private readonly IReportRepository _reportRepo;

        public ReportsController(IReportRepository reportRepo)
        {
            _reportRepo = reportRepo;
        }

        [SessionAuthorize("Admin")]
        public IActionResult Index(
            DateTime? fromDate, DateTime? toDate,
            int? centerId, int? plantId, int? distributorId)
        {
            fromDate ??= DateTime.Today.AddDays(-30);
            toDate ??= DateTime.Today;

            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");

            ViewBag.DailySummary = _reportRepo.GetDailySummaryByCenter(centerId, fromDate, toDate);
            ViewBag.FarmerReport = _reportRepo.GetCollectionByFarmer(centerId, fromDate, toDate);
            ViewBag.Production = _reportRepo.GetPlantProductionSummary(plantId, fromDate, toDate);
            ViewBag.Wastage = _reportRepo.GetWastageSummary(plantId, fromDate, toDate);
            ViewBag.Sales = _reportRepo.GetSalesReport(distributorId, fromDate, toDate);

            return View();
        }
    }
}
