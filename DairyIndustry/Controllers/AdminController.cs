using DairyIndustry.Filters;
using DairyIndustry.Models.Admin;
using DairyIndustry.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;

namespace DairyIndustry.Controllers
{
    [ServiceFilter(typeof(ActionLogFilter))]
    public class AdminController : Controller
    {
        private readonly IAdminRepository _adminRepo;
        private readonly ILogisticsRepository _logisticsRepo;
        private readonly ICollectionCenterRepository _centerRepository;
        private readonly IReportRepository _reportRepo;


        public AdminController(IAdminRepository adminRepo, ILogisticsRepository logisticsRepo, ICollectionCenterRepository centerRepository,IReportRepository reportRepo)
        {
            _adminRepo = adminRepo; 
            _logisticsRepo=logisticsRepo;
            _centerRepository = centerRepository;
            _reportRepo = reportRepo;
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
            HttpContext.Session.SetInt32("StaffId", user.StaffId??0);

            switch (user.RoleName)
            {
                case "Admin":
                    return RedirectToAction("Index", "Admin");

                case "Driver":
                    var driver = _logisticsRepo.GetDriverByUserId(user.UserId);
                    if (driver != null)
                        HttpContext.Session.SetInt32("DriverId", driver.DriverId);
                    return RedirectToAction("Index", "Logistics");

                case "Collection Agent":
                    var centerId = _centerRepository.GetCenterIdByStaffId(user.StaffId ?? 0);
                    HttpContext.Session.SetInt32("CenterId", centerId);
                    return RedirectToAction("Dashboard", "CollectionCenter");

                //case "Production Manager":
                //    // GetPlantIdByStaffId → returns int directly 
                //    var plantInfo = _plantRepository.GetPlantByStaffId(user.StaffId ?? 0);

                //    if (plantInfo.PlantId == 0)
                //    {
                //        ViewBag.Error = "You are not assigned to any plant.";
                //        return View();
                //    }
                //Added By Zaid
                case "Plant Manager":

                    var plantId = _adminRepo.GetPlantIdByUser(user.UserId);
                    if (plantId.HasValue)
                    {
                        HttpContext.Session.SetInt32("PlantId", plantId.Value);
                        var plant = _adminRepo.getPlantById(plantId.Value);
                        if (plant != null)
                            HttpContext.Session.SetString("PlantName", plant.PlantName);
                    }
                    return RedirectToAction("Index", "Production");

                //case "Collection Agent":
                //    return RedirectToAction("Index", "Production");

                //    HttpContext.Session.SetInt32("PlantId", plantInfo.PlantId);
                //    HttpContext.Session.SetString("PlantName", plantInfo.PlantName ?? "");
                //    HttpContext.Session.SetString("Name", plantInfo.StaffName ?? "");
                //    return RedirectToAction("Index", "Plant");

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

        //[SessionAuthorize("Admin")]
        public IActionResult Index()
        {
            var vm = new AdminDashboardViewModel
            {
                Users = _adminRepo.GetAllUsers(),
                Staff = _adminRepo.GetAllStaff(),
                Plants = _adminRepo.GetAllPlants(isActive: null),
                Centers = _adminRepo.GetAllCollection(isActive: null),
                Products = _adminRepo.GetAllProducts(isActive: null),
                Batches = _adminRepo.GetProductionBatches(),
                Transfers = _adminRepo.GetMilkTransfers()
            };
            return View(vm);
        }
        // ════════════════════════════════════════════════════════
        // ROLES
        // ════════════════════════════════════════════════════════

        //[SessionAuthorize("Admin")]
        public IActionResult Roles()
        {
            var roles = _adminRepo.GetAllRoles();
            return View(roles);
        }

        //[SessionAuthorize("Admin")]
        [HttpPost]
        public IActionResult CreateRole(string roleName)
        {
            _adminRepo.CreateRole(roleName);
            return RedirectToAction("Roles");
        }

        // ════════════════════════════════════════════════════════
        // USERS
        // ════════════════════════════════════════════════════════

        //[SessionAuthorize("Admin")]
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

        //[SessionAuthorize("Admin")]
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

        //[SessionAuthorize("Admin")]
        [HttpPost]
        public IActionResult UpdateUserStatus(int userId, bool isActive)
        {
            _adminRepo.UpdateUserStatus(userId, isActive);
            return RedirectToAction("Users");
        }

        // ════════════════════════════════════════════════════════
        // AUDIT LOGS
        // ════════════════════════════════════════════════════════

        ////[SessionAuthorize("Admin")]
        //public IActionResult AuditLogs(int? userId, string? entityName, DateTime? fromDate, DateTime? toDate)
        //[SessionAuthorize("Admin")]
        //[HttpGet]
        //public IActionResult AssignUserToPlant()
        //{
        //    ViewBag.Users = _adminRepo.GetAllUsers();
        //    ViewBag.Plants = _adminRepo.GetAllPlants();
        //    ViewBag.Assignments = _adminRepo.GetAllUserPlantAssignments();
        //    return View();
        //}

        //[SessionAuthorize("Admin")]
        //[HttpPost]
        //public IActionResult AssignUserToPlant(int userId, int plantId)
        //{
        //    if (userId == 0 || plantId == 0)
        //    {
        //        ViewBag.Error = "Please select both user and plant.";
        //        ViewBag.Users = _adminRepo.GetAllUsers();
        //        ViewBag.Plants = _adminRepo.GetAllPlants();
        //        ViewBag.Assignments = _adminRepo.GetAllUserPlantAssignments();
        //        return View();
        //    }
        //    _adminRepo.AssignUserToPlant(userId, plantId);
        //    TempData["Success"] = "User assigned to plant successfully.";
        //    return RedirectToAction("AssignUserToPlant");
        //}

        //[SessionAuthorize("Admin")]
        //[HttpGet]
        //public IActionResult AssignUserToCenter()
        //{
        //    ViewBag.Users = _adminRepo.GetAllUsers();
        //    ViewBag.Centers = _adminRepo.GetAllCenters();
        //    ViewBag.Assignments = _adminRepo.GetAllUserCenterAssignments();
        //    return View();
        //}

        ////[SessionAuthorize("Admin")]
        //[HttpPost]
        //public IActionResult AssignUserToCenter(int userId, int centerId)
        //{
        //    if (userId == 0 || centerId == 0)
        //    {
        //        ViewBag.Error = "Please select both user and center.";
        //        ViewBag.Users = _adminRepo.GetAllUsers();
        //        ViewBag.Centers = _adminRepo.GetAllCenters();
        //        ViewBag.Assignments = _adminRepo.GetAllUserCenterAssignments();
        //        return View();
        //    }
        //    _adminRepo.AssignUserToCenter(userId, centerId);
        //    TempData["Success"] = "User assigned to center successfully.";
        //    return RedirectToAction("AssignUserToCenter");
        //}

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

        // ════════════════════════════════════════════════════════
        // LOCATION — VILLAGE
        // ════════════════════════════════════════════════════════

        //[SessionAuthorize("Admin")]
        //public IActionResult Villages()
        [SessionAuthorize("Admin")]
        [HttpPost]
        public IActionResult AddCity(string cityName, int stateId)
        {
            _adminRepo.AddCity(cityName, stateId);
            TempData["Success"] = $"City '{cityName}' added successfully.";
            TempData["ActiveTab"] = "city";
            return RedirectToAction("Location");
        }

        //[SessionAuthorize("Admin")]
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

        //[SessionAuthorize("Admin")]
        public IActionResult GetVillagesByCity(int cityId)
        {
            var villages = _adminRepo.GetVillagesByCity(cityId);
            return Json(villages);
        }

        // ════════════════════════════════════════════════════════
        // MILK TYPES
        // ════════════════════════════════════════════════════════

       // [SessionAuthorize("Admin")]
        public IActionResult MilkTypes()
        {
            var milkTypes = _adminRepo.GetAllMilkTypes();
            return View(milkTypes);
        }

       // [SessionAuthorize("Admin")]
        [HttpPost]
        public IActionResult AddMilkType(string milkTypeName)
        {
            _adminRepo.AddMilkType(milkTypeName);
            return RedirectToAction("MilkTypes");
        }

        // ════════════════════════════════════════════════════════
        // RATE CHART
        // ════════════════════════════════════════════════════════

      //  [SessionAuthorize("Admin")]
        public IActionResult RateChart()
        {
            var rateCharts = _adminRepo.GetAllRateCharts();
            ViewBag.MilkTypes = _adminRepo.GetAllMilkTypes();
            return View(rateCharts);
        }

      //  [SessionAuthorize("Admin")]
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

        //[SessionAuthorize("Admin")]
        public IActionResult Staff()
        {
            var staffList = _adminRepo.GetAllStaff();
            return View(staffList);
        }

        //[SessionAuthorize("Admin")]
        // Update GET action to pass plants and centers to view
        [SessionAuthorize("Admin")]
        [HttpGet]
        public IActionResult AddStaff()
        {
            ViewBag.Plants = _adminRepo.GetAllPlants();
            ViewBag.Centers = _adminRepo.GetAllCenters();
            return View(_adminRepo.GetAllRoles());
        }

        [SessionAuthorize("Admin")]
        public IActionResult GetStaffById(int id)
        {
            var staff = _adminRepo.GetStaffById(id);
            if (staff == null)
            {
                TempData["Error"] = "Staff not found.";
                return RedirectToAction("staff");
            }
            return View(staff);
        }

        //[SessionAuthorize("Admin")]
        [HttpPost]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public IActionResult AddStaff(string firstName, string lastName,
                               string phone, string email,
                               int roleId, DateTime? doj,
                               string bankName, string accountNumber,
                               string ifscCode, IFormFile profilePhoto, Decimal Salary,
                               int? centerId, int? plantId)   // NEW
        {
            // Validate — cannot assign both
            if (centerId.HasValue && plantId.HasValue)
            {
                ViewBag.Error = "Please assign staff to either a Collection Center or a Plant — not both.";
                ViewBag.Plants = _adminRepo.GetAllPlants();
                ViewBag.Centers = _adminRepo.GetAllCenters();
                return View(_adminRepo.GetAllRoles());
            }

            string photoPath = null;
            if (profilePhoto != null && profilePhoto.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var extension = Path.GetExtension(profilePhoto.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                {
                    ViewBag.Error = "Only .jpg, .jpeg and .png files are allowed.";
                    ViewBag.Plants = _adminRepo.GetAllPlants();
                    ViewBag.Centers = _adminRepo.GetAllCenters();
                    return View(_adminRepo.GetAllRoles());
                }
                if (profilePhoto.Length > 2 * 1024 * 1024)
                {
                    ViewBag.Error = "Photo size must be less than 2MB.";
                    ViewBag.Plants = _adminRepo.GetAllPlants();
                    ViewBag.Centers = _adminRepo.GetAllCenters();
                    return View(_adminRepo.GetAllRoles());
                }
                string uploadsFolder = Path.Combine(
                    Directory.GetCurrentDirectory(), "wwwroot", "uploads", "staff");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);
                string uniqueFileName = $"staff_{DateTime.Now:yyyyMMddHHmmss}_{Path.GetFileName(profilePhoto.FileName)}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                    profilePhoto.CopyTo(stream);
                photoPath = $"/uploads/staff/{uniqueFileName}";
            }

            _adminRepo.AddStaff(firstName, lastName, phone, email,
    roleId, doj, bankName, accountNumber,
    ifscCode, Salary, photoPath,
    centerId, plantId); // NEW

            TempData["Success"] = "Staff member added successfully.";
            return RedirectToAction("Staff");
        }

        //[SessionAuthorize("Admin")]
        [HttpPost]
        public IActionResult ToggleStaffActive(int staffId, int isActive)
        {
            _adminRepo.ToggleStaffActive(staffId, isActive == 1);
            TempData["Success"] = isActive == 1 ? "Staff activated." : "Staff deactivated.";
            return RedirectToAction("Staff");
        }
        [HttpGet]
        [SessionAuthorize("Admin")]
        public IActionResult EditStaff(int id)
        {
            var staff = _adminRepo.GetStaffById(id);
            if (staff == null) return NotFound();

            ViewBag.Roles = _adminRepo.GetAllRoles();
            ViewBag.Centers = _adminRepo.GetAllCenters();
            ViewBag.Plants = _adminRepo.GetAllPlants();

            return View(staff);
        }

        [HttpPost]
        [SessionAuthorize("Admin")]
        [RequestSizeLimit(5 * 1024 * 1024)] // 2MB limit
        public IActionResult EditStaff(
    int staffId,
    string firstName,
    string lastName,
    string phone,
    string email,
    int roleId,
    DateTime? doj,
    string bankName,
    string accountNumber,
    string ifscCode,
    decimal salary,
    string profilePhoto,   
    IFormFile photoFile,   
    int? centerId,
    int? plantId)
        {
            if (centerId.HasValue && plantId.HasValue)
            {
                ViewBag.Error = "Please assign staff to either a Collection Center or a Plant — not both.";
                ViewBag.Roles = _adminRepo.GetAllRoles();
                ViewBag.Plants = _adminRepo.GetAllPlants();
                ViewBag.Centers = _adminRepo.GetAllCenters();
                return View(_adminRepo.GetStaffById(staffId));
            }

            string finalPhoto = profilePhoto; 

            if (photoFile != null && photoFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var extension = Path.GetExtension(photoFile.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                {
                    ViewBag.Error = "Only .jpg, .jpeg and .png files are allowed.";
                    ViewBag.Roles = _adminRepo.GetAllRoles();
                    ViewBag.Plants = _adminRepo.GetAllPlants();
                    ViewBag.Centers = _adminRepo.GetAllCenters();
                    return View(_adminRepo.GetStaffById(staffId));
                }

                if (photoFile.Length > 2 * 1024 * 1024)
                {
                    ViewBag.Error = "Photo size must be less than 2MB.";
                    ViewBag.Roles = _adminRepo.GetAllRoles();
                    ViewBag.Plants = _adminRepo.GetAllPlants();
                    ViewBag.Centers = _adminRepo.GetAllCenters();
                    return View(_adminRepo.GetStaffById(staffId));
                }

                string uploadsFolder = Path.Combine(
                    Directory.GetCurrentDirectory(), "wwwroot", "uploads", "staff");

                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                string fileName = Guid.NewGuid().ToString() + extension;
                string filePath = Path.Combine(uploadsFolder, fileName);

                try
                {
                    using (var stream = new FileStream(
                        filePath,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None,
                        4096,
                        FileOptions.SequentialScan))
                    {
                        photoFile.CopyTo(stream);
                    }

                    finalPhoto = $"/uploads/staff/{fileName}";
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "Error uploading file: " + ex.Message;
                    ViewBag.Roles = _adminRepo.GetAllRoles();
                    ViewBag.Plants = _adminRepo.GetAllPlants();
                    ViewBag.Centers = _adminRepo.GetAllCenters();
                    return View(_adminRepo.GetStaffById(staffId));
                }
            }

            _adminRepo.UpdateStaff(
                staffId,
                firstName,
                lastName,
                phone,
                email,
                roleId,
                doj,
                bankName,
                accountNumber,
                ifscCode,
                salary,
                finalPhoto,
                centerId,
                plantId
            );

            TempData["Message"] = "Staff updated successfully.";
            return RedirectToAction("Staff");
        }


        // ════════════════════════════════════════════════════════
        // PLANT
        // ════════════════════════════════════════════════════════

        //[SessionAuthorize("Admin")]
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
        // COLLECTION
        // ════════════════════════════════════════════════════════

        [SessionAuthorize("Admin")]
        [HttpGet]
        public ActionResult AddCollection()
        {
            var village = _adminRepo.GetAllVillages();
            return View(village);
        }

        [SessionAuthorize("Admin")]
        [HttpPost]
        public ActionResult AddCollection(string CenterName, int VillageID, decimal Capacity, string Location)
        {
            _adminRepo.AddCollection(CenterName, VillageID, Capacity, Location);
            return RedirectToAction("GetAllCollection");
        }
        [SessionAuthorize("Admin")]
        public ActionResult GetAllCollection(bool? isActive = true)
        {
            var collection = _adminRepo.GetAllCollection(isActive);
            return View(collection);
        }
        [SessionAuthorize("Admin")]
        [HttpPost]
        public ActionResult ToggleCollection(int id, bool isActive)
        {
            _adminRepo.ToggleCollection(id, isActive);
            return RedirectToAction("GetAllCollection");
        }
        [SessionAuthorize("Admin")]
        [HttpGet]
        public ActionResult EditCollection(int id)
        {
            ViewBag.Villages = _adminRepo.GetAllVillages();

            var collection = _adminRepo.getCollectionById(id);
            if (collection == null)
                return NotFound();

            return View(collection);
        }

        [SessionAuthorize("Admin")]
        [HttpPost]
        public ActionResult UpdateCollection(CollectionCenterModel collection)
        {

            _adminRepo.UpdateCollection(collection);
            return RedirectToAction("GetAllCollection");

            // repopulate ViewBag before returning view
            ViewBag.Villages = _adminRepo.GetAllVillages();
            return View("EditCollection", collection);
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
        [SessionAuthorize("Admin")]
        public IActionResult ProductionBatches(int? plantId = null, int? productId = null,
                                        string batchStatus = null,
                                        DateTime? fromDate = null, DateTime? toDate = null)
        {
            var batches = _adminRepo.GetProductionBatches(plantId, productId, batchStatus, fromDate, toDate);

            ViewBag.Plants = _adminRepo.GetAllPlants();
            ViewBag.Products = _adminRepo.GetActiveProducts();
            ViewBag.PlantId = plantId;
            ViewBag.ProductId = productId;
            ViewBag.BatchStatus = batchStatus;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            return View(batches);
        }
        [SessionAuthorize("Admin")]
        public IActionResult WastageSummary(int? plantId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var wastage = _reportRepo.GetWastageSummary(plantId, fromDate, toDate);

            ViewBag.Plants = _adminRepo.GetAllPlants();
            ViewBag.PlantId = plantId;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            return View(wastage);
        }

        [SessionAuthorize("Admin")]
        public IActionResult MilkTransfers(int? plantId = null, int? centerId = null,
                                    DateTime? fromDate = null, DateTime? toDate = null)
        {
            var transfers = _adminRepo.GetMilkTransfers(plantId, centerId, fromDate, toDate);

            ViewBag.Plants = _adminRepo.GetAllPlants();
            ViewBag.Centers = _adminRepo.GetAllCenters();
            ViewBag.PlantId = plantId;
            ViewBag.CenterId = centerId;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            return View(transfers);
        }

        //DISTRIBUTOR   
        [SessionAuthorize("Admin")]
        public ActionResult Distributors()
        {
            var distributors = _adminRepo.GetDistributors();
            return View(distributors);
        }
        public IActionResult AddDistributor()
        {
            return View();
        }

        // POST: Add Distributor
        [HttpPost]
        public IActionResult AddDistributor(Distributor distributor)
        {
            if (ModelState.IsValid)
            {
                _adminRepo.AddDistributor(distributor);
                TempData["success"] = "Distributor added successfully!";
                return RedirectToAction("Distributors");
            }

            return View(distributor);
        }
        [HttpPost]
        [SessionAuthorize("Admin")]
        public IActionResult UpdateDistributorStatus(int distributorId, string status)
        {
            var allowed = new[] { "Approved", "Rejected", "Suspended" };
            if (!allowed.Contains(status))
            {
                TempData["error"] = "Invalid status value.";
                return RedirectToAction("PendingDistributors");
            }

            _adminRepo.UpdateDistributorStatus(distributorId, status);

            TempData["success"] = status switch
            {
                "Approved" => "Distributor approved successfully.",
                "Rejected" => "Distributor rejected.",
                "Suspended" => "Distributor suspended.",
                _ => "Status updated."
            };

            // After approving/rejecting, go back to pending queue.
            // After suspending from main list, go back to full list.
            return status == "Suspended"
                ? RedirectToAction("Distributors")
                : RedirectToAction("PendingDistributors");
        }
        [SessionAuthorize("Admin")]
        public IActionResult PendingDistributors()
        {
            var pending = _adminRepo.GetPendingDistributors();
            return View(pending);
        }

        public IActionResult AdminOrder()
        {
            var model = new AdminOrderModel();
            model.DistributorList = _adminRepo.GetDistributors();
            model.ProductList = _adminRepo.GetAllProducts(null, true);
            model.PlantList = _adminRepo.GetActivePlants();   // added
            return View(model);
        }

        [HttpPost]
        [SessionAuthorize("Admin")]
        public IActionResult AdminOrder(AdminOrderModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _adminRepo.CreateOrder(model);
                    ViewBag.Message = "Order Created Successfully!";
                    model = new AdminOrderModel();
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }
            model.DistributorList = _adminRepo.GetDistributors();
            model.ProductList = _adminRepo.GetAllProducts(null, true);
            model.PlantList = _adminRepo.GetActivePlants();   // added
            return View(model);
        }
        [HttpGet]
        [SessionAuthorize("Admin")]
        public IActionResult AdminOrderList()
        {
            var model = new AdminOrderListModel();
            model.DistributorList = _adminRepo.GetDistributors();
            model.Orders = _adminRepo.GetAllOrders(null, null, null, null);
            return View(model);
        }

        [HttpPost]
        [SessionAuthorize("Admin")]
        public IActionResult AdminOrderList(AdminOrderListModel model)
        {
            model.DistributorList = _adminRepo.GetDistributors();
            model.Orders = _adminRepo.GetAllOrders(
                model.DistributorId,
                model.OrderStatus,
                model.FromDate,
                model.ToDate
            );
            return View(model);
        }

        [HttpPost]
        [SessionAuthorize("Admin")]
        public IActionResult UpdateOrderStatus(int orderId, string status, int? distributorId, string? orderStatus, DateTime? fromDate, DateTime? toDate)
        {
            _adminRepo.UpdateOrderStatus(orderId, status);
            TempData["Message"] = $"Order #{orderId} updated to {status}.";

            return RedirectToAction("AdminOrderList", new
            {
                DistributorId = distributorId,
                OrderStatus = orderStatus,
                FromDate = fromDate?.ToString("yyyy-MM-dd"),
                ToDate = toDate?.ToString("yyyy-MM-dd")
            });
        }

        //Notification

        [HttpGet]
        [SessionAuthorize("Admin")]
        public IActionResult GetNotifications()
        {
            try
            {
                var list = _adminRepo.GetNotifications();
                return Json(list);
            }
            catch
            {
                return Json(new List<NotificationModel>());
            }
        }

        [HttpGet]
        [SessionAuthorize("Admin")]
        public IActionResult GetNotificationCount()
        {
            try
            {
                var count = _adminRepo.GetNotificationCount();
                return Json(new { count });
            }
            catch
            {
                return Json(new { count = 0 });
            }
        }

        [HttpPost]
        [SessionAuthorize("Admin")]
        public IActionResult MarkNotificationRead(int notificationId)
        {
            if (notificationId <= 0)
                return Json(new { success = false, message = "Invalid notification." });

            var result = _adminRepo.MarkNotificationRead(notificationId);
            return Json(new { success = result });
        }

        [HttpPost]
        [SessionAuthorize("Admin")]
        public IActionResult MarkAllNotificationsRead()
        {
            var result = _adminRepo.MarkAllNotificationsRead();
            return Json(new { success = result });
        }


    }
}