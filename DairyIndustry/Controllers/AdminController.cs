using DairyIndustry.Filters;
using DairyIndustry.Interfaces;
using DairyIndustry.Models.Admin;
using DairyIndustry.Models.Finance;
using DairyIndustry.Repositories;
using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using System.Text;

namespace DairyIndustry.Controllers
{
    [ServiceFilter(typeof(ActionLogFilter))]
    public class AdminController : Controller
    {
        private readonly IAdminRepository _adminRepo;
        private readonly ILogisticsRepository _logisticsRepo;
        private readonly IReportRepository _reportRepo;
        private readonly IFinanceRepository _financeRepo;
        private readonly IWebHostEnvironment _env;
        private readonly EmailSettings _settings;
        private readonly IAuthRepository _authRepo;
        private readonly IConverter _pdfConverter;
        public AdminController(IAdminRepository adminRepo, ILogisticsRepository logisticsRepo, IReportRepository reportRepo, IWebHostEnvironment env, IFinanceRepository financeRepo, IAuthRepository authRepo, IOptions<EmailSettings> settings, IConverter pdfConverter)
        {
            _adminRepo = adminRepo;
            _logisticsRepo = logisticsRepo;
            _reportRepo = reportRepo;
            _financeRepo = financeRepo;
            _env = env;
            _authRepo = authRepo;
            _settings = settings.Value;
            _pdfConverter = pdfConverter;
        }

        // ════════════════════════════════════════════════════════
        // LOGIN — NO [SessionAuthorize] here
        // ════════════════════════════════════════════════════════

        public IActionResult Profile()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return RedirectToAction("Login");

            var user = _adminRepo.GetUserProfile(Convert.ToInt32(userId));

            return View(user);
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            // 1. Get user
            var user = _adminRepo.GetUserByUsername(username);
            if (user == null)
            {
                ViewBag.Error = "Invalid username or password.";
                return View();
            }

            // 2. Check active
            if (!user.IsActive)
            {
                ViewBag.Error = "Your account is inactive. Contact admin.";
                return View();
            }

            // 3. Verify password
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            if (!isPasswordValid)
            {
                ViewBag.Error = "Invalid username or password.";
                return View();
            }

            // 4. Check trusted device cookie
            var deviceToken = Request.Cookies["DMS_TrustedDevice"];
            if (!string.IsNullOrEmpty(deviceToken))
            {
                bool isTrusted = _authRepo.CheckTrustedDevice(user.UserId, deviceToken);
                if (isTrusted)
                {
                    SetSession(user);
                    return RedirectByRole(user);
                }
            }

            // 5. Admin has no email — skip OTP
            if (string.IsNullOrEmpty(user.Email))
            {
                SetSession(user);
                return RedirectByRole(user);
            }

            // 6. Generate and send OTP
            var otp = _authRepo.GenerateOtp(user.UserId, "Login");
            _adminRepo.SendOtpEmail(user.Email, user.FullName ?? user.Username, otp, "Login");

            // 7. Store in TempData for OTP page
            TempData["OtpUserId"] = user.UserId;
            TempData["OtpUsername"] = user.Username;
            TempData["OtpFullName"] = user.FullName ?? user.Username;
            TempData["OtpEmail"] = MaskEmail(user.Email);

            return RedirectToAction("VerifyOtp");
        }
        private void SetSession(User user)
        {
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("RoleName", user.RoleName);

            if (user.StaffId.HasValue)
                HttpContext.Session.SetInt32("StaffId", user.StaffId.Value);

            if (user.CenterId.HasValue)
            {
                HttpContext.Session.SetInt32("CenterId", user.CenterId.Value);
                HttpContext.Session.SetString("CenterName", user.CenterName ?? "");
            }

            if (user.PlantId.HasValue)
            {
                HttpContext.Session.SetInt32("PlantId", user.PlantId.Value);
                HttpContext.Session.SetString("PlantName", user.PlantName ?? "");
            }
        }

        private IActionResult RedirectByRole(User user)
        {
            switch (user.RoleName)
            {
                case "Admin":
                    return RedirectToAction("Index", "Admin");

                case "Driver":
                    var driver = _logisticsRepo.GetDriverByUserId(user.UserId);
                    if (driver != null)
                        HttpContext.Session.SetInt32("DriverId", driver.DriverId);
                    return RedirectToAction("Index", "Logistics");

                case "Plant Manager":
                    return RedirectToAction("Index", "Production");

                case "Collection Agent":
                    return RedirectToAction("Index", "Production");

                default:
                    return RedirectToAction("Login", "Admin");
            }
        }

