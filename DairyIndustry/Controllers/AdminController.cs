using DairyIndustry.Models.Admin;
using DairyIndustry.Repositories;
using Microsoft.AspNetCore.Mvc;
namespace DairyIndustry.Controllers
{
    public class AdminController : Controller
    {
        private readonly IAdminRepository _adminRepo;

        public AdminController(IAdminRepository adminRepo)
        {
            _adminRepo = adminRepo;
        }

        // ════════════════════════════════════════════════════════
        // DASHBOARD
        // ════════════════════════════════════════════════════════

        public IActionResult Index()
        {
            var users = _adminRepo.GetAllUsers();
            return View(users);
        }

        // ════════════════════════════════════════════════════════
        // ROLES
        // ════════════════════════════════════════════════════════

        public IActionResult Roles()
        {
            var roles = _adminRepo.GetAllRoles();
            return View(roles);
        }

        [HttpPost]
        public IActionResult CreateRole(string roleName)
        {
            _adminRepo.CreateRole(roleName);
            return RedirectToAction("Roles");
        }

        // ════════════════════════════════════════════════════════
        // USERS
        // ════════════════════════════════════════════════════════

        public IActionResult Users()
        {
            var users = _adminRepo.GetAllUsers();
            return View(users);
        }

        public IActionResult RegisterUser()
        {
            ViewBag.Roles = _adminRepo.GetAllRoles();
            return View();
        }

        [HttpPost]
        public IActionResult RegisterUser(string username, string password, int roleId, int? staffId)
        {
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            _adminRepo.RegisterUser(username, passwordHash, roleId, staffId);
            return RedirectToAction("Users");
        }

        [HttpPost]
        public IActionResult UpdateUserStatus(int userId, bool isActive)
        {
            _adminRepo.UpdateUserStatus(userId, isActive);
            return RedirectToAction("Users");
        }

        // ════════════════════════════════════════════════════════
        // LOGIN
        // ════════════════════════════════════════════════════════

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            var user = _adminRepo.GetUserByUsername(username);

            if (user == null)
            {
                ViewBag.Error = "Invalid username or password.";
                return View();
            }

            if (!user.IsActive)
            {
                ViewBag.Error = "Your account is inactive. Contact admin.";
                return View();
            }

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

            if (!isPasswordValid)
            {
                ViewBag.Error = "Invalid username or password.";
                return View();
            }

            return RedirectToAction("Index");
        }

        // ════════════════════════════════════════════════════════
        // AUDIT LOGS
        // ════════════════════════════════════════════════════════

        public IActionResult AuditLogs(int? userId, string? entityName, DateTime? fromDate, DateTime? toDate)
        {
            var logs = _adminRepo.GetAuditLogs(userId, entityName, fromDate, toDate);
            return View(logs);
        }

        // ════════════════════════════════════════════════════════
        // LOCATION — STATE
        // ════════════════════════════════════════════════════════

        public IActionResult States()
        {
            var states = _adminRepo.GetAllStates();
            return View(states);
        }

        [HttpPost]
        public IActionResult AddState(string stateName)
        {
            _adminRepo.AddState(stateName);
            return RedirectToAction("States");
        }

        // ════════════════════════════════════════════════════════
        // LOCATION — CITY
        // ════════════════════════════════════════════════════════

        public IActionResult Cities()
        {
            var cities = _adminRepo.GetAllCities();
            ViewBag.States = _adminRepo.GetAllStates();
            return View(cities);
        }

        [HttpPost]
        public IActionResult AddCity(string cityName, int stateId)
        {
            _adminRepo.AddCity(cityName, stateId);
            return RedirectToAction("Cities");
        }

        // Cascading dropdown — called via AJAX from Village page
        public IActionResult GetCitiesByState(int stateId)
        {
            var cities = _adminRepo.GetCitiesByState(stateId);
            return Json(cities);
        }

        // ════════════════════════════════════════════════════════
        // LOCATION — VILLAGE
        // ════════════════════════════════════════════════════════

        public IActionResult Villages()
        {
            var villages = _adminRepo.GetAllVillages();
            ViewBag.States = _adminRepo.GetAllStates();
            return View(villages);
        }

        [HttpPost]
        public IActionResult AddVillage(string villageName, int cityId)
        {
            _adminRepo.AddVillage(villageName, cityId);
            return RedirectToAction("Villages");
        }

        // Cascading dropdown — called via AJAX from Village page
        public IActionResult GetVillagesByCity(int cityId)
        {
            var villages = _adminRepo.GetVillagesByCity(cityId);
            return Json(villages);
        }

        // ════════════════════════════════════════════════════════
        // MILK TYPES
        // ════════════════════════════════════════════════════════

        public IActionResult MilkTypes()
        {
            var milkTypes = _adminRepo.GetAllMilkTypes();
            return View(milkTypes);
        }

        [HttpPost]
        public IActionResult AddMilkType(string milkTypeName)
        {
            _adminRepo.AddMilkType(milkTypeName);
            return RedirectToAction("MilkTypes");
        }

        // ════════════════════════════════════════════════════════
        // RATE CHART
        // ════════════════════════════════════════════════════════

        public IActionResult RateChart()
        {
            var rateCharts = _adminRepo.GetAllRateCharts();
            ViewBag.MilkTypes = _adminRepo.GetAllMilkTypes();
            return View(rateCharts);
        }

        [HttpPost]
        public IActionResult AddRateChart(int milkTypeId, decimal fatFrom, decimal fatTo,
                                          decimal clrFrom, decimal clrTo,
                                          decimal ratePerLiter, DateTime effectiveFrom)
        {
            _adminRepo.AddRateChart(milkTypeId, fatFrom, fatTo, clrFrom, clrTo, ratePerLiter, effectiveFrom);
            return RedirectToAction("RateChart");
        }

    }

}
