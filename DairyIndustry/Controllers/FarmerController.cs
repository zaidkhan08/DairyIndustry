using DairyIndustry.Models.Admin;
using DairyIndustry.Models.FarmerModel;
using DairyIndustry.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace DairyIndustry.Controllers
{
    public class FarmerController : Controller
    {
        private readonly IFarmerRepository _repo;
        private readonly IAdminRepository _adminRepo;
        private readonly IWebHostEnvironment _env;
        private readonly ICollectionCenterRepository _collectionCenter;

        public FarmerController(
        IFarmerRepository repo,
        IAdminRepository adminRepo,
        IWebHostEnvironment env,ICollectionCenterRepository collectionRepo)
        {
            _repo = repo;
            _adminRepo = adminRepo;
            _collectionCenter = collectionRepo;
            _env = env;
        }

        public IActionResult LayoutIndex()
        {
            return View();
        }

        // =========================
        // COMMON
        // =========================
        private int GetStaffId()
        {
            return HttpContext.Session.GetInt32("StaffId") ?? 0;
        }

        // =========================
        //  AJAX FOR CASCADING
        // =========================
        [HttpGet]
        public JsonResult GetCities(int stateId)
        {
            var cities = _repo.GetCitiesByState(stateId);
            return Json(cities);
        }

        [HttpGet]
        public JsonResult GetVillages(int cityId)
        {
            var villages = _repo.GetVillagesByCity(cityId);
            return Json(villages);
        }

        // =========================
        // INDEX
        // =========================
        public IActionResult Index(bool? isActive, string search)
        {
            int staffId = GetStaffId();
            if (staffId == 0)
                return RedirectToAction("Login", "Auth");

            var data = _repo.GetAllFarmers(staffId, isActive, search);
            return View(data);
        }

        // =========================
        // CREATE (GET)
        // =========================
        public IActionResult Create()
        {
            var model = new FarmerViewModel
            {
                States = _repo.GetStates(),
                Cities = new List<CityModel>(),
                Villages = new List<VillageModel>()
            };

            return View(model);
        }

        // =========================
        // CREATE (POST)
        // =========================
        [HttpPost]
        public IActionResult Create(FarmerViewModel model)
        {
            int staffId = GetStaffId();
            if (staffId == 0)
                return RedirectToAction("Login", "Auth");

            // Reload dropdowns
            model.States = _repo.GetStates();

            model.Cities = model.StateId > 0
                ? _repo.GetCitiesByState(model.StateId.Value)
                : new List<CityModel>();

            model.Villages = model.CityId > 0
                ? _repo.GetVillagesByCity(model.CityId.Value)
                : new List<VillageModel>();

            // FILE UPLOAD
            if (model.PhotoFile != null && model.PhotoFile.Length > 0)
            {
                string folder = Path.Combine(_env.WebRootPath, "uploads");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string fileName = Guid.NewGuid() + Path.GetExtension(model.PhotoFile.FileName);
                string filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    model.PhotoFile.CopyTo(stream);
                }

                model.ProfilePhoto = "/uploads/" + fileName;
            }

            // SAVE
            if (ModelState.IsValid)
            {
                var result = _repo.AddFarmer(model, staffId);
                TempData["FarmerCode"] = result.FarmerCode;
                TempData["Password"] = result.DefaultPassword;

                TempData["Success"] = "Farmer Registered Successfully!";
                //TempData["Success"] = $"Farmer Registered! Code: {result.FarmerCode}";
                return RedirectToAction("Create");
            }

            return View(model);
        }


        [HttpGet]
        public IActionResult Edit(int id)
        {
            int staffId = GetStaffId();
            if (staffId == 0)
                return RedirectToAction("Login", "Auth");

            var model = _repo.GetFarmerById(id, staffId);

            if (model == null)
                return NotFound();

            // Load dropdowns
            model.States = _repo.GetStates();
            model.Cities = _repo.GetCitiesByState(model.StateId ?? 0);
            model.Villages = _repo.GetVillagesByCity(model.CityId ?? 0);

            return View(model);
        }

        [HttpPost]
        public IActionResult Edit(FarmerViewModel model)
        {
            int staffId = GetStaffId();
            if (staffId == 0)
                return RedirectToAction("Login", "Auth");

            //  VERY IMPORTANT FIX
            ModelState.Remove("PhotoFile");

            //  Handle photo
            if (model.PhotoFile != null && model.PhotoFile.Length > 0)
            {
                string folder = Path.Combine(_env.WebRootPath, "uploads");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string fileName = Guid.NewGuid() + Path.GetExtension(model.PhotoFile.FileName);
                string filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    model.PhotoFile.CopyTo(stream);
                }

                model.ProfilePhoto = "/uploads/" + fileName;
            }

            // Validation AFTER removing PhotoFile
            if (!ModelState.IsValid)
            {
                model.States = _repo.GetStates();
                model.Cities = _repo.GetCitiesByState(model.StateId ?? 0);
                model.Villages = _repo.GetVillagesByCity(model.CityId ?? 0);

                return View(model);
            }

            int result = _repo.UpdateFarmer(model, staffId);

            if (result > 0)
            {
                TempData["Success"] = "Farmer updated successfully!";
                return RedirectToAction("Index");
            }

            TempData["Error"] = "Failed to update farmer.";

            model.States = _repo.GetStates();
            model.Cities = _repo.GetCitiesByState(model.StateId ?? 0);
            model.Villages = _repo.GetVillagesByCity(model.CityId ?? 0);

            return View(model);
        }
      

        // =========================
        // TOGGLE STATUS
        // =========================
        public IActionResult ToggleStatus(int id, bool isActive)
        {
            try
            {
                _repo.ToggleFarmerStatus(GetStaffId(), id, isActive);
                TempData["Success"] = "Farmer status updated successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("Index");
        }

        //farmer login
        public IActionResult Login()
        {
            return View();
        }

        // =========================
        // LOGIN POST
        // =========================
        [HttpPost]
        public IActionResult Login(FarmerLoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var farmer = _repo.FarmerLogin(model.FarmerCode, model.Password);

            if (farmer == null)
            {
                ModelState.AddModelError("", "Invalid Farmer Code or Password");
                return View(model);
            }

            // SESSION
            HttpContext.Session.SetInt32("FarmerId", farmer.FarmerId);
            HttpContext.Session.SetString("FarmerCode", farmer.FarmerCode);
            HttpContext.Session.SetString("FarmerName", farmer.FarmerName);

            return RedirectToAction("Dashboard", "Farmer");
        }

        // =========================
        // LOGOUT
        // =========================
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        //dashboard

        public IActionResult Dashboard()
        {
            int farmerId = HttpContext.Session.GetInt32("FarmerId") ?? 0;

            if (farmerId == 0)
                return RedirectToAction("Login");

            ViewData["Title"] = "Dashboard";

            var vm = _repo.GetDashboard(farmerId);

            return View(vm);
        }

        //Tody's milk Entries
        public IActionResult TodayMilk()
        {
            int farmerId = HttpContext.Session.GetInt32("FarmerId") ?? 0;

            if (farmerId == 0)
                return RedirectToAction("Login", "Farmer");

            var data = _repo.GetTodayMilkEntries(farmerId);

            return View(data);
        }

        //all milk Entries
        public IActionResult AllMilkEntries()
        {
            int farmerId = HttpContext.Session.GetInt32("FarmerId") ?? 0;

            if (farmerId == 0)
                return RedirectToAction("Login", "Farmer");

            var data = _repo.GetAllMilkEntries(farmerId);

            return View(data);
        }

        //farmer profile
        public IActionResult Profile()
        {
            int farmerId = HttpContext.Session.GetInt32("FarmerId") ?? 0;

            if (farmerId == 0)
                return RedirectToAction("Login");

            var model = _repo.GetFarmerProfile(farmerId);

            return View(model);
        }




        // SELF REGISTRATION — GET
        // Public page, no login needed.
        // Loads states for the first step dropdown.
        [HttpGet]
        public IActionResult Register()
        {
            var model = new SelfRegisterViewModel
            {
                States = _repo.GetStates()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(SelfRegisterViewModel model)
        {
            // Reload states
            model.States = _repo.GetStates();

            // Reload dropdowns if validation fails
            if (model.StateId != null)
                model.Cities = _repo.GetCitiesByState(model.StateId.Value);

            if (model.CityId != null)
                model.Villages = _repo.GetVillagesByCity(model.CityId.Value);

            if (model.VillageId != null)
                model.Centers = _collectionCenter.GetCentersByVillage(model.VillageId.Value);

            // Validation
            if (string.IsNullOrWhiteSpace(model.FarmerName))
            {
                TempData["Error"] = "Farmer name is required.";
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.Phone) || model.Phone.Length != 10)
            {
                TempData["Error"] = "Valid 10-digit phone number required.";
                return View(model);
            }

            if (model.CenterId == null)
            {
                TempData["Error"] = "Please select a center.";
                return View(model);
            }

            try
            {
                _repo.SelfRegisterFarmer(model);
                return RedirectToAction("RegisterSuccess");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return View(model);
            }
        }
        // REGISTER SUCCESS — static thank-you page
        [HttpGet]
        public IActionResult RegisterSuccess()
        {
            return View();
        }

        // CHECK STATUS — GET
        // Public page. Just shows the phone input form.
        [HttpGet]
        public IActionResult CheckStatus()
        {
            return View(new FarmerStatusViewModel());
        }

        // CHECK STATUS — POST
        // Looks up registration by phone and shows result.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CheckStatus(FarmerStatusViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Phone))
            {
                TempData["Error"] = "Please enter your phone number.";
                model.Searched = false;
                return View(model);
            }

            var result = _repo.GetFarmerStatusByPhone(model.Phone.Trim());

            model.Searched = true;

            if (result != null)
            {
                model.FarmerId = result.FarmerId;
                model.FarmerName = result.FarmerName;
                model.FarmerCode = result.FarmerCode;
                model.ApprovalStatus = result.ApprovalStatus;
                model.ApprovalRemark = result.ApprovalRemark;
                model.CenterName = result.CenterName;
            }

            return View(model);
        }
        [HttpGet]
        public JsonResult GetCenters(int villageId)
        {
            var centers = _collectionCenter.GetCentersByVillage(villageId);
            return Json(centers);
        }


        //milk rejection entries (history) for farmer
        public IActionResult RejectionHistory(DateTime? fromDate, DateTime? toDate)
        {
            int farmerId = HttpContext.Session.GetInt32("FarmerId") ?? 0;

            if (farmerId == 0)
                return RedirectToAction("Login");

            var data = _repo.GetRejectionHistory(farmerId, fromDate, toDate);

            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            if (data.Count == 0)
                TempData["Info"] = "No rejections found for this period.";

            return View(data);
        }
    }

}