        private string MaskEmail(string email)
        {
            if (string.IsNullOrEmpty(email)) return "";
            var parts = email.Split('@');
            if (parts.Length != 2) return email;
            var name = parts[0];
            var domain = parts[1];
            var masked = name.Length <= 2
                ? new string('*', name.Length)
                : name[..2] + new string('*', name.Length - 2);
            return $"{masked}@{domain}";
        }
        [HttpGet]
        public IActionResult ResendOtp()
        {
            if (TempData["OtpUserId"] == null)
                return RedirectToAction("Login");

            int userId = Convert.ToInt32(TempData["OtpUserId"]);
            string username = TempData["OtpUsername"]?.ToString();
            string fullName = TempData["OtpFullName"]?.ToString();
            string email = TempData["OtpEmail"]?.ToString();

            // Get real email from DB since TempData has masked version
            var user = _adminRepo.GetUserByUsername(username);

            var otp = _authRepo.GenerateOtp(userId, "Login");
            _adminRepo.SendOtpEmail(user.Email, fullName, otp, "Login");

            // Keep TempData alive
            TempData["OtpUserId"] = userId;
            TempData["OtpUsername"] = username;
            TempData["OtpFullName"] = fullName;
            TempData["OtpEmail"] = email;

            TempData["Success"] = "OTP resent successfully.";
            return RedirectToAction("VerifyOtp");
        }
        [HttpGet]
        public IActionResult VerifyOtp()
        {
            // If OtpUserId not in TempData, user didn't come from login
            if (TempData["OtpUserId"] == null)
                return RedirectToAction("Login");

            // Keep TempData alive for POST
            TempData.Keep();

            ViewBag.MaskedEmail = TempData["OtpEmail"];
            ViewBag.FullName = TempData["OtpFullName"];
            return View();
        }

        // ════════════════════════════════════════════════════
        // POST — OTP Verification
        // ════════════════════════════════════════════════════

        [HttpPost]
        public IActionResult VerifyOtp(string otpCode, bool rememberDevice = false)
        {
            if (TempData["OtpUserId"] == null)
                return RedirectToAction("Login");

            int userId = Convert.ToInt32(TempData["OtpUserId"]);
            string username = TempData["OtpUsername"]?.ToString();

            // Validate OTP
            var result = _authRepo.ValidateOtp(userId, otpCode, "Login");

            if (!result.IsValid)
            {
                // Keep TempData alive so user can try again
                TempData.Keep();
                ViewBag.MaskedEmail = TempData["OtpEmail"];
                ViewBag.FullName = TempData["OtpFullName"];
                ViewBag.Error = result.Reason;
                return View();
            }

            // OTP valid — get full user to set session
            var user = _adminRepo.GetUserByUsername(username);

            // Remember this device?
            if (rememberDevice)
            {
                var newDeviceToken = Guid.NewGuid().ToString();
                var deviceName = Request.Headers["User-Agent"].ToString();

                _authRepo.RegisterTrustedDevice(userId, newDeviceToken, deviceName);

                // Set HttpOnly cookie for 30 days
                Response.Cookies.Append("DMS_TrustedDevice", newDeviceToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    Expires = DateTimeOffset.UtcNow.AddDays(30),
                    SameSite = SameSiteMode.Strict
                });
            }

