using DairyIndustry.Filters;
using DairyIndustry.Models.Reports;
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
            DateTime? fromDate,
            DateTime? toDate,
            int? plantId,
            int? distributorId,
            string? activeTab)
        {
            var model = new AdminReportViewModel
            {
                FromDate = fromDate ?? DateTime.Today.AddDays(-30),
                ToDate = toDate ?? DateTime.Today,
                PlantId = plantId,
                DistributorId = distributorId,
                ActiveTab = activeTab ?? "dashboard"
            };

            model.Dashboard = _reportRepo.GetDashboardOverview();
            model.Sales = _reportRepo.GetSalesReport(distributorId, model.FromDate, model.ToDate);
            model.Production = _reportRepo.GetPlantProductionSummary(plantId, model.FromDate, model.ToDate);
            model.Wastage = _reportRepo.GetWastageSummary(plantId, model.FromDate, model.ToDate);
            model.Transfers = _reportRepo.GetMilkTransfers(plantId, model.FromDate, model.ToDate);
            model.StaffPayments = _reportRepo.GetStaffPayments(model.FromDate, model.ToDate);
            model.CenterPayments = _reportRepo.GetCenterPayments(model.FromDate, model.ToDate);
            model.PendingPayments = _reportRepo.GetPendingPayments();

            return View(model);
        }
    }
}