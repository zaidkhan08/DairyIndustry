using DairyIndustry.Models.FarmerModel;
using DairyIndustry.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DairyIndustry.Controllers
{
    public class CollectionCenterController : Controller
    {
        private readonly ICollectionCenterRepository _repository;
        private readonly ILocationRepository _locationRepo;


        public CollectionCenterController(ICollectionCenterRepository repository, ILocationRepository locationRepo)
        {
            _repository = repository;
            _locationRepo = locationRepo;
        }

        public IActionResult Dashboard()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            int? centerId = HttpContext.Session.GetInt32("CenterId");

            if (userId == null || centerId == null)
                return RedirectToAction("Login", "Account");

            var center = _repository.GetCollectionCenterById(centerId.Value);

            if (center == null)
                return Content("Center not found");

            return View(center);
        }

        [HttpGet]
        public IActionResult FarmersDashboard()
        {
            int? centerId = HttpContext.Session.GetInt32("CenterId");

            if (centerId == null)
                return RedirectToAction("Login", "Account");

            var farmers = _repository.GetFarmersByCenterStaff(centerId.Value);

            return View(farmers);
        }

        [HttpGet]
        public JsonResult GetCitiesByState(int stateId)
        {
            var cities = _locationRepo.GetCitiesByState(stateId);
            return Json(cities);
        }

        [HttpGet]
        public JsonResult GetVillagesByCity(int cityId)
        {
            var villages = _locationRepo.GetVillagesByCity(cityId);
            return Json(villages);
        }

        [HttpGet]
        public IActionResult AddFarmer()
        {
            ViewBag.States = _locationRepo.GetAllStates();
            return View();
        }

        [HttpPost]
        public IActionResult AddFarmer(Farmer farmer, IFormFile photo)
        {
            int? centerId = HttpContext.Session.GetInt32("CenterId");

            if (centerId == null)
                return RedirectToAction("Login", "Account");

            if (photo != null && photo.Length > 0)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(photo.FileName);
                string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/profiles", fileName);

                using (var stream = new FileStream(uploadPath, FileMode.Create))
                {
                    photo.CopyTo(stream);
                }

                farmer.ProfilePhoto = "/uploads/profiles/" + fileName;
            }

            //  PASS CENTER ID HERE
            bool isSaved = _repository.AddFarmer(farmer, centerId.Value);

            if (isSaved)
                return RedirectToAction("FarmersDashboard");

            return View(farmer);
        }
    }
}