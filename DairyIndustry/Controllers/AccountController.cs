using DairyIndustry.Models.ViewModels;
using DairyIndustry.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DairyIndustry.Controllers
{
    public class AccountController : Controller
    {
       
        private readonly IUserRepository _userRepo;

        public AccountController(IUserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Login()
        {
            return View();
        }


        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            var user = _userRepo.ValidateUser(model.UserName, model.PasswordHash);

            if (user == null)
            {
                ViewBag.Error = "Invalid login";
                return View(model);
            }

            //  Store session
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetInt32("RoleId", user.RoleId);
            HttpContext.Session.SetString("Username", user.UserName);

            //  IMPORTANT (for filtering data)

            HttpContext.Session.SetInt32("CenterId", user.CenterId);
            HttpContext.Session.SetInt32("FarmerId", user.FarmerId);

            //  Role-based redirect
            if (user.RoleId == 2) // Farmer
            {
                return RedirectToAction("Dashboard", "Farmer");
            }
            else if (user.RoleId == 3) // Collection Center
            {
                return RedirectToAction("Dashboard", "CollectionCenter");
            }
            else
            {
                ViewBag.Error = "Unauthorized user";
                return View(model);
            }
        }

        public IActionResult LogOut()
        {
            return RedirectToAction("Index");
        }


    }

}