            SetSession(user);
            return RedirectByRole(user);
        }
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // ════════════════════════════════════════════════════
        // GET — Change Password Page (Step 1: enter current password)
        // ════════════════════════════════════════════════════

        [HttpGet]
        public IActionResult ChangePassword()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Login");

            return View();
        }

        // ════════════════════════════════════════════════════
        // POST — Verify current password, send OTP
        // ════════════════════════════════════════════════════

        [HttpPost]
        public IActionResult ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login");

            // 1. Validate new password match
            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "New passwords do not match.";
                return View();
            }

            // 2. Validate password length
            if (newPassword.Length < 8)
            {
                ViewBag.Error = "Password must be at least 8 characters.";
                return View();
            }

            // 3. Get user and verify current password
            string username = HttpContext.Session.GetString("Username");
            var user = _adminRepo.GetUserByUsername(username);

            bool isCurrentValid = BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash);
            if (!isCurrentValid)
            {
                ViewBag.Error = "Current password is incorrect.";
                return View();
            }

            // 4. Check user has email for OTP
            if (string.IsNullOrEmpty(user.Email))
            {
                // No email — change directly without OTP
                var hash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                _adminRepo.ChangePassword(userId.Value, hash);
                TempData["Success"] = "Password changed successfully.";
                return RedirectToAction("ChangePassword");
            }

            // 5. Generate and send OTP
            var otp = _authRepo.GenerateOtp(userId.Value, "ChangePassword");
            _adminRepo.SendOtpEmail(user.Email, user.FullName ?? user.Username, otp, "ChangePassword");

            // 6. Store new password hash in TempData (hashed — safe to store)
            TempData["CpUserId"] = userId.Value;
            TempData["CpNewHash"] = BCrypt.Net.BCrypt.HashPassword(newPassword);
            TempData["CpMaskedEmail"] = MaskEmail(user.Email);

            return RedirectToAction("VerifyChangePasswordOtp");
        }

        // ════════════════════════════════════════════════════
        // GET — OTP verification for change password
        // ════════════════════════════════════════════════════

        [HttpGet]
        public IActionResult VerifyChangePasswordOtp()
        {
            if (TempData["CpUserId"] == null)
                return RedirectToAction("ChangePassword");

            TempData.Keep();
            ViewBag.MaskedEmail = TempData["CpMaskedEmail"];
            return View();
        }

        // ════════════════════════════════════════════════════
        // POST — Validate OTP and apply new password
        // ════════════════════════════════════════════════════

        [HttpPost]
        public IActionResult VerifyChangePasswordOtp(string otpCode)
        {
            if (TempData["CpUserId"] == null)
                return RedirectToAction("ChangePassword");

            int userId = Convert.ToInt32(TempData["CpUserId"]);
            string newHash = TempData["CpNewHash"]?.ToString();

            // Validate OTP
            var result = _authRepo.ValidateOtp(userId, otpCode, "ChangePassword");

            if (!result.IsValid)
            {
                TempData.Keep();
                ViewBag.MaskedEmail = TempData["CpMaskedEmail"];
                ViewBag.Error = result.Reason;
                return View();
            }

            // OTP valid — apply new password
            _adminRepo.ChangePassword(userId, newHash);

            // Revoke all trusted devices — force re-login on other devices
            _authRepo.RevokeAllTrustedDevices(userId);

            // Clear the trusted device cookie on this device too
            Response.Cookies.Delete("DMS_TrustedDevice");

            // Clear session — force re-login
            HttpContext.Session.Clear();

            TempData["Success"] = "Password changed successfully. Please log in again.";
            return RedirectToAction("Login");
        }

        // ════════════════════════════════════════════════════
        // GET — Resend OTP for change password
        // ════════════════════════════════════════════════════

        [HttpGet]
        public IActionResult ResendChangePasswordOtp()
        {
            if (TempData["CpUserId"] == null)
                return RedirectToAction("ChangePassword");

            int userId = Convert.ToInt32(TempData["CpUserId"]);
            string username = HttpContext.Session.GetString("Username");
            var user = _adminRepo.GetUserByUsername(username);

            var otp = _authRepo.GenerateOtp(userId, "ChangePassword");
            _adminRepo.SendOtpEmail(user.Email, user.FullName ?? user.Username, otp, "ChangePassword");

            TempData.Keep();
            TempData["Success"] = "OTP resent successfully.";
            return RedirectToAction("VerifyChangePasswordOtp");
        }

        // ════════════════════════════════════════════════════
        // GET — Settings Page
        // ════════════════════════════════════════════════════

        [HttpGet]
        public IActionResult Settings()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login");

            var devices = _authRepo.GetTrustedDevices(userId.Value);
            return View(devices);
        }

        // ════════════════════════════════════════════════════
        // POST — Revoke single device
        // ════════════════════════════════════════════════════

        [HttpPost]
        public IActionResult RevokeDevice(int deviceId)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login");

            _authRepo.RevokeDeviceById(userId.Value, deviceId);

            TempData["Success"] = "Device removed successfully.";
            return RedirectToAction("Settings");
        }

        // ════════════════════════════════════════════════════
        // POST — Revoke ALL devices
        // ════════════════════════════════════════════════════

        [HttpPost]
        public IActionResult RevokeAllDevices()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login");

            _authRepo.RevokeAllTrustedDevices(userId.Value);

            // Also clear cookie on current device
            Response.Cookies.Delete("DMS_TrustedDevice");

            TempData["Success"] = "All trusted devices removed.";
            return RedirectToAction("Settings");
        }

        // ════════════════════════════════════════════════════════
        // DASHBOARD
        // ════════════════════════════════════════════════════════

        [SessionAuthorize("Admin")]
        public IActionResult Index()
        {
            // ── Core entity lists ──────────────────────────────────────────
            var users = _adminRepo.GetAllUsers();
            var staff = _adminRepo.GetAllStaff();
            var plants = _adminRepo.GetAllPlants(isActive: null);
            var centers = _adminRepo.GetAllCollection(isActive: null);
            var products = _adminRepo.GetAllProducts(isActive: null);
            var batches = _adminRepo.GetProductionBatches();
            var transfers = _adminRepo.GetMilkTransfers();

            // ── Chart / summary data ───────────────────────────────────────
            var collectionSummary = _adminRepo.GetCollectionSummary();
            var financeSummary = _adminRepo.GetFinanceSummary();
            var milkLast7Days = _adminRepo.GetMilkCollectedLast7Days();
            var topProducts = _adminRepo.GetTopProductsByMilkUsed(5);
            var ordersByStatus = _adminRepo.GetOrdersByStatus();

            // ── Transfer chart series — last 5 received transfers ─────────
            var recentReceived = transfers
                .Where(t => t.ReceivedQty.HasValue)
                .OrderByDescending(t => t.DispatchDate)
                .Take(5)
                .Reverse()
                .ToList();

            var transferDispatch = recentReceived.Select(t => new ChartPoint
            { Label = $"T-{t.TransferId}", Value = t.DispatchQty }).ToList();

            var transferReceived = recentReceived.Select(t => new ChartPoint
            { Label = $"T-{t.TransferId}", Value = t.ReceivedQty ?? 0 }).ToList();

            var transferLoss = recentReceived.Select(t => new ChartPoint
            { Label = $"T-{t.TransferId}", Value = t.LossQty ?? 0 }).ToList();

            // ── Production summary ─────────────────────────────────────────
            var prodSummary = new DashboardProductionSummary
            {
                InProgress = batches.Count(b => b.BatchStatus == "InProgress"),
                Completed = batches.Count(b => b.BatchStatus == "Completed"),
                QCFailed = batches.Count(b => b.BatchStatus == "QCFailed"),
                Cancelled = batches.Count(b => b.BatchStatus == "Cancelled"),
                TotalMilkUsed = batches.Sum(b => b.MilkUsedQuantity)
            };

            // ── Transfer summary ───────────────────────────────────────────
            var totalDispatch = transfers.Sum(t => t.DispatchQty);
            var totalLoss = transfers.Sum(t => t.LossQty ?? 0);
            var transferSummary = new DashboardTransferSummary
            {
                TotalDispatched = totalDispatch,
                TotalReceived = transfers.Sum(t => t.ReceivedQty ?? 0),
                TotalLoss = totalLoss,
                LossPercent = totalDispatch > 0 ? Math.Round((totalLoss / totalDispatch) * 100, 2) : 0,
                PendingCount = transfers.Count(t => t.TransferStatus == "Pending"),
                ReceivedCount = transfers.Count(t => t.TransferStatus == "Received")
            };

            // ── Payment bar chart series ───────────────────────────────────
            var payPaid = new List<ChartPoint>
    {
        new() { Label = "Farmers", Value = financeSummary.TotalFarmerPaid    },
        new() { Label = "Staff",   Value = financeSummary.TotalStaffPaid     },
        new() { Label = "Centers", Value = financeSummary.TotalCenterPaid    }
    };
            var payPending = new List<ChartPoint>
    {
        new() { Label = "Farmers", Value = financeSummary.TotalFarmerPending },
        new() { Label = "Staff",   Value = financeSummary.TotalStaffPending  },
        new() { Label = "Centers", Value = financeSummary.TotalCenterPending }
    };

            // ── Batch status chart ─────────────────────────────────────────
            var batchByStatus = new List<ChartPoint>
    {
        new() { Label = "In Progress", Value = prodSummary.InProgress },
        new() { Label = "Completed",   Value = prodSummary.Completed  },
        new() { Label = "QC Failed",   Value = prodSummary.QCFailed   },
        new() { Label = "Cancelled",   Value = prodSummary.Cancelled  }
    };

            // ── Compose VM ─────────────────────────────────────────────────
            var vm = new AdminDashboardViewModel
            {
                Users = users,
                Staff = staff,
                Plants = plants,
                Centers = centers,
                Products = products,
                Batches = batches,
                Transfers = transfers,

                Collection = collectionSummary,
                Production = prodSummary,
                Transfer = transferSummary,
                Finance = financeSummary,

                MilkLast7Days = milkLast7Days,
                BatchByStatus = batchByStatus,
                TopProductsByMilkUsed = topProducts,
                TransferDispatchSeries = transferDispatch,
                TransferReceivedSeries = transferReceived,
                TransferLossSeries = transferLoss,
                PaymentPaidSeries = payPaid,
                PaymentPendingSeries = payPending,
                OrdersByStatus = ordersByStatus,

                RecentUsers = users.OrderByDescending(u => u.CreatedDate).Take(5).ToList(),
                RecentBatches = batches.OrderByDescending(b => b.ProductionDate).Take(5).ToList(),
                RecentTransfers = transfers.OrderByDescending(t => t.DispatchDate).Take(5).ToList()
            };

            return View(vm);
        }
        // ════════════════════════════════════════════════════════
        // ROLES
        // ════════════════════════════════════════════════════════

        [SessionAuthorize("Admin")]
        public IActionResult Roles(int page = 1, int pageSize = 5)
        {
            var allRoles = _adminRepo.GetAllRoles();

            var totalRoles = allRoles.Count();

            var roles = allRoles
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalRoles / pageSize);

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
        public IActionResult Users(int page = 1, int pageSize = 10)
        {
            var allUsers = _adminRepo.GetAllUsers();

            var totalUsers = allUsers.Count();

            var users = allUsers
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalUsers / pageSize);

            return View(users);
        }

        private string BuildUsersHtml(List<User> users)
        {
            var sb = new StringBuilder();

            sb.Append(@"
    <html>
    <head>
        <style>
            body{font-family:Arial;font-size:12px;}
            table{width:100%;border-collapse:collapse;}
            th,td{
                border:1px solid #ccc;
                padding:8px;
                text-align:left;
            }
            th{
                background:#f2f2f2;
            }
        </style>
    </head>
    <body>
    ");

            sb.Append("<h2>Users List</h2>");

            sb.Append(@"
    <table>
        <thead>
            <tr>
                <th>Username</th>
                <th>Role</th>
                <th>Status</th>
            </tr>
        </thead>
        <tbody>
    ");

            foreach (var item in users)
            {
                sb.Append($@"
        <tr>
            <td>{item.Username}</td>
            <td>{item.RoleName}</td>
            <td>{(item.IsActive ? "Active" : "Inactive")}</td>
        </tr>
        ");
            }

            sb.Append("</tbody></table></body></html>");

            return sb.ToString();
        }

        public IActionResult DownloadUsersPdf()
        {
            var users = _adminRepo.GetAllUsers()
                                  .Take(50)
                                  .ToList();

            string html = BuildUsersHtml(users);

            byte[] pdfBytes = GeneratePdfFromHtml(html);

            return File(
                pdfBytes,
                "application/pdf",
                $"UsersList_{DateTime.Now:yyyyMMdd}.pdf"
            );
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

        [SessionAuthorize("Admin")]
        [HttpGet]
        public IActionResult AssignUserToPlant()
        {
            ViewBag.Users = _adminRepo.GetAllUsers();
            ViewBag.Plants = _adminRepo.GetAllPlants();
            ViewBag.Assignments = _adminRepo.GetAllUserPlantAssignments();
            return View();
        }

        [SessionAuthorize("Admin")]
        [HttpPost]
        public IActionResult AssignUserToPlant(int userId, int plantId)
        {
            if (userId == 0 || plantId == 0)
            {
                ViewBag.Error = "Please select both user and plant.";
                ViewBag.Users = _adminRepo.GetAllUsers();
                ViewBag.Plants = _adminRepo.GetAllPlants();
                ViewBag.Assignments = _adminRepo.GetAllUserPlantAssignments();
                return View();
            }
            _adminRepo.AssignUserToPlant(userId, plantId);
            TempData["Success"] = "User assigned to plant successfully.";
            return RedirectToAction("AssignUserToPlant");
        }

        [SessionAuthorize("Admin")]
        [HttpGet]
        public IActionResult AssignUserToCenter()
        {
            ViewBag.Users = _adminRepo.GetAllUsers();
            ViewBag.Centers = _adminRepo.GetAllCenters();
            ViewBag.Assignments = _adminRepo.GetAllUserCenterAssignments();
            return View();
        }

        [SessionAuthorize("Admin")]
        [HttpPost]
        public IActionResult AssignUserToCenter(int userId, int centerId)
        {
            if (userId == 0 || centerId == 0)
            {
                ViewBag.Error = "Please select both user and center.";
                ViewBag.Users = _adminRepo.GetAllUsers();
                ViewBag.Centers = _adminRepo.GetAllCenters();
                ViewBag.Assignments = _adminRepo.GetAllUserCenterAssignments();
                return View();
            }
            _adminRepo.AssignUserToCenter(userId, centerId);
            TempData["Success"] = "User assigned to center successfully.";
            return RedirectToAction("AssignUserToCenter");
        }

        // ════════════════════════════════════════════════════════
        // AUDIT LOGS
        // ════════════════════════════════════════════════════════

        [SessionAuthorize("Admin")]
        public IActionResult AuditLogs(
    int? userId,
    string? entityName,
    DateTime? fromDate,
    DateTime? toDate,
    int page = 1,
    int pageSize = 10)
        {
            var allLogs = _adminRepo.GetAuditLogs(userId, entityName, fromDate, toDate);

            var totalRecords = allLogs.Count();

            var logs = allLogs
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalRecords = totalRecords;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

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
        public IActionResult Staff(int page = 1, int pageSize = 10)
        {
            var staffList = _adminRepo.GetAllStaff();
            var totalStaff = staffList.Count();

            var staff = staffList
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalStaff / pageSize);
            ViewBag.PageSize = pageSize;
            ViewBag.TotalStaff = totalStaff;

            return View(staff);
        }
        [SessionAuthorize("Admin")]
        private string BuildStaffHtml(List<StaffModel> staff)
        {
            var sb = new StringBuilder();

            sb.Append(@"
        <html>
        <head>
            <style>
                body{
                    font-family:Arial;
                    font-size:12px;
                }

                table{
                    width:100%;
                    border-collapse:collapse;
                }

                th,td{
                    border:1px solid #ccc;
                    padding:8px;
                    text-align:left;
                }

                th{
                    background:#f2f2f2;
                }

                h2{
                    text-align:center;
                }
            </style>
        </head>
        <body>
    ");

            sb.Append("<h2>Staff List</h2>");

            sb.Append(@"
        <table>
            <thead>
                <tr>
                    <th>Name</th>
                    <th>Role</th>
                    <th>Email</th>
                    <th>Phone</th>
                    <th>Status</th>
                </tr>
            </thead>
            <tbody>
    ");

            foreach (var item in staff)
            {
                sb.Append($@"
            <tr>
                <td>{item.FirstName} {item.LastName}</td>
                <td>{item.RoleName}</td>
                <td>{item.Email}</td>
                <td>{item.Phone}</td>
                <td>{(item.IsActive ? "Active" : "Inactive")}</td>
            </tr>
        ");
            }

            sb.Append(@"
            </tbody>
        </table>
        </body>
        </html>
    ");

            return sb.ToString();
        }

        private byte[] GeneratePdfFromHtml(string html)
        {
            var doc = new HtmlToPdfDocument
            {
                GlobalSettings = new GlobalSettings
                {
                    ColorMode = ColorMode.Color,
                    Orientation = Orientation.Landscape,
                    PaperSize = PaperKind.A4,
                    Margins = new MarginSettings
                    {
                        Top = 10,
                        Bottom = 10,
                        Left = 10,
                        Right = 10
                    }
                },

                Objects =
        {
            new ObjectSettings
            {
                HtmlContent = html,
                WebSettings =
                {
                    DefaultEncoding = "utf-8"
                }
            }
        }
            };

            return _pdfConverter.Convert(doc);
        }
        public IActionResult DownloadStaffPdf()
        {
            var staffList = _adminRepo.GetAllStaff()
                                      .Take(50)
                                      .ToList();

            string html = BuildStaffHtml(staffList);

            byte[] pdfBytes = GeneratePdfFromHtml(html);

            return File(
                pdfBytes,
                "application/pdf",
                $"StaffList_{DateTime.Now:yyyyMMdd}.pdf"
            );
        }

        [SessionAuthorize("Admin")]
        [HttpGet]
        public IActionResult AddStaff()
        {
            ViewBag.Plants = _adminRepo.GetAllPlants();
            ViewBag.Centers = _adminRepo.GetAllCenters();
            return View(_adminRepo.GetAllRoles());
        }
        [HttpPost]
        [SessionAuthorize("Admin")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> AddStaff(string firstName, string lastName,
            string phone, string email,
            int roleId, DateTime? doj,
            string bankName, string accountNumber,
            string ifscCode, IFormFile profilePhoto, decimal Salary,
            int? centerId, int? plantId,
            string username, string password)
        {
            // Helper to reload dropdowns on error
            void ReloadViewBag()
            {
                ViewBag.Plants = _adminRepo.GetAllPlants();
                ViewBag.Centers = _adminRepo.GetAllCenters();
            }

            try
            {
                // Get role name to enforce business rules
                var allRoles = _adminRepo.GetAllRoles();
                var selectedRole = allRoles.FirstOrDefault(r => r.RoleId == roleId);
                string roleName = selectedRole?.RoleName ?? "";

                // ── Business rule: can't assign both ────────────────
                if (centerId.HasValue && plantId.HasValue)
                {
                    ViewBag.Error = "Assign staff to either a Collection Center or a Plant — not both.";
                    ReloadViewBag();
                    return View(allRoles);
                }

                // ── Business rule: Plant Manager must have a plant ──
                if (roleName == "Plant Manager" && !plantId.HasValue)
                {
                    ViewBag.Error = "Plant Manager must be assigned to a Plant.";
                    ReloadViewBag();
                    return View(allRoles);
                }

                // ── Business rule: Collection Agent must have center─
                if (roleName == "Collection Agent" && !centerId.HasValue)
                {
                    ViewBag.Error = "Collection Agent must be assigned to a Collection Center.";
                    ReloadViewBag();
                    return View(allRoles);
                }

                // ── Business rule: login roles need credentials ──────
                var loginRoles = new[] { "Administrator", "Plant Manager", "Collection Agent", "HR Manager" };
                bool needsLogin = loginRoles.Contains(roleName);

                if (needsLogin && (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)))
                {
                    ViewBag.Error = $"{roleName} requires a username and password.";
                    ReloadViewBag();
                    return View(allRoles);
                }

                // ── Photo upload ─────────────────────────────────────
                string photoPath = null;
                if (profilePhoto != null && profilePhoto.Length > 0)
                {
                    var extension = Path.GetExtension(profilePhoto.FileName).ToLowerInvariant();
                    if (extension != ".jpg" && extension != ".jpeg" && extension != ".png")
                    {
                        ViewBag.Error = "Only .jpg, .jpeg, .png allowed.";
                        ReloadViewBag();
                        return View(allRoles);
                    }
                    if (profilePhoto.Length > 2 * 1024 * 1024)
                    {
                        ViewBag.Error = "Max photo size is 2MB.";
                        ReloadViewBag();
                        return View(allRoles);
                    }

                    string uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "staff");
                    Directory.CreateDirectory(uploadFolder);
                    string fileName = Guid.NewGuid() + extension;
                    string filePath = Path.Combine(uploadFolder, fileName);

                    using var ms = new MemoryStream();
                    await profilePhoto.CopyToAsync(ms);
                    await System.IO.File.WriteAllBytesAsync(filePath, ms.ToArray());
                    photoPath = "/uploads/staff/" + fileName;
                }

                // ── Save ─────────────────────────────────────────────
                if (needsLogin)
                {
                    string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
                    await _adminRepo.AddStaffWithUserAsync(
                        firstName, lastName, phone, email, roleId, doj,
                        bankName, accountNumber, ifscCode, Salary, photoPath,
                        centerId, plantId, username, passwordHash);

                    TempData["Success"] = $"{roleName} added with login account successfully.";
                }
                else
                {
                    await _adminRepo.AddStaffAsync(
                        firstName, lastName, phone, email, roleId, doj,
                        bankName, accountNumber, ifscCode, Salary, photoPath,
                        centerId, plantId);

                    TempData["Success"] = "Staff member added successfully.";
                }

                return RedirectToAction("Staff");
            }
            catch (Exception ex)
            {
                var allRoles = _adminRepo.GetAllRoles();
                ViewBag.Error = ex.Message.Contains("Username already exists")
                    ? "That username is already taken. Please choose another."
                    : "Error: " + ex.Message;
                ReloadViewBag();
                return View(allRoles);
            }
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

        [SessionAuthorize("Admin")]
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
        [RequestSizeLimit(5 * 1024 * 1024)]
        [HttpPost]
        public async Task<IActionResult> EditStaff(int staffId, string firstName, string lastName,
    string phone, string email, int roleId, DateTime? doj, string bankName,
    string accountNumber, string ifscCode, decimal salary, string profilePhoto,
    IFormFile photoFile, int? centerId, int? plantId)
        {
            string finalPhoto = profilePhoto;

            if (photoFile != null && photoFile.Length > 0)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(photoFile.FileName);
                string filePath = Path.Combine(_env.WebRootPath, "uploads", "staff", fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await photoFile.CopyToAsync(stream);
                }
                finalPhoto = "/uploads/staff/" + fileName;
            }

            await _adminRepo.UpdateStaffAsync(staffId, firstName, lastName, phone, email,
                roleId, doj, bankName, accountNumber, ifscCode, salary,
                finalPhoto, centerId, plantId);

            TempData["Message"] = "Staff updated successfully.";
            return RedirectToAction("Staff");
        }


        // ════════════════════════════════════════════════════════
        // PLANT
        // ════════════════════════════════════════════════════════

        [SessionAuthorize("Admin")]
        [HttpGet]
        public ActionResult AddPlant()
        {
            ViewBag.States = _adminRepo.GetAllStates();
            ViewBag.City = _adminRepo.GetAllCities();
            return View();
        }

        [SessionAuthorize("Admin")]
        [HttpPost]
        public ActionResult AddPlant(string PlantName, string Location, string City, string State)
        {
            string loc = $"{Location}, {City}, {State}";
            _adminRepo.AddPlant(PlantName, loc);
            return RedirectToAction("GetAllPlants");
        }

        [SessionAuthorize("Admin")]
        [HttpGet]
        public ActionResult GetAllPlants()
        {
            var plants = _adminRepo.GetAllPlants(isActive: null);
            return View(plants);
        }

        private string BuildPlantsHtml(List<PlantModel> plants)
        {
            var sb = new StringBuilder();

            sb.Append(@"
    <html>
    <head>
        <style>
            body{font-family:Arial;}
            table{
                width:100%;
                border-collapse:collapse;
            }
            th,td{
                border:1px solid #ccc;
                padding:8px;
            }
        </style>
    </head>
    <body>
    ");

            sb.Append("<h2>Plants List</h2>");

            sb.Append(@"
    <table>
        <thead>
            <tr>
                <th>Plant</th>
                <th>Location</th>
                <th>Status</th>
            </tr>
        </thead>
        <tbody>
    ");

            foreach (var item in plants)
            {
                sb.Append($@"
        <tr>
            <td>{item.PlantName}</td>
            <td>{item.Location}</td>
            <td>{(item.IsActive ? "Active" : "Inactive")}</td>
        </tr>
        ");
            }

            sb.Append("</tbody></table></body></html>");

            return sb.ToString();
        }

        public IActionResult DownloadPlantsPdf()
        {
            var plants = _adminRepo.GetAllPlants()
                                   .Take(50)
                                   .ToList();

            string html = BuildPlantsHtml(plants);

            byte[] pdfBytes = GeneratePdfFromHtml(html);

            return File(
                pdfBytes,
                "application/pdf",
                $"PlantsList_{DateTime.Now:yyyyMMdd}.pdf"
            );
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

        private string BuildCollectionHtml(List<CollectionCenterModel> collections)
        {
            var sb = new StringBuilder();

            sb.Append(@"
    <html>
    <head>
        <style>
            body{
                font-family:Arial;
                font-size:12px;
            }

            table{
                width:100%;
                border-collapse:collapse;
            }

            th,td{
                border:1px solid #ccc;
                padding:8px;
                text-align:left;
            }

            th{
                background:#f2f2f2;
            }

            h2{
                text-align:center;
            }
        </style>
    </head>
    <body>
    ");

            sb.Append("<h2>Collection Centers List</h2>");

            sb.Append(@"
    <table>
        <thead>
            <tr>
                <th>Center Name</th>
                <th>Village</th>
                <th>Capacity</th>
                <th>Location</th>
                <th>Status</th>
            </tr>
        </thead>
        <tbody>
    ");

            foreach (var item in collections)
            {
                sb.Append($@"
        <tr>
            <td>{item.CenterName}</td>
            <td>{item.VillageName}</td>
            <td>{item.Capacity}</td>
            <td>{item.Location}</td>
            <td>{(item.IsActive ? "Active" : "Inactive")}</td>
        </tr>
        ");
            }

            sb.Append(@"
        </tbody>
    </table>
    </body>
    </html>
    ");

            return sb.ToString();
        }

        public IActionResult DownloadCollectionsPdf()
        {
            var collections = _adminRepo.GetAllCollection(true)
                                        .Take(50)
                                        .ToList();

            string html = BuildCollectionHtml(collections);

            byte[] pdfBytes = GeneratePdfFromHtml(html);

            return File(
                pdfBytes,
                "application/pdf",
                $"Collections_{DateTime.Now:yyyyMMdd}.pdf"
            );
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

        private string BuildDistributorHtml(List<Distributor> distributors)
        {
            var sb = new StringBuilder();

            sb.Append(@"
    <html>
    <head>
        <style>
            body{
                font-family:Arial;
                font-size:12px;
            }

            table{
                width:100%;
                border-collapse:collapse;
            }

            th,td{
                border:1px solid #ccc;
                padding:8px;
                text-align:left;
            }

            th{
                background:#f2f2f2;
            }

            h2{
                text-align:center;
            }
        </style>
    </head>
    <body>
    ");

            sb.Append("<h2>Distributors List</h2>");

            sb.Append(@"
    <table>
        <thead>
            <tr>
                <th>Distributor Name</th>
                <th>Phone</th>
                <th>Email</th>
                <th>Location</th>
                <th>Status</th>
            </tr>
        </thead>
        <tbody>
    ");

            foreach (var item in distributors)
            {
                sb.Append($@"
        <tr>
            <td>{item.DistributorName}</td>
            <td>{item.ContactNumber}</td>
            <td>{item.Email}</td>
            <td>{item.Location}</td>
            <td>{item.Status}</td>
        </tr>
        ");
            }

            sb.Append(@"
        </tbody>
    </table>
    </body>
    </html>
    ");

            return sb.ToString();
        }

        public IActionResult DownloadDistributorsPdf()
        {
            var distributors = _adminRepo.GetDistributors()
                                         .Take(50)
                                         .ToList();

            string html = BuildDistributorHtml(distributors);

            byte[] pdfBytes = GeneratePdfFromHtml(html);

            return File(
                pdfBytes,
                "application/pdf",
                $"Distributors_{DateTime.Now:yyyyMMdd}.pdf"
            );
        }

        [HttpGet]
        [SessionAuthorize("Admin")]
        public IActionResult RegisterDistributor()
        {
            return View();
        }

        [HttpPost]
        [SessionAuthorize("Admin")]
        public IActionResult RegisterDistributor(Distributor distributor, string username, string password)
        {
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            _adminRepo.RegisterDistributor(distributor, username, passwordHash);
            ViewBag.Success = "Registration submitted. Please wait for admin approval.";
            return View();
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
            // Validate cart has items
            if (model.CartItems == null || !model.CartItems.Any())
            {
                ModelState.AddModelError("", "Please add at least one product to the order.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    int orderId = _adminRepo.CreateOrder(model);
                    TempData["Success"] = $"Order #{orderId} created successfully!";
                    return RedirectToAction("AdminOrder");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            model.DistributorList = _adminRepo.GetDistributors();
            model.ProductList = _adminRepo.GetAllProducts(null, true);
            model.PlantList = _adminRepo.GetActivePlants();
            return View(model);
        }

        [HttpGet]
        [SessionAuthorize("Admin")]
        public IActionResult AdminOrderList(
    int? distributorId,
    string? orderStatus,
    DateTime? fromDate,
    DateTime? toDate)
        {
            var model = new AdminOrderListModel
            {
                DistributorId = distributorId,
                OrderStatus = orderStatus,
                FromDate = fromDate,
                ToDate = toDate,
                Orders = _adminRepo.GetAllOrders(distributorId, orderStatus, fromDate, toDate),
                DistributorList = _adminRepo.GetDistributors()
            };
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
        public IActionResult UpdateOrderStatus(
     int orderId,
     string status,
     int? distributorId,
     string? orderStatus,
     string? fromDate,
     string? toDate)
        {
            try
            {
                _adminRepo.UpdateOrderStatus(orderId, status);
                TempData["Message"] = $"Order #{orderId} status updated to {status}.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            // Redirect back to list preserving whatever filters were active
            return RedirectToAction("AdminOrderList", new
            {
                distributorId,
                orderStatus,
                fromDate,
                toDate
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

        //Finance

        [SessionAuthorize("Admin")]
        public IActionResult CenterPayments()
        {
            List<CenterPaymentModel> payments = _financeRepo.GetAllCenterPayments(plantId: null);
            return View(payments);
        }

        [SessionAuthorize("Admin")]
        public IActionResult CenterPaymentDetail(int id)
        {
            CenterPaymentModel payment = _financeRepo.GetCenterPaymentById(id);

            if (payment == null)
            {
                TempData["Error"] = $"Center payment #{id} was not found.";
                return RedirectToAction(nameof(CenterPayments));
            }

            return View(payment);
        }


    }
}