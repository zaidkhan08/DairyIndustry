using DairyIndustry.Filters;
using DairyIndustry.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace DairyIndustry.Controllers
{
    [ServiceFilter(typeof(ActionLogFilter))]
    public class LogisticsController : Controller
    {
        private readonly ILogisticsRepository _logisticRepo;
        public LogisticsController(ILogisticsRepository logisticsRepository)
        {
            _logisticRepo = logisticsRepository;
        }
        [SessionAuthorize("Driver")]
        public IActionResult Index()
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            int driverId = HttpContext.Session.GetInt32("DriverId") ?? 0;

            var driver = _logisticRepo.GetDriverByUserId(userId);
            var vehicles = _logisticRepo.GetVehiclesByDriverId(driverId);

            ViewBag.Vehicles = vehicles;

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


        [SessionAuthorize("Driver")]
        public IActionResult RegisterVehicle()
        {
            return View();
        }

        [SessionAuthorize("Driver")]
        [HttpPost]
        public IActionResult RegisterVehicle(string vehicleNumber, decimal capacity)
        {
            int driverId = HttpContext.Session.GetInt32("DriverId") ?? 0;

            if (driverId == 0)
            {
                TempData["Error"] = "Session expired. Please login again.";
                return RedirectToAction("Login", "Admin");
            }

            _logisticRepo.AddVehicle(driverId, vehicleNumber, capacity);

            TempData["Success"] = "Vehicle registered successfully. Waiting for admin approval.";
            return RedirectToAction("Index");
        }

        [SessionAuthorize("Admin")]
        public IActionResult AllDrivers()
        {
            var drivers = _logisticRepo.GetAllDrivers();
            return View(drivers);
        }

        [SessionAuthorize("Admin")]
        [HttpPost]
        public IActionResult UpdateDriverStatus(int driverId, string status)
        {
            _logisticRepo.UpdateDriverStatus(driverId, status);
            TempData["Success"] = $"Driver status updated to {status} successfully.";
            return RedirectToAction("AllDrivers");
        }

        [SessionAuthorize("Admin")]
        public IActionResult AllVehicles()
        {
            var vehicles = _logisticRepo.GetAllVehicles();
            return View(vehicles);
        }

        [SessionAuthorize("Admin")]
        [HttpPost]
        public IActionResult UpdateVehicleStatus(int vehicleId, string status)
        {
            _logisticRepo.UpdateVehicleStatus(vehicleId, status);
            TempData["Success"] = $"Vehicle status updated to {status} successfully.";
            return RedirectToAction("AllVehicles");
        }
        [SessionAuthorize("Driver")]
        public IActionResult MyTransfers()
        {
            int driverId = HttpContext.Session.GetInt32("DriverId") ?? 0;
            if (driverId == 0)
            {
                TempData["Error"] = "Session expired. Please login again.";
                return RedirectToAction("Login", "Admin");
            }
            var transfers = _logisticRepo.GetDriverTransfers(driverId);
            return View(transfers);
        }
    }
}
