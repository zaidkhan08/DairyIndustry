using DairyIndustry.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace DairyIndustry.Controllers
{
    public class LogisticsController : Controller
    {
        private readonly ILogisticsRepository _logisticRepo;
        public LogisticsController(ILogisticsRepository logisticsRepository) 
        {
            _logisticRepo = logisticsRepository;
        }
        public ActionResult RegisterDriver()
        { 
            return View();
        }
        [HttpPost]
        public ActionResult RegisterDriver(string DriverName, string LicenceNo, string Phone)
        {
            _logisticRepo.AddDriver(DriverName,LicenceNo,Phone);
            return RedirectToAction("Index");
        }
        public IActionResult Index()
        {
            return View();
        }
    }
}
