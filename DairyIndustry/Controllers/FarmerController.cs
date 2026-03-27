using DairyIndustry.Models.FarmerModel;
using DairyIndustry.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DairyIndustry.Controllers
{
    public class FarmerController : Controller
    {

        private readonly IFarmerRepository _repository;

        public FarmerController(IFarmerRepository repository)
        {
            _repository = repository;
        }

        public IActionResult Dashboard()
        {
            return View();
        }

        
    }
}
