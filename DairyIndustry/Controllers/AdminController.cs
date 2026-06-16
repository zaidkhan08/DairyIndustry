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
        private readonly ICollectionCenterRepository _centerRepository;
        private readonly IFinanceRepository _financeRepo;
        private readonly IWebHostEnvironment _env;
        private readonly EmailSettings _settings;
        private readonly IAuthRepository _authRepo;
        private readonly IConverter _pdfConverter;

        public AdminController(IAdminRepository adminRepo, ILogisticsRepository logisticsRepo,
            IReportRepository reportRepo, IWebHostEnvironment env, IFinanceRepository financeRepo,
            IAuthRepository authRepo, IOptions<EmailSettings> settings, IConverter pdfConverter,
            ICollectionCenterRepository centerRepository)
        {
            _adminRepo = adminRepo;
            _logisticsRepo = logisticsRepo;
            _reportRepo = reportRepo;
            _financeRepo = financeRepo;
            _env = env;
            _authRepo = authRepo;
            _settings = settings.Value;
            _pdfConverter = pdfConverter;
            _centerRepository = centerRepository;
        }

        // ════════════════════════════════════════════════════════
        // LOGIN — NO [SessionAuthorize] here
        // ════════════════════════════════════════════════════════

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
                    return RedirectToAction("Dashboard", "CollectionCenter");

                default:
                    return RedirectToAction("Login", "Admin");
            }
        }

        public IActionResult Profile(int? userId)
        {
            int id;

            if (userId.HasValue)
            {
                id = userId.Value;
            }
            else
            {
                var sessionUserId = HttpContext.Session.GetInt32("UserId");

                if (sessionUserId == null)
                    return RedirectToAction("Login");

                id = sessionUserId.Value;
            }

            var user = _adminRepo.GetUserProfile(id);

            if (user == null)
            {
                TempData["Error"] = "User profile not found.";
                return RedirectToAction("Index");
            }

            return View(user);
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            // Basic input validation
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Username and password are required.";
                return View();
            }

            try
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
                string cookieName = $"DMS_TD_{user.Username}";
                string deviceToken = Request.Cookies[cookieName];

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
            catch (Exception ex)
            {
                ViewBag.Error = "An error occurred during login. Please try again.";
                return View();
            }
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

            try
            {
                // Get real email from DB since TempData has masked version
                var user = _adminRepo.GetUserByUsername(username);
                if (user == null)
                {
                    TempData["Error"] = "User not found. Please log in again.";
                    return RedirectToAction("Login");
                }

                var otp = _authRepo.GenerateOtp(userId, "Login");
                _adminRepo.SendOtpEmail(user.Email, fullName, otp, "Login");

                // Keep TempData alive
                TempData["OtpUserId"] = userId;
                TempData["OtpUsername"] = username;
                TempData["OtpFullName"] = fullName;
                TempData["OtpEmail"] = email;

                TempData["Success"] = "OTP resent successfully.";
            }
            catch (Exception ex)
            {
                // Keep TempData alive so user stays on OTP page
                TempData["OtpUserId"] = userId;
                TempData["OtpUsername"] = username;
                TempData["OtpFullName"] = fullName;
                TempData["OtpEmail"] = email;
                TempData["Error"] = "Failed to resend OTP. Please try again.";
            }

            return RedirectToAction("VerifyOtp");
        }

        [HttpGet]
        public IActionResult VerifyOtp()
        {
            if (TempData["OtpUserId"] == null)
                return RedirectToAction("Login");

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

            if (string.IsNullOrWhiteSpace(otpCode))
            {
                TempData.Keep();
                ViewBag.MaskedEmail = TempData["OtpEmail"];
                ViewBag.FullName = TempData["OtpFullName"];
                ViewBag.Error = "Please enter the OTP.";
                return View();
            }

            int userId = Convert.ToInt32(TempData["OtpUserId"]);
            string username = TempData["OtpUsername"]?.ToString();

            try
            {
                // Validate OTP
                var result = _authRepo.ValidateOtp(userId, otpCode, "Login");

                if (!result.IsValid)
                {
                    TempData.Keep();
                    ViewBag.MaskedEmail = TempData["OtpEmail"];
                    ViewBag.FullName = TempData["OtpFullName"];
                    ViewBag.Error = result.Reason;
                    return View();
                }

                // OTP valid — get full user to set session
                var user = _adminRepo.GetUserByUsername(username);
                if (user == null)
                {
                    TempData["Error"] = "User not found. Please log in again.";
                    return RedirectToAction("Login");
                }

                // Remember this device?
                if (rememberDevice)
                {
                    var newDeviceToken = Guid.NewGuid().ToString();
                    var deviceName = Request.Headers["User-Agent"].ToString();

                    _authRepo.RegisterTrustedDevice(userId, newDeviceToken, deviceName);

                    string cookieName = $"DMS_TD_{user.Username}";
                    Response.Cookies.Append(cookieName, newDeviceToken, new CookieOptions
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
            catch (Exception ex)
            {
                TempData.Keep();
                ViewBag.MaskedEmail = TempData["OtpEmail"];
                ViewBag.FullName = TempData["OtpFullName"];
                ViewBag.Error = "An error occurred during verification. Please try again.";
                return View();
            }
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // ════════════════════════════════════════════════════
        // GET — Change Password Page
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

            // 1. Validate inputs
            if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                ViewBag.Error = "All fields are required.";
                return View();
            }

            // 2. Validate new password match
            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "New passwords do not match.";
                return View();
            }

            // 3. Validate password length
            if (newPassword.Length < 3)
            {
                ViewBag.Error = "Password must be at least 3 characters.";
                return View();
            }

            try
            {
                // 4. Get user and verify current password
                string username = HttpContext.Session.GetString("Username");
                var user = _adminRepo.GetUserByUsername(username);

                if (user == null)
                {
                    ViewBag.Error = "User not found. Please log in again.";
                    return View();
                }

                bool isCurrentValid = BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash);
                if (!isCurrentValid)
                {
                    ViewBag.Error = "Current password is incorrect.";
                    return View();
                }

                // 5. Check user has email for OTP
                if (string.IsNullOrEmpty(user.Email))
                {
                    // No email — change directly without OTP
                    var hash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                    _adminRepo.ChangePassword(userId.Value, hash);
                    TempData["Success"] = "Password changed successfully.";
                    return RedirectToAction("ChangePassword");
                }

                // 6. Generate and send OTP
                var otp = _authRepo.GenerateOtp(userId.Value, "ChangePassword");
                _adminRepo.SendOtpEmail(user.Email, user.FullName ?? user.Username, otp, "ChangePassword");

                // 7. Store new password hash in TempData
                TempData["CpUserId"] = userId.Value;
                TempData["CpNewHash"] = BCrypt.Net.BCrypt.HashPassword(newPassword);
                TempData["CpMaskedEmail"] = MaskEmail(user.Email);

                return RedirectToAction("VerifyChangePasswordOtp");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "An error occurred. Please try again.";
                return View();
            }
        }

        [HttpGet]
        public IActionResult VerifyChangePasswordOtp()
        {
            if (TempData["CpUserId"] == null)
                return RedirectToAction("ChangePassword");

            TempData.Keep();
            ViewBag.MaskedEmail = TempData["CpMaskedEmail"];
            return View();
        }

        [HttpPost]
        public IActionResult VerifyChangePasswordOtp(string otpCode)
        {
            if (TempData["CpUserId"] == null)
                return RedirectToAction("ChangePassword");

            if (string.IsNullOrWhiteSpace(otpCode))
            {
                TempData.Keep();
                ViewBag.MaskedEmail = TempData["CpMaskedEmail"];
                ViewBag.Error = "Please enter the OTP.";
                return View();
            }

            int userId = Convert.ToInt32(TempData["CpUserId"]);
            string newHash = TempData["CpNewHash"]?.ToString();

            try
            {
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

                // Revoke all trusted devices
                _authRepo.RevokeAllTrustedDevices(userId);

                // Clear the trusted device cookie on this device too
                string username = HttpContext.Session.GetString("Username");
                if (!string.IsNullOrEmpty(username))
                    Response.Cookies.Delete($"DMS_TD_{username}");

                // Clear session — force re-login
                HttpContext.Session.Clear();

                TempData["Success"] = "Password changed successfully. Please log in again.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                TempData.Keep();
                ViewBag.MaskedEmail = TempData["CpMaskedEmail"];
                ViewBag.Error = "An error occurred. Please try again.";
                return View();
            }
        }

        [HttpGet]
        public IActionResult ResendChangePasswordOtp()
        {
            if (TempData["CpUserId"] == null)
                return RedirectToAction("ChangePassword");

            int userId = Convert.ToInt32(TempData["CpUserId"]);
            string username = HttpContext.Session.GetString("Username");

            try
            {
                var user = _adminRepo.GetUserByUsername(username);
                if (user == null)
                {
                    TempData["Error"] = "User not found. Please log in again.";
                    return RedirectToAction("Login");
                }

                var otp = _authRepo.GenerateOtp(userId, "ChangePassword");
                _adminRepo.SendOtpEmail(user.Email, user.FullName ?? user.Username, otp, "ChangePassword");

                TempData.Keep();
                TempData["Success"] = "OTP resent successfully.";
            }
            catch (Exception ex)
            {
                TempData.Keep();
                TempData["Error"] = "Failed to resend OTP. Please try again.";
            }

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

            try
            {
                var devices = _authRepo.GetTrustedDevices(userId.Value);
                return View(devices);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load settings. Please try again.";
                return View(new List<object>());
            }
        }

        [HttpPost]
        public IActionResult RevokeDevice(int deviceId)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");

            if (deviceId <= 0)
            {
                TempData["Error"] = "Invalid device.";
                return RedirectToAction("Settings");
            }

            try
            {
                var devices = _authRepo.GetTrustedDevices(userId.Value);
                var device = devices.FirstOrDefault(d => d.DeviceId == deviceId);

                _authRepo.RevokeDeviceById(userId.Value, deviceId);

                if (device != null)
                {
                    string cookieName = $"DMS_TD_{HttpContext.Session.GetString("Username")}";
                    string currentToken = Request.Cookies[cookieName];
                    if (currentToken == device.DeviceToken)
                        Response.Cookies.Delete(cookieName);
                }

                TempData["Success"] = "Device removed successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to remove device. Please try again.";
            }

            return RedirectToAction("Settings");
        }

        [HttpPost]
        public IActionResult RevokeAllDevices()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");

            try
            {
                _authRepo.RevokeAllTrustedDevices(userId.Value);

                string username = HttpContext.Session.GetString("Username");
                if (!string.IsNullOrEmpty(username))
                    Response.Cookies.Delete($"DMS_TD_{username}");

                TempData["Success"] = "All trusted devices removed.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to remove devices. Please try again.";
            }

            return RedirectToAction("Settings");
        }

        // ════════════════════════════════════════════════════════
        // DASHBOARD
        // ════════════════════════════════════════════════════════

        [SessionAuthorize("Admin")]
        public IActionResult Index()
        {
            try
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
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load dashboard data. Please refresh the page.";
                return View(new AdminDashboardViewModel());
            }
        }

        // ════════════════════════════════════════════════════════
        // ROLES
        // ════════════════════════════════════════════════════════

        [SessionAuthorize("Admin")]
        public IActionResult Roles(int page = 1, int pageSize = 5)
        {
            try
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
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load roles. Please try again.";
                return View(new List<RoleModel>());
            }
        }

        [SessionAuthorize("Admin")]
        [HttpPost]
        public IActionResult CreateRole(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                TempData["Error"] = "Role name is required.";
                return RedirectToAction("Roles");
            }

            try
            {
                _adminRepo.CreateRole(roleName.Trim());
                TempData["Success"] = $"Role '{roleName}' created successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message.Contains("duplicate") || ex.Message.Contains("Duplicate")
                    ? $"Role '{roleName}' already exists."
                    : "Failed to create role. Please try again.";
            }

            return RedirectToAction("Roles");
        }

        // ════════════════════════════════════════════════════════
        // USERS
        // ════════════════════════════════════════════════════════

        [SessionAuthorize("Admin")]
        public IActionResult Users(int page = 1, int pageSize = 10)
        {
            try
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
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load users. Please try again.";
                return View(new List<User>());
            }
        }

        private string BuildUsersHtml(List<User> users)
        {
            string docNo = $"USR-{DateTime.Now:yyyyMMdd}";
            int active = users.Count(u => u.IsActive);
            int inactive = users.Count(u => !u.IsActive);

            var sb = new StringBuilder();
            sb.Append($"<!DOCTYPE html><html><head><meta charset='utf-8'/><style>{PdfStyles()}</style></head><body><div class='page'>");
            sb.Append(PdfHeader("USERS LIST", "System User Accounts Report", docNo));

            // Summary boxes
            sb.Append("<div class='summary-row'>");
            sb.Append($"<div class='summary-box'><div class='lbl'>Total Users</div><div class='val'>{users.Count}</div><div class='sub'>All registered accounts</div></div>");
            sb.Append($"<div class='summary-box'><div class='lbl'>Active</div><div class='val'>{active}</div><div class='sub'>Currently active</div></div>");
            sb.Append($"<div class='summary-box'><div class='lbl'>Inactive</div><div class='val'>{inactive}</div><div class='sub'>Deactivated accounts</div></div>");
            sb.Append("</div>");

            // Table
            sb.Append("<table><thead><tr>");
            sb.Append("<th style='width:36px' class='c'>#</th><th>Username</th><th>Full Name</th><th>Role</th><th>Created Date</th><th class='c'>Status</th>");
            sb.Append("</tr></thead><tbody>");

            int i = 1;
            foreach (var u in users)
            {
                string badge = u.IsActive
                    ? "<span class='badge badge-active'>Active</span>"
                    : "<span class='badge badge-inactive'>Inactive</span>";

                sb.Append($@"<tr>
    <td class='c' style='color:#777'>{i++}</td>
    <td><strong>{u.Username}</strong></td>
    <td>{u.FullName ?? "—"}</td>
    <td>{u.RoleName}</td>
    <td>{u.CreatedDate.ToString("dd MMM yyyy")}</td>
    <td class='c'>{badge}</td>
</tr>");
            }

            sb.Append("</tbody></table>");
            sb.Append(PdfFooter(docNo));
            sb.Append("</div></body></html>");
            return sb.ToString();
        }
        public async Task<IActionResult> DownloadUsersPdf()
        {
            try
            {
                var userList = _adminRepo.GetAllUsers()
                    .Take(50)
                    .ToList();

                string html = BuildUsersHtml(userList);
                byte[] pdfBytes = await GeneratePdfFromHtmlAsync(html);

                return File(
                    pdfBytes,
                    "application/pdf",
                    $"UserList_{DateTime.Now:yyyyMMdd}.pdf"
                );
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to generate PDF. Please try again.";
                return RedirectToAction("Users");
            }
        }

        [SessionAuthorize("Admin")]
        [HttpGet]
        public IActionResult RegisterUser()
        {
            try
            {
                ViewBag.StaffList = _adminRepo.GetUnlinkedStaff();
                return View();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load registration form. Please try again.";
                return RedirectToAction("Users");
            }
        }

        [SessionAuthorize("Admin")]
        [HttpPost]
        public IActionResult RegisterUser(string username, string password, int staffId)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Username and password are required.";
                ViewBag.StaffList = _adminRepo.GetUnlinkedStaff();
                return View();
            }

            if (staffId <= 0)
            {
                ViewBag.Error = "Please select a staff member.";
                ViewBag.StaffList = _adminRepo.GetUnlinkedStaff();
                return View();
            }

            try
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
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message.Contains("Username already exists") || ex.Message.Contains("duplicate")
                    ? "That username is already taken. Please choose another."
                    : "Failed to create user. Please try again.";
                ViewBag.StaffList = _adminRepo.GetUnlinkedStaff();
                return View();
            }
        }

        [SessionAuthorize("Admin")]
        [HttpPost]
        public IActionResult UpdateUserStatus(int userId, bool isActive)
        {
            if (userId <= 0)
            {
                TempData["Error"] = "Invalid user.";
                return RedirectToAction("Users");
            }

            try
            {
                _adminRepo.UpdateUserStatus(userId, isActive);
                TempData["Success"] = isActive ? "User activated." : "User deactivated.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to update user status. Please try again.";
            }

            return RedirectToAction("Users");
        }

        // ─── ASSIGN USER TO PLANT ───────────────────────────────────────────

        [SessionAuthorize("Admin")]
        [HttpGet]
        public IActionResult AssignUserToPlant()
        {
            ViewBag.Users = _adminRepo.GetPlantManagers();        // 🔴 changed
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
                ViewBag.Users = _adminRepo.GetPlantManagers();    // 🔴 changed
                ViewBag.Plants = _adminRepo.GetAllPlants();
                ViewBag.Assignments = _adminRepo.GetAllUserPlantAssignments();
                return View();
            }
            try
            {
                _adminRepo.AssignUserToPlant(userId, plantId);
                TempData["Success"] = "User assigned to plant successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to assign user to plant. Please try again.";
            }
            return RedirectToAction("AssignUserToPlant");
        }

        // ─── ASSIGN USER TO CENTER ──────────────────────────────────────────

        [SessionAuthorize("Admin")]
        [HttpGet]
        public IActionResult AssignUserToCenter()
        {
            ViewBag.Users = _adminRepo.GetCollectionAgents();
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
                ViewBag.Users = _adminRepo.GetCollectionAgents();
                ViewBag.Centers = _adminRepo.GetAllCenters();
                ViewBag.Assignments = _adminRepo.GetAllUserCenterAssignments();
                return View();
            }
            try
            {
                _adminRepo.AssignUserToCenter(userId, centerId);
                TempData["Success"] = "User assigned to center successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to assign user to center. Please try again.";
            }
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
            try
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
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load audit logs. Please try again.";
                return View(new List<AuditLogModel>());
            }
        }

        [SessionAuthorize("Admin")]
        public IActionResult Location()
        {
            try
            {
                ViewBag.States = _adminRepo.GetAllStates();
                ViewBag.Cities = _adminRepo.GetAllCities();
                ViewBag.Villages = _adminRepo.GetAllVillages();
                return View();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load location data. Please try again.";
                return View();
            }
        }

        [SessionAuthorize("Admin")]
        [HttpPost]
        public IActionResult AddState(string stateName)
        {
            if (string.IsNullOrWhiteSpace(stateName))
            {
                TempData["Error"] = "State name is required.";
                TempData["ActiveTab"] = "state";
                return RedirectToAction("Location");
            }

            try
            {
                _adminRepo.AddState(stateName.Trim());
                TempData["Success"] = $"State '{stateName}' added successfully.";
                TempData["ActiveTab"] = "state";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message.Contains("duplicate") || ex.Message.Contains("Duplicate")
                    ? $"State '{stateName}' already exists."
                    : "Failed to add state. Please try again.";
                TempData["ActiveTab"] = "state";
            }

            return RedirectToAction("Location");
        }

        [SessionAuthorize("Admin")]
        [HttpPost]
        public IActionResult AddCity(string cityName, int stateId)
        {
            if (string.IsNullOrWhiteSpace(cityName))
            {
                TempData["Error"] = "City name is required.";
                TempData["ActiveTab"] = "city";
                return RedirectToAction("Location");
            }

            if (stateId <= 0)
            {
                TempData["Error"] = "Please select a state.";
                TempData["ActiveTab"] = "city";
                return RedirectToAction("Location");
            }

            try
            {
                _adminRepo.AddCity(cityName.Trim(), stateId);
                TempData["Success"] = $"City '{cityName}' added successfully.";
                TempData["ActiveTab"] = "city";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message.Contains("duplicate") || ex.Message.Contains("Duplicate")
                    ? $"City '{cityName}' already exists in the selected state."
                    : "Failed to add city. Please try again.";
                TempData["ActiveTab"] = "city";
            }

            return RedirectToAction("Location");
        }

        [SessionAuthorize("Admin")]
        [HttpPost]
        public IActionResult AddVillage(string villageName, int cityId)
        {
            if (string.IsNullOrWhiteSpace(villageName))
            {
                TempData["Error"] = "Village name is required.";
                TempData["ActiveTab"] = "village";
                return RedirectToAction("Location");
            }

            if (cityId <= 0)
            {
                TempData["Error"] = "Please select a city.";
                TempData["ActiveTab"] = "village";
                return RedirectToAction("Location");
            }

            try
            {
                _adminRepo.AddVillage(villageName.Trim(), cityId);
                TempData["Success"] = $"Village '{villageName}' added successfully.";
                TempData["ActiveTab"] = "village";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message.Contains("duplicate") || ex.Message.Contains("Duplicate")
                    ? $"Village '{villageName}' already exists in the selected city."
                    : "Failed to add village. Please try again.";
                TempData["ActiveTab"] = "village";
            }

            return RedirectToAction("Location");
        }

        [SessionAuthorize("Admin")]
        public IActionResult GetCitiesByState(int stateId)
        {
            try
            {
                var cities = _adminRepo.GetCitiesByState(stateId);
                return Json(cities);
            }
            catch (Exception ex)
            {
                return Json(new List<object>());
            }
        }

        [SessionAuthorize("Admin")]
        public IActionResult GetVillagesByCity(int cityId)
        {
            try
            {
                var villages = _adminRepo.GetVillagesByCity(cityId);
                return Json(villages);
            }
            catch (Exception ex)
            {
                return Json(new List<object>());
            }
        }

        // ════════════════════════════════════════════════════════
        // MILK TYPES
        // ════════════════════════════════════════════════════════

        [SessionAuthorize("Admin")]
        public IActionResult MilkTypes()
        {
            try
            {
                var milkTypes = _adminRepo.GetAllMilkTypes();
                return View(milkTypes);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load milk types. Please try again.";
                return View(new List<MilkTypeModel>());
            }
        }

        [SessionAuthorize("Admin")]
        [HttpPost]
        public IActionResult AddMilkType(string milkTypeName)
        {
            if (string.IsNullOrWhiteSpace(milkTypeName))
            {
                TempData["Error"] = "Milk type name is required.";
                return RedirectToAction("MilkTypes");
            }

            try
            {
                _adminRepo.AddMilkType(milkTypeName.Trim());
                TempData["Success"] = $"Milk type '{milkTypeName}' added successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message.Contains("duplicate") || ex.Message.Contains("Duplicate")
                    ? $"Milk type '{milkTypeName}' already exists."
                    : "Failed to add milk type. Please try again.";
            }

            return RedirectToAction("MilkTypes");
        }

        // ════════════════════════════════════════════════════════
        // RATE CHART
        // ════════════════════════════════════════════════════════

        [SessionAuthorize("Admin")]
        public IActionResult RateChart()
        {
            try
            {
                var rateCharts = _adminRepo.GetAllRateCharts();
                ViewBag.MilkTypes = _adminRepo.GetAllMilkTypes();
                return View(rateCharts);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load rate charts. Please try again.";
                ViewBag.MilkTypes = new List<MilkTypeModel>();
                return View(new List<RateChartModel>());
            }
        }

        [SessionAuthorize("Admin")]
        [HttpPost]
        public IActionResult AddRateChart(int milkTypeId, decimal fatFrom, decimal fatTo,
                                          decimal clrFrom, decimal clrTo,
                                          decimal ratePerLiter, DateTime effectiveFrom)
        {
            if (milkTypeId <= 0)
            {
                TempData["Error"] = "Please select a milk type.";
                return RedirectToAction("RateChart");
            }

            if (fatFrom >= fatTo)
            {
                TempData["Error"] = "Fat From must be less than Fat To.";
                return RedirectToAction("RateChart");
            }

            if (clrFrom >= clrTo)
            {
                TempData["Error"] = "CLR From must be less than CLR To.";
                return RedirectToAction("RateChart");
            }

            if (ratePerLiter <= 0)
            {
                TempData["Error"] = "Rate per liter must be greater than zero.";
                return RedirectToAction("RateChart");
            }

            try
            {
                _adminRepo.AddRateChart(milkTypeId, fatFrom, fatTo, clrFrom, clrTo, ratePerLiter, effectiveFrom);
                TempData["Success"] = "Rate chart added successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to add rate chart. Please try again.";
            }

            return RedirectToAction("RateChart");
        }

        // ════════════════════════════════════════════════════════
        // STAFF
        // ════════════════════════════════════════════════════════

        [SessionAuthorize("Admin")]
        public IActionResult Staff(int page = 1, int pageSize = 10)
        {
            try
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
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load staff list. Please try again.";
                return View(new List<StaffModel>());
            }
        }

        [SessionAuthorize("Admin")]
        private string BuildStaffHtml(List<StaffModel> staff)
        {
            string docNo = $"STF-{DateTime.Now:yyyyMMdd}";
            int active = staff.Count(s => s.IsActive);
            int inactive = staff.Count(s => !s.IsActive);

            var sb = new StringBuilder();
            sb.Append($"<!DOCTYPE html><html><head><meta charset='utf-8'/><style>{PdfStyles()}</style></head><body><div class='page'>");
            sb.Append(PdfHeader("STAFF LIST", "Human Resources — Staff Report", docNo));

            sb.Append("<div class='summary-row'>");
            sb.Append($"<div class='summary-box'><div class='lbl'>Total Staff</div><div class='val'>{staff.Count}</div><div class='sub'>All staff members</div></div>");
            sb.Append($"<div class='summary-box'><div class='lbl'>Active</div><div class='val'>{active}</div><div class='sub'>Currently active</div></div>");
            sb.Append($"<div class='summary-box'><div class='lbl'>Inactive</div><div class='val'>{inactive}</div><div class='sub'>Deactivated</div></div>");
            sb.Append("</div>");

            sb.Append("<table><thead><tr>");
            sb.Append("<th style='width:36px' class='c'>#</th><th>Name</th><th>Role</th><th>Phone</th><th>Email</th><th>Center / Plant</th><th>Salary (₹)</th><th class='c'>Status</th>");
            sb.Append("</tr></thead><tbody>");

            int i = 1;
            foreach (var s in staff)
            {
                string badge = s.IsActive
                    ? "<span class='badge badge-active'>Active</span>"
                    : "<span class='badge badge-inactive'>Inactive</span>";

                string assignment = s.CenterName != null ? $"Center: {s.CenterName}"
                                  : s.PlantName != null ? $"Plant: {s.PlantName}"
                                  : "—";

                sb.Append($@"<tr>
            <td class='c' style='color:#777'>{i++}</td>
            <td><strong>{s.FirstName} {s.LastName}</strong></td>
            <td>{s.RoleName}</td>
            <td>{s.Phone ?? "—"}</td>
            <td>{s.Email ?? "—"}</td>
            <td>{assignment}</td>
            <td class='r'>{s.Salary:N2}</td>
            <td class='c'>{badge}</td>
        </tr>");
            }

            sb.Append("</tbody></table>");
            sb.Append(PdfFooter(docNo));
            sb.Append("</div></body></html>");
            return sb.ToString();
        }
        private Task<byte[]> GeneratePdfFromHtmlAsync(string html)
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

            // Task.Run offloads the blocking native DinkToPdf call off the
            // ASP.NET synchronization context, preventing thread-pool deadlocks.
            return Task.Run(() => _pdfConverter.Convert(doc));
        }

        public async Task<IActionResult> DownloadStaffPdf()
        {
            try
            {
                var staffList = _adminRepo.GetAllStaff()
                    .Take(50)
                    .ToList();

                string html = BuildStaffHtml(staffList);
                byte[] pdfBytes = await GeneratePdfFromHtmlAsync(html);

                return File(
                    pdfBytes,
                    "application/pdf",
                    $"StaffList_{DateTime.Now:yyyyMMdd}.pdf"
                );
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to generate PDF. Please try again.";
                return RedirectToAction("Staff");
            }
        }

        [SessionAuthorize("Admin")]
        [HttpGet]
        public IActionResult AddStaff()
        {
            try
            {
                ViewBag.Plants = _adminRepo.GetAllPlants();
                ViewBag.Centers = _adminRepo.GetAllCenters();
                return View(_adminRepo.GetAllRoles());
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load form data. Please try again.";
                return RedirectToAction("Staff");
            }
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

            // Input validation
            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            {
                ViewBag.Error = "First name and last name are required.";
                ReloadViewBag();
                return View(_adminRepo.GetAllRoles());
            }

            if (roleId <= 0)
            {
                ViewBag.Error = "Please select a role.";
                ReloadViewBag();
                return View(_adminRepo.GetAllRoles());
            }

            try
            {
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

                    // Direct FileStream copy — avoids MemoryStream double-buffer
                    // and prevents async deadlocks on the upload path.
                    using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                    {
                        await profilePhoto.CopyToAsync(fileStream);
                    }
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
            if (id <= 0)
            {
                TempData["Error"] = "Invalid staff ID.";
                return RedirectToAction("Staff");
            }

            try
            {
                var staff = _adminRepo.GetStaffById(id);
                if (staff == null)
                {
                    TempData["Error"] = "Staff not found.";
                    return RedirectToAction("Staff");
                }
                return View(staff);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load staff details. Please try again.";
                return RedirectToAction("Staff");
            }
        }

        [SessionAuthorize("Admin")]
        [HttpPost]
        public IActionResult ToggleStaffActive(int staffId, int isActive)
        {
            if (staffId <= 0)
            {
                TempData["Error"] = "Invalid staff ID.";
                return RedirectToAction("Staff");
            }

            try
            {
                _adminRepo.ToggleStaffActive(staffId, isActive == 1);
                TempData["Success"] = isActive == 1 ? "Staff activated." : "Staff deactivated.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to update staff status. Please try again.";
            }

            return RedirectToAction("Staff");
        }

        [HttpGet]
        [SessionAuthorize("Admin")]
        public IActionResult EditStaff(int id)
        {
            if (id <= 0)
                return NotFound();

            try
            {
                var staff = _adminRepo.GetStaffById(id);
                if (staff == null) return NotFound();

                ViewBag.Roles = _adminRepo.GetAllRoles();
                ViewBag.Centers = _adminRepo.GetAllCenters();
                ViewBag.Plants = _adminRepo.GetAllPlants();

                return View(staff);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load staff details. Please try again.";
                return RedirectToAction("Staff");
            }
        }

        [HttpPost]
        [SessionAuthorize("Admin")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> EditStaff(
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
    string existingPhoto,
    IFormFile photoFile,
    int? centerId,
    int? plantId,

    // Login
    string username,
    string password)
        {
            void ReloadViewBag()
            {
                ViewBag.Roles = _adminRepo.GetAllRoles();
                ViewBag.Centers = _adminRepo.GetAllCenters();
                ViewBag.Plants = _adminRepo.GetAllPlants();
            }

            if (string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(lastName))
            {
                ViewBag.Error = "First name and last name are required.";
                ReloadViewBag();
                return View(_adminRepo.GetStaffById(staffId));
            }

            try
            {
                var allRoles = _adminRepo.GetAllRoles();

                var selectedRole = allRoles.FirstOrDefault(r => r.RoleId == roleId);

                string roleName = selectedRole?.RoleName ?? "";

                // Cannot assign both
                if (centerId.HasValue && plantId.HasValue)
                {
                    ViewBag.Error = "Assign staff to either a Collection Center or a Plant — not both.";
                    ReloadViewBag();
                    return View(_adminRepo.GetStaffById(staffId));
                }

                // Plant Manager
                if (roleName == "Plant Manager" && !plantId.HasValue)
                {
                    ViewBag.Error = "Plant Manager must be assigned to a Plant.";
                    ReloadViewBag();
                    return View(_adminRepo.GetStaffById(staffId));
                }

                // Collection Agent
                if (roleName == "Collection Agent" && !centerId.HasValue)
                {
                    ViewBag.Error = "Collection Agent must be assigned to a Collection Center.";
                    ReloadViewBag();
                    return View(_adminRepo.GetStaffById(staffId));
                }

                // Login Roles
                var loginRoles = new[]
                {
            "Administrator",
            "Plant Manager",
            "Collection Agent",
            "HR Manager"
        };

                bool needsLogin = loginRoles.Contains(roleName);

                if (needsLogin && string.IsNullOrWhiteSpace(username))
                {
                    ViewBag.Error = $"{roleName} requires a username.";
                    ReloadViewBag();
                    return View(_adminRepo.GetStaffById(staffId));
                }

                // Upload Photo
                string finalPhoto = existingPhoto;

                if (photoFile != null && photoFile.Length > 0)
                {
                    var extension = Path.GetExtension(photoFile.FileName).ToLowerInvariant();

                    if (extension != ".jpg" &&
                        extension != ".jpeg" &&
                        extension != ".png")
                    {
                        ViewBag.Error = "Only .jpg, .jpeg and .png files are allowed.";
                        ReloadViewBag();
                        return View(_adminRepo.GetStaffById(staffId));
                    }

                    if (photoFile.Length > 2 * 1024 * 1024)
                    {
                        ViewBag.Error = "Maximum photo size is 2 MB.";
                        ReloadViewBag();
                        return View(_adminRepo.GetStaffById(staffId));
                    }

                    string uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "staff");

                    Directory.CreateDirectory(uploadFolder);

                    string fileName = Guid.NewGuid() + extension;

                    string filePath = Path.Combine(uploadFolder, fileName);

                    using (var stream = new FileStream(
                        filePath,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None,
                        4096,
                        true))
                    {
                        await photoFile.CopyToAsync(stream);
                    }

                    finalPhoto = "/uploads/staff/" + fileName;
                }

                // Password Hash
                string passwordHash = null;

                if (!string.IsNullOrWhiteSpace(password))
                {
                    passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
                }

                // Save
                if (needsLogin)
                {
                    await _adminRepo.UpdateStaffWithUserAsync(
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
                        plantId,
                        username,
                        passwordHash);

                    TempData["Success"] = $"{roleName} updated successfully.";
                }
                else
                {
                    await _adminRepo.UpdateStaffAsync(
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
                        plantId);

                    TempData["Success"] = "Staff updated successfully.";
                }

                return RedirectToAction("Staff");
            }
            catch (Exception ex)
            {
                ReloadViewBag();

                ViewBag.Error = ex.Message.Contains("Username already exists")
                    ? "That username is already taken. Please choose another."
                    : "Error: " + ex.Message;

                return View(_adminRepo.GetStaffById(staffId));
            }
        }

        // ════════════════════════════════════════════════════════
        // PLANT
        // ════════════════════════════════════════════════════════

        [SessionAuthorize("Admin")]
        [HttpGet]
        public ActionResult AddPlant()
        {
            try
            {
                ViewBag.States = _adminRepo.GetAllStates();
                ViewBag.City = _adminRepo.GetAllCities();
                return View();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load form data. Please try again.";
                return RedirectToAction("GetAllPlants");
            }
        }

        [SessionAuthorize("Admin")]
        [HttpPost]
        public ActionResult AddPlant(string PlantName, string Location, string City, string State)
        {
            if (string.IsNullOrWhiteSpace(PlantName))
            {
                ViewBag.Error = "Plant name is required.";
                ViewBag.States = _adminRepo.GetAllStates();
                ViewBag.City = _adminRepo.GetAllCities();
                return View();
            }

            try
            {
                string loc = $"{Location}, {City}, {State}";
                _adminRepo.AddPlant(PlantName.Trim(), loc);
                TempData["Success"] = $"Plant '{PlantName}' added successfully.";
                return RedirectToAction("GetAllPlants");
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message.Contains("duplicate") || ex.Message.Contains("Duplicate")
                    ? $"Plant '{PlantName}' already exists."
                    : "Failed to add plant. Please try again.";
                ViewBag.States = _adminRepo.GetAllStates();
                ViewBag.City = _adminRepo.GetAllCities();
                return View();
            }
        }

        [SessionAuthorize("Admin")]
        [HttpGet]
        public ActionResult GetAllPlants()
        {
            try
            {
                var plants = _adminRepo.GetAllPlants(isActive: null);
                return View(plants);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load plants. Please try again.";
                return View(new List<PlantModel>());
            }
        }
        private string BuildPlantsHtml(List<PlantModel> plants)
        {
            string docNo = $"PLT-{DateTime.Now:yyyyMMdd}";
            int active = plants.Count(p => p.IsActive);
            int inactive = plants.Count(p => !p.IsActive);

            var sb = new StringBuilder();
            sb.Append($"<!DOCTYPE html><html><head><meta charset='utf-8'/><style>{PdfStyles()}</style></head><body><div class='page'>");
            sb.Append(PdfHeader("PLANTS LIST", "Processing Plants Report", docNo));

            sb.Append("<div class='summary-row'>");
            sb.Append($"<div class='summary-box'><div class='lbl'>Total Plants</div><div class='val'>{plants.Count}</div><div class='sub'>All processing plants</div></div>");
            sb.Append($"<div class='summary-box'><div class='lbl'>Active</div><div class='val'>{active}</div><div class='sub'>Operational</div></div>");
            sb.Append($"<div class='summary-box'><div class='lbl'>Inactive</div><div class='val'>{inactive}</div><div class='sub'>Deactivated</div></div>");
            sb.Append("</div>");

            sb.Append("<table><thead><tr>");
            sb.Append("<th style='width:36px' class='c'>#</th><th>Plant Name</th><th>Location</th><th class='c'>Status</th>");
            sb.Append("</tr></thead><tbody>");

            int i = 1;
            foreach (var p in plants)
            {
                string badge = p.IsActive
                    ? "<span class='badge badge-active'>Active</span>"
                    : "<span class='badge badge-inactive'>Inactive</span>";

                sb.Append($@"<tr>
            <td class='c' style='color:#777'>{i++}</td>
            <td><strong>{p.PlantName}</strong></td>
            <td>{p.Location ?? "—"}</td>
            <td class='c'>{badge}</td>
        </tr>");
            }

            sb.Append("</tbody></table>");
            sb.Append(PdfFooter(docNo));
            sb.Append("</div></body></html>");
            return sb.ToString();
        }

        public async Task<IActionResult> DownloadPlantsPdf()
        {
            try
            {
                var plants = _adminRepo.GetAllPlants()
                    .Take(50)
                    .ToList();

                string html = BuildPlantsHtml(plants);
                byte[] pdfBytes = await GeneratePdfFromHtmlAsync(html);

                return File(
                    pdfBytes,
                    "application/pdf",
                    $"PlantsList_{DateTime.Now:yyyyMMdd}.pdf"
                );
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to generate PDF. Please try again.";
                return RedirectToAction("GetAllPlants");
            }
        }

        [SessionAuthorize("Admin")]
        [HttpPost]
        public ActionResult DeletePlant(int id)
        {
            if (id <= 0)
            {
                TempData["Error"] = "Invalid plant ID.";
                return RedirectToAction("GetAllPlants");
            }

            try
            {
                _adminRepo.TogglePlant(id, isActive: false);
                TempData["Success"] = "Plant deactivated successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to deactivate plant. Please try again.";
            }

            return RedirectToAction("GetAllPlants");
        }

        [SessionAuthorize("Admin")]
        [HttpPost]
        public ActionResult RestorePlant(int id)
        {
            if (id <= 0)
            {
                TempData["Error"] = "Invalid plant ID.";
                return RedirectToAction("GetAllPlants");
            }

            try
            {
                _adminRepo.TogglePlant(id, isActive: true);
                TempData["Success"] = "Plant restored successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to restore plant. Please try again.";
            }

            return RedirectToAction("GetAllPlants");
        }

        [SessionAuthorize("Admin")]
        [HttpGet]
        public ActionResult EditPlant(int id)
        {
            if (id <= 0)
                return NotFound();

            try
            {
                var plant = _adminRepo.getPlantById(id);
                if (plant == null)
                    return NotFound();
                return View(plant);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load plant details. Please try again.";
                return RedirectToAction("GetAllPlants");
            }
        }

        [SessionAuthorize("Admin")]
        [HttpPost]
        public ActionResult UpdatePlant(PlantModel plant)
        {
            if (!ModelState.IsValid)
                return View("EditPlant", plant);

            try
            {
                _adminRepo.UpdatePlant(plant);
                TempData["Success"] = "Plant updated successfully.";
                return RedirectToAction("GetAllPlants");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Failed to update plant. Please try again.";
                return View("EditPlant", plant);
            }
        }

        // ════════════════════════════════════════════════════════
        // COLLECTION
        // ════════════════════════════════════════════════════════

        [SessionAuthorize("Admin")]
        [HttpGet]
        public ActionResult AddCollection()
        {
            try
            {
                var village = _adminRepo.GetAllVillages();
                return View(village);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load form data. Please try again.";
                return RedirectToAction("GetAllCollection");
            }
        }

        [SessionAuthorize("Admin")]
        [HttpPost]
        public ActionResult AddCollection(string CenterName, int VillageID, decimal Capacity, string Location)
        {
            if (string.IsNullOrWhiteSpace(CenterName))
            {
                ViewBag.Error = "Center name is required.";
                return View(_adminRepo.GetAllVillages());
            }

            if (VillageID <= 0)
            {
                ViewBag.Error = "Please select a village.";
                return View(_adminRepo.GetAllVillages());
            }

            if (Capacity <= 0)
            {
                ViewBag.Error = "Capacity must be greater than zero.";
                return View(_adminRepo.GetAllVillages());
            }

            try
            {
                _adminRepo.AddCollection(CenterName.Trim(), VillageID, Capacity, Location);
                TempData["Success"] = $"Collection center '{CenterName}' added successfully.";
                return RedirectToAction("GetAllCollection");
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message.Contains("duplicate") || ex.Message.Contains("Duplicate")
                    ? $"Collection center '{CenterName}' already exists."
                    : "Failed to add collection center. Please try again.";
                return View(_adminRepo.GetAllVillages());
            }
        }

        [SessionAuthorize("Admin")]
        public ActionResult GetAllCollection(bool? isActive = true)
        {
            try
            {
                var collection = _adminRepo.GetAllCollection(isActive);
                return View(collection);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load collection centers. Please try again.";
                return View(new List<CollectionCenterModel>());
            }
        }

        private string BuildCollectionHtml(List<CollectionCenterModel> collections)
        {
            string docNo = $"COL-{DateTime.Now:yyyyMMdd}";
            int active = collections.Count(c => c.IsActive);
            int inactive = collections.Count(c => !c.IsActive);

            var sb = new StringBuilder();
            sb.Append($"<!DOCTYPE html><html><head><meta charset='utf-8'/><style>{PdfStyles()}</style></head><body><div class='page'>");
            sb.Append(PdfHeader("COLLECTION CENTERS", "Collection Centers Report", docNo));

            sb.Append("<div class='summary-row'>");
            sb.Append($"<div class='summary-box'><div class='lbl'>Total Centers</div><div class='val'>{collections.Count}</div><div class='sub'>All collection centers</div></div>");
            sb.Append($"<div class='summary-box'><div class='lbl'>Active</div><div class='val'>{active}</div><div class='sub'>Operational</div></div>");
            sb.Append($"<div class='summary-box'><div class='lbl'>Inactive</div><div class='val'>{inactive}</div><div class='sub'>Deactivated</div></div>");
            sb.Append("</div>");

            sb.Append("<table><thead><tr>");
            sb.Append("<th style='width:36px' class='c'>#</th><th>Center Name</th><th>Village</th><th>Location</th><th class='r'>Capacity (L)</th><th class='c'>Status</th>");
            sb.Append("</tr></thead><tbody>");

            int i = 1;
            foreach (var c in collections)
            {
                string badge = c.IsActive
                    ? "<span class='badge badge-active'>Active</span>"
                    : "<span class='badge badge-inactive'>Inactive</span>";

                sb.Append($@"<tr>
            <td class='c' style='color:#777'>{i++}</td>
            <td><strong>{c.CenterName}</strong></td>
            <td>{c.VillageName ?? "—"}</td>
            <td>{c.Location ?? "—"}</td>
            <td class='r'>{c.Capacity:N0}</td>
            <td class='c'>{badge}</td>
        </tr>");
            }

            sb.Append("</tbody></table>");
            sb.Append(PdfFooter(docNo));
            sb.Append("</div></body></html>");
            return sb.ToString();
        }
        public async Task<IActionResult> DownloadCollectionsPdf()
        {
            try
            {
                var collections = _adminRepo.GetAllCollection(true)
                    .Take(50)
                    .ToList();

                string html = BuildCollectionHtml(collections);
                byte[] pdfBytes = await GeneratePdfFromHtmlAsync(html);

                return File(
                    pdfBytes,
                    "application/pdf",
                    $"Collections_{DateTime.Now:yyyyMMdd}.pdf"
                );
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to generate PDF. Please try again.";
                return RedirectToAction("GetAllCollection");
            }
        }

        [SessionAuthorize("Admin")]
        [HttpPost]
        public ActionResult ToggleCollection(int id, bool isActive)
        {
            if (id <= 0)
            {
                TempData["Error"] = "Invalid collection center ID.";
                return RedirectToAction("GetAllCollection");
            }

            try
            {
                _adminRepo.ToggleCollection(id, isActive);
                TempData["Success"] = isActive ? "Collection center activated." : "Collection center deactivated.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to update collection center status. Please try again.";
            }

            return RedirectToAction("GetAllCollection");
        }

        [SessionAuthorize("Admin")]
        [HttpGet]
        public ActionResult EditCollection(int id)
        {
            if (id <= 0)
                return NotFound();

            try
            {
                ViewBag.Villages = _adminRepo.GetAllVillages();
                var collection = _adminRepo.getCollectionById(id);
                if (collection == null)
                    return NotFound();
                return View(collection);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load collection center details. Please try again.";
                return RedirectToAction("GetAllCollection");
            }
        }

        [SessionAuthorize("Admin")]
        [HttpPost]
        public ActionResult UpdateCollection(CollectionCenterModel collection)
        {
            try
            {
                _adminRepo.UpdateCollection(collection);
                TempData["Success"] = "Collection center updated successfully.";
                return RedirectToAction("GetAllCollection");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Failed to update collection center. Please try again.";
                ViewBag.Villages = _adminRepo.GetAllVillages();
                return View("EditCollection", collection);
            }
        }

        [SessionAuthorize("Admin")]
        public IActionResult Batches(int? centerId = null, string status = null,
                              DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var batches = _adminRepo.GetAllBatches(centerId, status, fromDate, toDate);

                ViewBag.Centers = _adminRepo.GetAllCenters();
                ViewBag.CenterId = centerId;
                ViewBag.Status = status;
                ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
                ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

                ViewBag.OpenCount = batches.Count(b => b.Status == "Open");
                ViewBag.ClosedCount = batches.Count(b => b.Status == "Closed");
                ViewBag.DispatchedCount = batches.Count(b => b.Status == "Dispatched");

                return View(batches);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load batches. Please try again.";
                ViewBag.Centers = _adminRepo.GetAllCenters();
                return View(new List<BatchModel>());
            }
        }

        [SessionAuthorize("Admin")]
        [HttpGet]
        public IActionResult BatchDetail(int id)
        {
            if (id <= 0)
            {
                TempData["Error"] = "Invalid batch ID.";
                return RedirectToAction("Batches");
            }

            try
            {
                var batch = _adminRepo.GetBatchById(id);
                if (batch == null)
                {
                    TempData["Error"] = "Batch not found.";
                    return RedirectToAction("Batches");
                }

                var entries = _adminRepo.GetBatchCollections(id);
                ViewBag.Entries = entries;
                ViewBag.TotalEntries = entries.Count;
                ViewBag.TotalQty = entries.Sum(e => e.Quantity);
                ViewBag.TotalAmount = entries.Sum(e => e.Amount ?? 0);

                return View(batch);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load batch details. Please try again.";
                return RedirectToAction("Batches");
            }
        }

        [SessionAuthorize("Admin")]
        [HttpPost]
        public IActionResult OpenBatch(int centerId, string shift, DateTime batchDate)
        {
            if (centerId <= 0)
            {
                TempData["Error"] = "Please select a collection center.";
                return RedirectToAction("Batches");
            }

            if (string.IsNullOrWhiteSpace(shift))
            {
                TempData["Error"] = "Please select a shift.";
                return RedirectToAction("Batches");
            }

            try
            {
                int batchId = _adminRepo.OpenBatch(centerId, shift, batchDate);
                TempData["Success"] = $"Batch #{batchId} opened successfully.";
                return RedirectToAction("BatchDetail", new { id = batchId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message.Contains("already exists")
                    ? "An open batch already exists for this center, shift and date."
                    : "Error: " + ex.Message;
                return RedirectToAction("Batches");
            }
        }

        [SessionAuthorize("Admin")]
        [HttpPost]
        public IActionResult CloseBatch(int batchId)
        {
            if (batchId <= 0)
            {
                TempData["Error"] = "Invalid batch ID.";
                return RedirectToAction("Batches");
            }

            try
            {
                _adminRepo.CloseBatch(batchId);
                TempData["Success"] = $"Batch #{batchId} closed successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message.Contains("not Open")
                    ? "Batch is not in Open status and cannot be closed."
                    : "Error: " + ex.Message;
            }

            return RedirectToAction("BatchDetail", new { id = batchId });
        }

        // ════════════════════════════════════════════════════════
        // Production
        // ════════════════════════════════════════════════════════

        [SessionAuthorize("Admin")]
        public IActionResult Products(string productType = null, bool? isActive = null)
        {
            try
            {
                var products = _adminRepo.GetAllProducts(productType, isActive);
                ViewBag.ProductTypes = _adminRepo.GetProductTypes();
                ViewBag.CurrentType = productType;
                ViewBag.CurrentActive = isActive;
                return View(products);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load products. Please try again.";
                ViewBag.ProductTypes = new List<string>();
                return View(new List<ProductModel>());
            }
        }

        [SessionAuthorize("Admin")]
        public IActionResult ProductDetail(int id)
        {
            if (id <= 0)
            {
                TempData["Error"] = "Invalid product ID.";
                return RedirectToAction("Products");
            }

            try
            {
                var product = _adminRepo.GetProductById(id);
                if (product == null)
                {
                    TempData["Error"] = "Product not found.";
                    return RedirectToAction("Products");
                }
                return View(product);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load product details. Please try again.";
                return RedirectToAction("Products");
            }
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
            if (string.IsNullOrWhiteSpace(productName))
            {
                ViewBag.Error = "Product name is required.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(productType))
            {
                ViewBag.Error = "Product type is required.";
                return View();
            }

            if (mrp <= 0)
            {
                ViewBag.Error = "MRP must be greater than zero.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(unit))
            {
                ViewBag.Error = "Unit is required.";
                return View();
            }

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
                ViewBag.Error = ex.Message.Contains("duplicate") || ex.Message.Contains("Duplicate")
                    ? $"Product '{productName}' already exists."
                    : ex.Message;
                return View();
            }
        }

        [SessionAuthorize("Admin")]
        [HttpGet]
        public IActionResult EditProduct(int id)
        {
            if (id <= 0)
                return NotFound();

            try
            {
                var product = _adminRepo.GetProductById(id);
                if (product == null)
                {
                    TempData["Error"] = "Product not found.";
                    return RedirectToAction("Products");
                }
                return View(product);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load product. Please try again.";
                return RedirectToAction("Products");
            }
        }

        [SessionAuthorize("Admin")]
        [HttpPost]
        public IActionResult EditProduct(int productId, string productName,
                                         string productType, decimal mrp,
                                         string unit, int? shelfLifeDays,
                                         string description)
        {
            if (string.IsNullOrWhiteSpace(productName))
            {
                ViewBag.Error = "Product name is required.";
                return View(_adminRepo.GetProductById(productId));
            }

            if (mrp <= 0)
            {
                ViewBag.Error = "MRP must be greater than zero.";
                return View(_adminRepo.GetProductById(productId));
            }

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
            if (productId <= 0)
            {
                TempData["Error"] = "Invalid product ID.";
                return RedirectToAction("Products");
            }

            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                _adminRepo.ToggleProductStatus(productId, isActive, userId);

                TempData["Success"] = isActive ? "Product activated." : "Product deactivated.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to update product status. Please try again.";
            }

            return RedirectToAction("Products");
        }

        [SessionAuthorize("Admin")]
        public IActionResult ProductionBatches(int? plantId = null, int? productId = null,
                                        string batchStatus = null,
                                        DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
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
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load production batches. Please try again.";
                ViewBag.Plants = _adminRepo.GetAllPlants();
                ViewBag.Products = _adminRepo.GetActiveProducts();
                return View(new List<ProductionBatchModel>());
            }
        }

        [SessionAuthorize("Admin")]
        public IActionResult WastageSummary(int? plantId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var wastage = _reportRepo.GetWastageSummary(plantId, fromDate, toDate);

                ViewBag.Plants = _adminRepo.GetAllPlants();
                ViewBag.PlantId = plantId;
                ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
                ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

                return View(wastage);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load wastage summary. Please try again.";
                ViewBag.Plants = _adminRepo.GetAllPlants();
                return View(new List<object>());
            }
        }

        [SessionAuthorize("Admin")]
        public IActionResult MilkTransfers(int? plantId = null, int? centerId = null,
                                    DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
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
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load milk transfers. Please try again.";
                ViewBag.Plants = _adminRepo.GetAllPlants();
                ViewBag.Centers = _adminRepo.GetAllCenters();
                return View(new List<MilkTransferModel>());
            }
        }

        // ════════════════════════════════════════════════════════
        // DISTRIBUTOR
        // ════════════════════════════════════════════════════════

        [SessionAuthorize("Admin")]
        public ActionResult Distributors()
        {
            try
            {
                var distributors = _adminRepo.GetDistributors();
                return View(distributors);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load distributors. Please try again.";
                return View(new List<Distributor>());
            }
        }

        private string BuildDistributorHtml(List<Distributor> distributors)
        {
            string docNo = $"DST-{DateTime.Now:yyyyMMdd}";
            int approved = distributors.Count(d => d.Status == "Approved");
            int pending = distributors.Count(d => d.Status == "Pending");
            int others = distributors.Count - approved - pending;

            var sb = new StringBuilder();
            sb.Append($"<!DOCTYPE html><html><head><meta charset='utf-8'/><style>{PdfStyles()}</style></head><body><div class='page'>");
            sb.Append(PdfHeader("DISTRIBUTORS LIST", "Sales — Distributor Report", docNo));

            sb.Append("<div class='summary-row'>");
            sb.Append($"<div class='summary-box'><div class='lbl'>Total</div><div class='val'>{distributors.Count}</div><div class='sub'>All distributors</div></div>");
            sb.Append($"<div class='summary-box'><div class='lbl'>Approved</div><div class='val'>{approved}</div><div class='sub'>Active distributors</div></div>");
            sb.Append($"<div class='summary-box'><div class='lbl'>Pending</div><div class='val'>{pending}</div><div class='sub'>Awaiting approval</div></div>");
            sb.Append($"<div class='summary-box'><div class='lbl'>Other</div><div class='val'>{others}</div><div class='sub'>Rejected / Suspended</div></div>");
            sb.Append("</div>");

            sb.Append("<table><thead><tr>");
            sb.Append("<th style='width:36px' class='c'>#</th><th>Distributor Name</th><th>Phone</th><th>Email</th><th>Location</th><th>GSTIN</th><th class='c'>Status</th>");
            sb.Append("</tr></thead><tbody>");

            int i = 1;
            foreach (var d in distributors)
            {
                string badgeStyle = d.Status == "Approved" ? "badge-active"
                                  : d.Status == "Pending" ? "style='background:#fef9c3;color:#854d0e;border:0.5px solid #fde047;'"
                                  : "badge-inactive";

                // handle pending badge separately since it needs inline style
                string badge = d.Status == "Approved"
                    ? "<span class='badge badge-active'>Approved</span>"
                    : d.Status == "Pending"
                    ? "<span class='badge' style='background:#fef9c3;color:#854d0e;border:0.5px solid #fde047;'>Pending</span>"
                    : "<span class='badge badge-inactive'>Rejected</span>";

                sb.Append($@"<tr>
            <td class='c' style='color:#777'>{i++}</td>
            <td><strong>{d.DistributorName}</strong></td>
            <td>{d.ContactNumber ?? "—"}</td>
            <td>{d.Email ?? "—"}</td>
            <td>{d.Location ?? "—"}</td>
            <td>{d.GSTIN ?? "—"}</td>
            <td class='c'>{badge}</td>
        </tr>");
            }

            sb.Append("</tbody></table>");
            sb.Append(PdfFooter(docNo));
            sb.Append("</div></body></html>");
            return sb.ToString();
        }

        private string PdfHeader(string title, string subtitle, string docNumber)
        {
            string generated = DateTime.Now.ToString("dd MMM yyyy, hh:mm tt");
            string today = DateTime.Now.ToString("dd MMM yyyy");
            return $@"
        <div class='top-header'>
            <div>
                <div class='co-name'>Dairy Management System</div>
                <div class='co-sub'>{subtitle}</div>
                <div class='co-sub'>Generated: {generated}</div>
            </div>
            <div class='rt'>
                <h2>{title}</h2>
                <p>Doc No: {docNumber}</p>
                <p>Date: {today}</p>
            </div>
        </div>";
        }

        private string PdfStyles()
        {
            return @"
        *{margin:0;padding:0;box-sizing:border-box;}
        body{font-family:'Segoe UI',Arial,sans-serif;background:#f0f0f0;padding:24px;color:#111;}
        .page{background:#fff;max-width:900px;margin:0 auto;padding:28px 32px;font-size:12px;}
        .top-header{display:flex;justify-content:space-between;align-items:flex-start;border-bottom:2px solid #1e3a5f;padding-bottom:10px;margin-bottom:16px;}
        .co-name{font-size:18px;font-weight:700;color:#1e3a5f;letter-spacing:0.5px;}
        .co-sub{font-size:10px;color:#555;margin-top:3px;}
        .rt{text-align:right;}
        .rt h2{font-size:14px;font-weight:700;color:#1e3a5f;border:1px solid #1e3a5f;padding:4px 10px;display:inline-block;}
        .rt p{font-size:10px;color:#555;margin-top:4px;}
        .summary-row{display:flex;gap:12px;margin-bottom:16px;}
        .summary-box{flex:1;border:0.5px solid #ccc;border-radius:4px;padding:8px 12px;}
        .summary-box .lbl{font-size:9px;text-transform:uppercase;color:#777;letter-spacing:0.5px;font-weight:600;margin-bottom:4px;}
        .summary-box .val{font-size:16px;font-weight:700;color:#1e3a5f;}
        .summary-box .sub{font-size:10px;color:#555;margin-top:2px;}
        table{width:100%;border-collapse:collapse;margin-bottom:12px;}
        table th{background:#1e3a5f;color:#fff;font-size:10px;padding:7px 8px;text-align:left;font-weight:600;-webkit-print-color-adjust:exact;}
        table th.r,table td.r{text-align:right;}
        table th.c,table td.c{text-align:center;}
        table td{font-size:11px;padding:6px 8px;border-bottom:0.5px solid #e5e7eb;color:#111;}
        table tr:nth-child(even) td{background:#f9fafb;}
        .badge{display:inline-block;padding:2px 8px;border-radius:20px;font-size:10px;font-weight:600;}
        .badge-active{background:#dcfce7;color:#166534;border:0.5px solid #86efac;}
        .badge-inactive{background:#fee2e2;color:#991b1b;border:0.5px solid #fca5a5;}
        .footer{border-top:1px solid #ccc;margin-top:16px;padding-top:8px;text-align:center;font-size:9px;color:#888;}";
        }

        private string PdfFooter(string docNumber)
        {
            return $@"<div class='footer'>
        This is a computer-generated document and does not require a physical signature. &nbsp;|&nbsp;
        Doc: {docNumber} &nbsp;|&nbsp; Generated: {DateTime.Now:dd MMM yyyy, hh:mm tt} &nbsp;|&nbsp;
        Dairy Management System
    </div>";
        }
        [HttpGet]
        [SessionAuthorize("Admin")]
        public async Task<IActionResult> DownloadFarmerPaymentReceiptPdf(int id)
        {
            if (id <= 0)
            {
                TempData["Error"] = "Invalid payment ID.";
                return RedirectToAction("CenterPayments");
            }

            try
            {
                var payment = _financeRepo.GetFarmerPaymentById(id);
                if (payment == null)
                {
                    TempData["Error"] = $"Farmer payment #{id} not found.";
                    return RedirectToAction("CenterPayments");
                }

                string html = BuildReceiptHtml(payment);
                byte[] pdfBytes = await GeneratePdfFromHtmlAsync(html);

                return File(
                    pdfBytes,
                    "application/pdf",
                    $"FarmerReceipt_FP{payment.PaymentId:D6}_{DateTime.Now:yyyyMMdd}.pdf"
                );
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to generate receipt PDF. Please try again.";
                return RedirectToAction("CenterPayments");
            }
        }
        private string BuildReceiptHtml(FarmerPaymentModel p)
        {
            string statusColor = p.PaymentStatus == "Processed" ? "#dcfce7" : p.PaymentStatus == "Pending" ? "#fef9c3" : "#fee2e2";
            string statusText = p.PaymentStatus == "Processed" ? "#166534" : p.PaymentStatus == "Pending" ? "#854d0e" : "#991b1b";
            string statusBorder = p.PaymentStatus == "Processed" ? "#86efac" : p.PaymentStatus == "Pending" ? "#fde047" : "#fca5a5";
            string bankDetails = p.BankName != null ? $"{p.BankName}" : "Not linked";
            string accountNo = p.AccountNumber ?? "N/A";
            string ifsc = p.IFSCCode ?? "N/A";
            string transRef = p.TransactionReference ?? "N/A";
            string bankSt = p.BankStatus ?? "N/A";
            string bankStColor = p.BankStatus == "Success" ? "#166534" : "#991b1b";
            string pid = p.PaymentId.ToString("D6");
            string generated = DateTime.Now.ToString("dd MMM yyyy, hh:mm tt");
            string today = DateTime.Now.ToString("dd MMM yyyy");
            string fromDate = p.FromDate.ToString("dd MMM yyyy");
            string toDate = p.ToDate.ToString("dd MMM yyyy");
            string payDate = p.PaymentDate.ToString("dd MMM yyyy");
            string qty = p.TotalQty.ToString("N2");
            string amt = p.TotalAmount.ToString("N2");

            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html><html><head><meta charset='utf-8'/><style>");
            sb.Append("*{margin:0;padding:0;box-sizing:border-box;}");
            sb.Append("body{font-family:'Segoe UI',Arial,sans-serif;background:#f0f0f0;padding:24px;color:#111;}");
            sb.Append(".page{background:#fff;max-width:720px;margin:0 auto;padding:28px 32px;font-size:12px;}");
            sb.Append(".top-header{display:flex;justify-content:space-between;align-items:flex-start;border-bottom:2px solid #1e3a5f;padding-bottom:10px;margin-bottom:10px;}");
            sb.Append(".co-name{font-size:18px;font-weight:700;color:#1e3a5f;letter-spacing:0.5px;}");
            sb.Append(".co-sub{font-size:10px;color:#555;margin-top:3px;}");
            sb.Append(".rt{text-align:right;}");
            sb.Append(".rt h2{font-size:15px;font-weight:700;color:#1e3a5f;border:1px solid #1e3a5f;padding:4px 10px;display:inline-block;}");
            sb.Append(".rt p{font-size:10px;color:#555;margin-top:4px;}");
            sb.Append($".status-bar{{text-align:center;padding:5px;font-size:11px;font-weight:700;letter-spacing:1px;margin-bottom:10px;border-radius:3px;background:{statusColor};color:{statusText};border:0.5px solid {statusBorder};}}");
            sb.Append(".meta-row{display:flex;justify-content:space-between;margin-bottom:10px;gap:12px;}");
            sb.Append(".meta-box{flex:1;border:0.5px solid #ccc;border-radius:4px;padding:8px 10px;}");
            sb.Append(".meta-box .lbl{font-size:9px;text-transform:uppercase;color:#777;letter-spacing:0.5px;font-weight:600;margin-bottom:4px;}");
            sb.Append(".meta-box .val{font-size:11px;color:#111;line-height:1.6;}");
            sb.Append("table{width:100%;border-collapse:collapse;margin-bottom:10px;}");
            sb.Append("table th{background:#1e3a5f;color:#fff;font-size:10px;padding:6px 8px;text-align:left;font-weight:600;}");
            sb.Append("table th.r,table td.r{text-align:right;}");
            sb.Append("table td{font-size:11px;padding:5px 8px;border-bottom:0.5px solid #e5e7eb;color:#111;}");
            sb.Append("table tr:nth-child(even) td{background:#f9fafb;}");
            sb.Append(".totals-section{display:flex;justify-content:flex-end;margin-bottom:10px;}");
            sb.Append(".totals-table{width:260px;font-size:11px;}");
            sb.Append(".totals-table td{padding:4px 8px;border-bottom:0.5px solid #e5e7eb;}");
            sb.Append(".grand td{background:#1e3a5f !important;color:#fff !important;font-weight:700;font-size:13px;-webkit-print-color-adjust:exact;}");
            sb.Append(".bottom-row{display:flex;gap:12px;margin-top:10px;}");
            sb.Append(".bank-box{flex:1;border:0.5px solid #ccc;border-radius:4px;padding:8px 10px;}");
            sb.Append(".bank-box .lbl{font-size:9px;text-transform:uppercase;color:#777;letter-spacing:0.5px;font-weight:600;margin-bottom:5px;}");
            sb.Append(".bank-box table{margin:0;} .bank-box table td{border:none;padding:2px 0;font-size:11px;} .bank-box table td:first-child{color:#777;width:110px;}");
            sb.Append(".sig-box{width:160px;border:0.5px solid #ccc;border-radius:4px;padding:8px 10px;text-align:center;display:flex;flex-direction:column;justify-content:space-between;}");
            sb.Append(".sig-line{border-top:1px solid #aaa;margin-top:40px;padding-top:4px;font-size:10px;color:#555;}");
            sb.Append(".footer{border-top:1px solid #ccc;margin-top:12px;padding-top:8px;text-align:center;font-size:9px;color:#888;}");
            sb.Append("</style></head><body><div class='page'>");

            sb.Append("<div class='top-header'>");
            sb.Append("<div><div class='co-name'>Dairy Management System</div>");
            sb.Append("<div class='co-sub'>Farmer Milk Payment Receipt</div>");
            sb.Append("<div class='co-sub'>Payment Type: Direct Bank Transfer via Stripe</div></div>");
            sb.Append($"<div class='rt'><h2>PAYMENT RECEIPT</h2><p>Receipt No: FP-{pid}</p><p>Date: {today}</p></div>");
            sb.Append("</div>");

            sb.Append($"<div class='status-bar'>PAYMENT STATUS: {p.PaymentStatus.ToUpper()}</div>");

            sb.Append("<div class='meta-row'>");
            sb.Append($"<div class='meta-box'><div class='lbl'>Farmer Details</div><div class='val'><strong>{p.FarmerName}</strong><br/>Bank: {bankDetails}<br/>A/C: {accountNo} | IFSC: {ifsc}</div></div>");
            sb.Append($"<div class='meta-box'><div class='lbl'>Collection Center</div><div class='val'><strong>{p.CenterName}</strong><br/>Collection Period:<br/>{fromDate} → {toDate}</div></div>");
            sb.Append($"<div class='meta-box'><div class='lbl'>Payment Info</div><div class='val'>Payment Date: <strong>{payDate}</strong><br/>Method: Stripe<br/>Currency: INR<br/>Bank Status: <strong style='color:{bankStColor}'>{bankSt}</strong></div></div>");
            sb.Append("</div>");

            sb.Append("<table><thead><tr>");
            sb.Append("<th style='width:36px'>S.No</th><th>Description</th><th>Collection Period</th>");
            sb.Append("<th class='r'>Total Qty (L)</th><th class='r'>Amount (₹)</th>");
            sb.Append("</tr></thead><tbody>");
            sb.Append($"<tr><td>1</td><td>Milk Collection Payment</td><td>{fromDate} – {toDate}</td><td class='r'>{qty}</td><td class='r'>{amt}</td></tr>");
            sb.Append("</tbody></table>");

            sb.Append("<div class='totals-section'><table class='totals-table'>");
            sb.Append($"<tr><td style='color:#777'>Total Quantity</td><td class='r'>{qty} L</td></tr>");
            sb.Append($"<tr><td style='color:#777'>Subtotal</td><td class='r'>₹ {amt}</td></tr>");
            sb.Append($"<tr><td style='color:#777'>Deductions</td><td class='r'>₹ 0.00</td></tr>");
            sb.Append($"<tr class='grand'><td>Total Amount Paid</td><td class='r'>₹ {amt}</td></tr>");
            sb.Append("</table></div>");

            sb.Append("<div class='bottom-row'>");
            sb.Append("<div class='bank-box'><div class='lbl'>Bank &amp; Transaction Details</div><table>");
            sb.Append($"<tr><td>Bank Name</td><td>{bankDetails}</td></tr>");
            sb.Append($"<tr><td>Account No</td><td>{accountNo}</td></tr>");
            sb.Append($"<tr><td>IFSC Code</td><td>{ifsc}</td></tr>");
            sb.Append($"<tr><td>Transaction Ref</td><td style='color:#1e3a5f;font-weight:600'>{transRef}</td></tr>");
            sb.Append("<tr><td>Payment Via</td><td>Stripe</td></tr>");
            sb.Append("</table></div>");
            sb.Append("<div class='sig-box'>");
            sb.Append("<div style='font-size:10px;color:#777;text-align:left'>For Dairy Management System</div>");
            sb.Append("<div><div class='sig-line'>Authorised Signatory</div>");
            sb.Append("<div style='font-size:9px;color:#777;margin-top:3px;'>Dairy Management System</div></div>");
            sb.Append("</div></div>");

            sb.Append($"<div class='footer'>This is a computer-generated receipt and does not require a physical signature. | Receipt No: FP-{pid} | Generated: {generated} | Dairy Management System</div>");
            sb.Append("</div></body></html>");
            return sb.ToString();
        }
        public async Task<IActionResult> DownloadDistributorsPdf()
        {
            try
            {
                var distributors = _adminRepo.GetDistributors()
                    .Take(50)
                    .ToList();

                string html = BuildDistributorHtml(distributors);
                byte[] pdfBytes = await GeneratePdfFromHtmlAsync(html);

                return File(
                    pdfBytes,
                    "application/pdf",
                    $"Distributors_{DateTime.Now:yyyyMMdd}.pdf"
                );
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to generate PDF. Please try again.";
                return RedirectToAction("Distributors");
            }
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
            if (distributor == null)
            {
                ViewBag.Error = "Invalid distributor data.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Username and password are required.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(distributor.DistributorName))
            {
                ViewBag.Error = "Distributor name is required.";
                return View();
            }

            try
            {
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
                _adminRepo.RegisterDistributor(distributor, username, passwordHash);
                ViewBag.Success = "Registration submitted. Please wait for admin approval.";
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message.Contains("duplicate") || ex.Message.Contains("Username already exists")
                    ? "That username is already taken. Please choose another."
                    : "Failed to register distributor. Please try again.";
                return View();
            }
        }

        [HttpPost]
        [SessionAuthorize("Admin")]
        public IActionResult UpdateDistributorStatus(int distributorId, string status)
        {
            if (distributorId <= 0)
            {
                TempData["error"] = "Invalid distributor ID.";
                return RedirectToAction("Distributors");
            }

            var allowed = new[] { "Approved", "Rejected", "Suspended" };
            if (!allowed.Contains(status))
            {
                TempData["error"] = "Invalid status value.";
                return RedirectToAction("PendingDistributors");
            }

            try
            {
                _adminRepo.UpdateDistributorStatus(distributorId, status);

                TempData["success"] = status switch
                {
                    "Approved" => "Distributor approved successfully.",
                    "Rejected" => "Distributor rejected.",
                    "Suspended" => "Distributor suspended.",
                    _ => "Status updated."
                };

                return status == "Suspended"
                    ? RedirectToAction("Distributors")
                    : RedirectToAction("PendingDistributors");
            }
            catch (Exception ex)
            {
                TempData["error"] = "Failed to update distributor status. Please try again.";
                return status == "Suspended"
                    ? RedirectToAction("Distributors")
                    : RedirectToAction("PendingDistributors");
            }
        }

        [SessionAuthorize("Admin")]
        public IActionResult PendingDistributors()
        {
            try
            {
                var pending = _adminRepo.GetPendingDistributors();
                return View(pending);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load pending distributors. Please try again.";
                return View(new List<Distributor>());
            }
        }

        public IActionResult AdminOrder()
        {
            try
            {
                var model = new AdminOrderModel();
                model.DistributorList = _adminRepo.GetDistributors();
                model.ProductList = _adminRepo.GetAllProducts(null, true);
                model.PlantList = _adminRepo.GetActivePlants();
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load order form. Please try again.";
                return View(new AdminOrderModel());
            }
        }

        [HttpPost]
        [SessionAuthorize("Admin")]
        public IActionResult AdminOrder(AdminOrderModel model)
        {
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
                    ModelState.AddModelError("", "Failed to create order: " + ex.Message);
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
            try
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
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load orders. Please try again.";
                return View(new AdminOrderListModel { DistributorList = new List<Distributor>(), Orders = new List<OrderSummary>() });
            }
        }

        [HttpPost]
        [SessionAuthorize("Admin")]
        public IActionResult AdminOrderList(AdminOrderListModel model)
        {
            try
            {
                model.DistributorList = _adminRepo.GetDistributors();
                model.Orders = _adminRepo.GetAllOrders(
                    model.DistributorId,
                    model.OrderStatus,
                    model.FromDate,
                    model.ToDate
                );
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load orders. Please try again.";
                model.DistributorList = new List<Distributor>();
                model.Orders = new List<OrderSummary>();
            }

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
            if (orderId <= 0)
            {
                TempData["Error"] = "Invalid order ID.";
                return RedirectToAction("AdminOrderList");
            }

            if (string.IsNullOrWhiteSpace(status))
            {
                TempData["Error"] = "Please select a valid status.";
                return RedirectToAction("AdminOrderList");
            }

            try
            {
                _adminRepo.UpdateOrderStatus(orderId, status);
                TempData["Message"] = $"Order #{orderId} status updated to {status}.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to update order status. Please try again.";
            }

            return RedirectToAction("AdminOrderList", new
            {
                distributorId,
                orderStatus,
                fromDate,
                toDate
            });
        }

        // ════════════════════════════════════════════════════════
        // Notification
        // ════════════════════════════════════════════════════════

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

            try
            {
                var result = _adminRepo.MarkNotificationRead(notificationId);
                return Json(new { success = result });
            }
            catch
            {
                return Json(new { success = false, message = "Failed to mark notification as read." });
            }
        }

        [HttpPost]
        [SessionAuthorize("Admin")]
        public IActionResult MarkAllNotificationsRead()
        {
            try
            {
                var result = _adminRepo.MarkAllNotificationsRead();
                return Json(new { success = result });
            }
            catch
            {
                return Json(new { success = false, message = "Failed to mark notifications as read." });
            }
        }

        // ════════════════════════════════════════════════════════
        // Finance
        // ════════════════════════════════════════════════════════

        [SessionAuthorize("Admin")]
        public IActionResult CenterPayments()
        {
            try
            {
                List<CenterPaymentModel> payments = _financeRepo.GetAllCenterPayments(plantId: null);
                return View(payments);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load center payments. Please try again.";
                return View(new List<CenterPaymentModel>());
            }
        }

        [SessionAuthorize("Admin")]
        public IActionResult CenterPaymentDetail(int id)
        {
            if (id <= 0)
            {
                TempData["Error"] = "Invalid payment ID.";
                return RedirectToAction(nameof(CenterPayments));
            }

            try
            {
                CenterPaymentModel payment = _financeRepo.GetCenterPaymentById(id);

                if (payment == null)
                {
                    TempData["Error"] = $"Center payment #{id} was not found.";
                    return RedirectToAction(nameof(CenterPayments));
                }

                return View(payment);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load payment details. Please try again.";
                return RedirectToAction(nameof(CenterPayments));
            }
        }
    }
}