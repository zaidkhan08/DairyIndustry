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

    public FarmerController(
        IFarmerRepository repo,
        IAdminRepository adminRepo,
        IWebHostEnvironment env)
        {
            _repo = repo;
            _adminRepo = adminRepo;
            _env = env;
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

                TempData["Success"] = $"Farmer Registered! Code: {result.FarmerCode}";
                return RedirectToAction("Create");
            }

            return View(model);
        }

        // =========================
        // EDIT (GET)
        // =========================
        public IActionResult Edit(int id)
        {
            int staffId = GetStaffId();
            if (staffId == 0)
                return RedirectToAction("Login", "Auth");

            var model = _repo.GetFarmerById(id, staffId);

            if (model == null)
                return NotFound();

            model.States = _repo.GetStates();
            model.Cities = _repo.GetCitiesByState(model.StateId ?? 0);
            model.Villages = _repo.GetVillagesByCity(model.CityId ?? 0);

            return View(model);
        }

        // =========================
        // EDIT (POST)
        // =========================
        [HttpPost]
        public IActionResult Edit(FarmerViewModel model)
        {
            int staffId = GetStaffId();
            if (staffId == 0)
                return RedirectToAction("Login", "Auth");

            model.States = _repo.GetStates();
            model.Cities = _repo.GetCitiesByState(model.StateId ?? 0);
            model.Villages = _repo.GetVillagesByCity(model.CityId ?? 0);

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

            try
            {
                int result = _repo.UpdateFarmer(model, staffId);

                if (result > 0)
                {
                    TempData["Success"] = "Farmer updated successfully!";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

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
    }


}
