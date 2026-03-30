using DairyIndustry.Models.FarmerModel;
using DairyIndustry.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DairyIndustry.Controllers
{
    public class FarmerController : Controller
    {
        private readonly IFarmerRepository _farmerRepo;
        public FarmerController(IFarmerRepository farmerRepo, IWebHostEnvironment env)
        {
            _farmerRepo = farmerRepo;
        }


    }
}