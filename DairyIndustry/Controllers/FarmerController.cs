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

        public FarmerController(IFarmerRepository repo, IAdminRepository adminRepo, IWebHostEnvironment env)
        {
            _repo = repo;
            _adminRepo = adminRepo;
            _env = env;
        }

        public IActionResult Index()
        {
            return View();
        }
        private int GetStaffId()
        {
            return HttpContext.Session.GetInt32("StaffId") ?? 0;
        }

        // GET
        public IActionResult Create()
        {
            var model = new FarmerViewModel
            {
                States = _adminRepo.GetAllStates(),
                Cities = new List<Models.Admin.CityModel>(),
                Villages = new List<Models.Admin.VillageModel>()
            };

            return View(model);
        }

        // POST
        [HttpPost]
        public IActionResult Create(FarmerViewModel model)
        {
            model.States = _adminRepo.GetAllStates();

            model.Cities = model.StateId > 0
                ? _adminRepo.GetCitiesByState(model.StateId.Value)
                : new List<Models.Admin.CityModel>();

            model.Villages = model.CityId > 0
                ? _adminRepo.GetVillagesByCity(model.CityId.Value)
                : new List<Models.Admin.VillageModel>();

            //  FILE UPLOAD
            if (model.PhotoFile != null)
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

            if (ModelState.IsValid && model.VillageId > 0)
            {
                _repo.AddFarmer(model, GetStaffId());

                TempData["Success"] = "Farmer Registered Successfully!";
                return RedirectToAction("Create");
            }

            return View(model);
        }
    }
}