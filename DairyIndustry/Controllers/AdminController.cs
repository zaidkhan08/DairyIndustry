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
    }

}
