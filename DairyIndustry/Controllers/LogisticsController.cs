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

        public ActionResult Index()
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            int driverId = HttpContext.Session.GetInt32("DriverId") ?? 0;

            var driver = _logisticRepo.GetDriverByUserId(userId);
            var vehicle = _logisticRepo.GetVehicleByDriverId(driverId);

            ViewBag.Vehicle = vehicle;
            return View(driver);
        }

        [HttpGet]
        public ActionResult RegisterDriver()
        {
            return View();
        }
        [HttpPost]
        public IActionResult RegisterDriver(string driverName, string licenseNo,
                               string phone, string username, string password)
        {
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            _logisticRepo.RegisterDriver(driverName, licenseNo, phone, username, passwordHash);
            ViewBag.Success = "Registration submitted. Please wait for admin approval.";
            return View();
        }
    }
}
