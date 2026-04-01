using DairyIndustry.Filters;
using DairyIndustry.Models.Admin;
using DairyIndustry.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace DairyIndustry.Controllers
{
    [ServiceFilter(typeof(ActionLogFilter))]
    public class AdminController : Controller
    {
        private readonly IAdminRepository _adminRepo;
        private readonly ILogisticsRepository _logisticsRepo;


        public AdminController(IAdminRepository adminRepo, ILogisticsRepository logisticsRepo)
        {
            _adminRepo = adminRepo;
            _logisticsRepo = logisticsRepo;
        }

        // ════════════════════════════════════════════════════════
        // LOGIN — NO [SessionAuthorize] here
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

            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("RoleName", user.RoleName);

            switch (user.RoleName)
            {
                case "Admin":
                    return RedirectToAction("Index", "Admin");

                case "Driver":
                    var driver = _logisticsRepo.GetDriverByUserId(user.UserId);
                    if (driver != null)
                        HttpContext.Session.SetInt32("DriverId", driver.DriverId);
                    return RedirectToAction("Index", "Logistics");

                //case "Collection Agent":
                //    return RedirectToAction("Index", "Collection");

                //case "Production Manager":
                //    return RedirectToAction("Index", "Production");

                //case "Finance Manager":
                //    return RedirectToAction("Index", "Finance");

                //case "Sales Manager":
                //    return RedirectToAction("Index", "Sales");

                //case "HR Manager":
                //    return RedirectToAction("Index", "HR");

                default:
                    return RedirectToAction("Index", "Admin");
            }
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // ════════════════════════════════════════════════════════
        // DASHBOARD
        // ════════════════════════════════════════════════════════

        [SessionAuthorize("Admin")]
        public IActionResult Index()
        {
            var users = _adminRepo.GetAllUsers();
            return View(users);
        }

        // ════════════════════════════════════════════════════════
        // ROLES
        // ════════════════════════════════════════════════════════

        [SessionAuthorize("Admin")]
        public IActionResult Roles()
        {
            var roles = _adminRepo.GetAllRoles();
            return View(roles);
        }

        [SessionAuthorize("Admin")]
        [HttpPost]
        public IActionResult CreateRole(string roleName)
        {
            _adminRepo.CreateRole(roleName);
            return RedirectToAction("Roles");
        }

        // ════════════════════════════════════════════════════════
        // USERS
        // ════════════════════════════════════════════════════════

        [SessionAuthorize("Admin")]
        public IActionResult Users()
        {
            var users = _adminRepo.GetAllUsers();
            return View(users);
        }

        [SessionAuthorize("Admin")]
        [HttpGet]
        public IActionResult RegisterUser()
        {
            ViewBag.StaffList = _adminRepo.GetUnlinkedStaff();
            return View();
        }

        [SessionAuthorize("Admin")]
        [HttpPost]
        public IActionResult RegisterUser(string username, string password, int staffId)
        {
            var staff = _adminRepo.GetStaffById(staffId);

            if (staff == null)
            {
                ViewBag.Error = "Selected staff not found.";
                ViewBag.StaffList = _adminRepo.GetUnlinkedStaff();
                return View();
            }

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            _adminRepo.RegisterUser(username, passwordHash, staff.RoleId, staffId);

            TempData["Success"] = $"User account created for {staff.FullName}.";
            return RedirectToAction("Users");
        }

        [SessionAuthorize("Admin")]
        [HttpPost]
        public IActionResult UpdateUserStatus(int userId, bool isActive)
        {
            _adminRepo.UpdateUserStatus(userId, isActive);
            return RedirectToAction("Users");
        }

        // ════════════════════════════════════════════════════════
        // AUDIT LOGS
        // ════════════════════════════════════════════════════════

        [SessionAuthorize("Admin")]
        public IActionResult AuditLogs(int? userId, string? entityName, DateTime? fromDate, DateTime? toDate)
        {
            var logs = _adminRepo.GetAuditLogs(userId, entityName, fromDate, toDate);
            return View(logs);
        }

        [SessionAuthorize("Admin")]
        public IActionResult Location()
        {
            ViewBag.States = _adminRepo.GetAllStates();
            ViewBag.Cities = _adminRepo.GetAllCities();
            ViewBag.Villages = _adminRepo.GetAllVillages();
            return View();
        }

        [SessionAuthorize("Admin")]
        [HttpPost]
        public IActionResult AddState(string stateName)
        {
            _adminRepo.AddState(stateName);
            TempData["Success"] = $"State '{stateName}' added successfully.";
            TempData["ActiveTab"] = "state";
            return RedirectToAction("Location");
        }

        [SessionAuthorize("Admin")]
        [HttpPost]
        public IActionResult AddCity(string cityName, int stateId)
        {
            _adminRepo.AddCity(cityName, stateId);
            TempData["Success"] = $"City '{cityName}' added successfully.";
            TempData["ActiveTab"] = "city";
            return RedirectToAction("Location");
        }

        [SessionAuthorize("Admin")]
        [HttpPost]
        public IActionResult AddVillage(string villageName, int cityId)
        {
            _adminRepo.AddVillage(villageName, cityId);
            TempData["Success"] = $"Village '{villageName}' added successfully.";
            TempData["ActiveTab"] = "village";
            return RedirectToAction("Location");
        }

        [SessionAuthorize("Admin")]
        public IActionResult GetCitiesByState(int stateId)
        {
            var cities = _adminRepo.GetCitiesByState(stateId);
            return Json(cities);
        }

        [SessionAuthorize("Admin")]
        public IActionResult GetVillagesByCity(int cityId)
        {
            var villages = _adminRepo.GetVillagesByCity(cityId);
            return Json(villages);
        }

        // ════════════════════════════════════════════════════════
        // MILK TYPES
        // ════════════════════════════════════════════════════════

        [SessionAuthorize("Admin")]
        public IActionResult MilkTypes()
        {
            var milkTypes = _adminRepo.GetAllMilkTypes();
            return View(milkTypes);
        }

        [SessionAuthorize("Admin")]
        [HttpPost]
        public IActionResult AddMilkType(string milkTypeName)
        {
            _adminRepo.AddMilkType(milkTypeName);
            return RedirectToAction("MilkTypes");
        }

        // ════════════════════════════════════════════════════════
        // RATE CHART
        // ════════════════════════════════════════════════════════

        [SessionAuthorize("Admin")]
        public IActionResult RateChart()
        {
            var rateCharts = _adminRepo.GetAllRateCharts();
            ViewBag.MilkTypes = _adminRepo.GetAllMilkTypes();
            return View(rateCharts);
        }

        [SessionAuthorize("Admin")]
        [HttpPost]
        public IActionResult AddRateChart(int milkTypeId, decimal fatFrom, decimal fatTo,
                                          decimal clrFrom, decimal clrTo,
                                          decimal ratePerLiter, DateTime effectiveFrom)
        {
            _adminRepo.AddRateChart(milkTypeId, fatFrom, fatTo, clrFrom, clrTo, ratePerLiter, effectiveFrom);
            return RedirectToAction("RateChart");
        }

        // ════════════════════════════════════════════════════════
        // STAFF
        // ════════════════════════════════════════════════════════

        [SessionAuthorize("Admin")]
        public IActionResult Staff()
        {
            var staffList = _adminRepo.GetAllStaff();
            return View(staffList);
        }

        [SessionAuthorize("Admin")]
        [HttpGet]
        public IActionResult AddStaff()
        {
            var Roles = _adminRepo.GetAllRoles();
            return View(Roles);
        }

        [SessionAuthorize("Admin")]
        [HttpPost]
        public IActionResult AddStaff(string firstName, string lastName,
                              string phone, string email,
                              int roleId, DateTime? doj,
                              string bankName, string accountNumber,
                              string ifscCode, IFormFile profilePhoto)
        {
            string photoPath = null;

            if (profilePhoto != null && profilePhoto.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var extension = Path.GetExtension(profilePhoto.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                {
                    ViewBag.Error = "Only .jpg, .jpeg and .png files are allowed.";
                    return View(_adminRepo.GetAllRoles());
                }

                if (profilePhoto.Length > 2 * 1024 * 1024)
                {
                    ViewBag.Error = "Photo size must be less than 2MB.";
                    return View(_adminRepo.GetAllRoles());
                }

                string uploadsFolder = Path.Combine(
                    Directory.GetCurrentDirectory(), "wwwroot", "uploads", "staff"
                );

                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = $"staff_{DateTime.Now:yyyyMMddHHmmss}_{Path.GetFileName(profilePhoto.FileName)}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    profilePhoto.CopyTo(stream);   // ← no await, no async
                }

                photoPath = $"/uploads/staff/{uniqueFileName}";
            }

            _adminRepo.AddStaff(firstName, lastName, phone, email,
                                roleId, doj, bankName, accountNumber,
                                ifscCode, photoPath);

            TempData["Success"] = "Staff member added successfully.";
            return RedirectToAction("Staff");
        }


        [SessionAuthorize("Admin")]
        [HttpPost]
        public IActionResult ToggleStaffActive(int staffId, bool isActive)
        {
            _adminRepo.ToggleStaffActive(staffId, isActive);
            return RedirectToAction("Staff");
        }

        // ════════════════════════════════════════════════════════
        // PLANT
        // ════════════════════════════════════════════════════════

        [SessionAuthorize("Admin")]
        [HttpGet]
        public ActionResult AddPlant()
        {
            return View();
        }

        [SessionAuthorize("Admin")]
        [HttpPost]
        public ActionResult AddPlant(string PlantName, string Location)
        {
            _adminRepo.AddPlant(PlantName, Location);
            return RedirectToAction("GetAllPlants");
        }

        [SessionAuthorize("Admin")]
        [HttpGet]
        public ActionResult GetAllPlants()
        {
            var plants = _adminRepo.GetAllPlants(isActive: null);
            return View(plants);
        }

        [SessionAuthorize("Admin")]
        [HttpPost]
        public ActionResult DeletePlant(int id)
        {
            _adminRepo.TogglePlant(id, isActive: false);
            return RedirectToAction("GetAllPlants");
        }

        [SessionAuthorize("Admin")]
        [HttpPost]
        public ActionResult RestorePlant(int id)
        {
            _adminRepo.TogglePlant(id, isActive: true);
            return RedirectToAction("GetAllPlants");
        }

        [SessionAuthorize("Admin")]
        [HttpGet]
        public ActionResult EditPlant(int id)
        {
            var plant = _adminRepo.getPlantById(id);
            if (plant == null)
                return NotFound();
            return View(plant);
        }

        [SessionAuthorize("Admin")]
        [HttpPost]
        public ActionResult UpdatePlant(PlantModel plant)
        {
            if (ModelState.IsValid)
            {
                _adminRepo.UpdatePlant(plant);
                return RedirectToAction("GetAllPlants");
            }
            return View("EditPlant", plant);
        }

        // ════════════════════════════════════════════════════════
        // Production
        // ════════════════════════════════════════════════════════
        
        [SessionAuthorize("Admin")]
        public IActionResult Products(string productType = null, bool? isActive = null)
        {
            var products = _adminRepo.GetAllProducts(productType, isActive);
            ViewBag.ProductTypes = _adminRepo.GetProductTypes();
            ViewBag.CurrentType = productType;
            ViewBag.CurrentActive = isActive;   // ← must be bool? not string
            return View(products);
        }

        [SessionAuthorize("Admin")]
        public IActionResult ProductDetail(int id)
        {
            var product = _adminRepo.GetProductById(id);
            if (product == null)
            {
                TempData["Error"] = "Product not found.";
                return RedirectToAction("Products");
            }
            return View(product);
        }

        [SessionAuthorize("Admin")]
        [HttpGet]
        public IActionResult AddProduct()
        {
            return View();
        }

        [SessionAuthorize("Admin")]
        [HttpPost]
        public IActionResult AddProduct(string productName, string productType,
                                        decimal mrp, string unit,
                                        int? shelfLifeDays, string description)
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;

            try
            {
                _adminRepo.AddProduct(productName, productType, mrp,
                                           unit, shelfLifeDays, description, userId);

                TempData["Success"] = $"{productName} added successfully.";
                return RedirectToAction("Products");
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View();
            }
        }

        [SessionAuthorize("Admin")]
        [HttpGet]
        public IActionResult EditProduct(int id)
        {
            var product = _adminRepo.GetProductById(id);
            if (product == null)
            {
                TempData["Error"] = "Product not found.";
                return RedirectToAction("Products");
            }
            return View(product);
        }

        [SessionAuthorize("Admin")]
        [HttpPost]
        public IActionResult EditProduct(int productId, string productName,
                                         string productType, decimal mrp,
                                         string unit, int? shelfLifeDays,
                                         string description)
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;

            try
            {
                _adminRepo.UpdateProduct(productId, productName, productType,
                                              mrp, unit, shelfLifeDays, description, userId);

                TempData["Success"] = $"{productName} updated successfully.";
                return RedirectToAction("Products");
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                var product = _adminRepo.GetProductById(productId);
                return View(product);
            }
        }

        [SessionAuthorize("Admin")]
        [HttpPost]
        public IActionResult ToggleProductStatus(int productId, bool isActive)
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            _adminRepo.ToggleProductStatus(productId, isActive, userId);

            TempData["Success"] = isActive
                ? "Product activated."
                : "Product deactivated.";

            return RedirectToAction("Products");
        }


    }
}