using DairyIndustry.Filters;
using DairyIndustry.Repositories;
using DairyIndustry.Services;
using Microsoft.AspNetCore.Mvc;

namespace DairyIndustry.Controllers
{
    [ServiceFilter(typeof(ActionLogFilter))]
    public class LogisticsController : Controller
    {
        private readonly ILogisticsRepository _logisticRepo;
        private readonly EmailService _emailService;
        private readonly FileUploadService _fileUpload;

        public LogisticsController(ILogisticsRepository logisticsRepository,
            EmailService emailService, FileUploadService fileUploadService)
        {
            _logisticRepo = logisticsRepository;
            _emailService = emailService;
            _fileUpload = fileUploadService;
        }

        [SessionAuthorize("Driver")]
        public IActionResult Index()
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            int driverId = HttpContext.Session.GetInt32("DriverId") ?? 0;

            var vm = _logisticRepo.GetDriverDashboard(driverId);

            vm.Driver = _logisticRepo.GetDriverByUserId(userId);

            return View(vm);
        }

        [SessionAuthorize("Driver")]
        public IActionResult MyVehicles()
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            int driverId = HttpContext.Session.GetInt32("DriverId") ?? 0;
            var driver = _logisticRepo.GetDriverByUserId(userId);
            var vehicles = _logisticRepo.GetVehiclesByDriverId(driverId);
            ViewBag.Vehicles = vehicles;
            return View(driver);
        }

        [HttpGet]
        public IActionResult RegisterDriver() => View();

        [HttpGet]
        public IActionResult CompleteRegistration()
        {
            if (HttpContext.Session.GetString("OtpVerified") != "true")
            {
                TempData["Error"] = "Please verify your email first.";
                return RedirectToAction("RegisterDriver");
            }
            ViewBag.Email = HttpContext.Session.GetString("PendingDriverEmail");
            ViewBag.Name = HttpContext.Session.GetString("PendingDriverName");
            return View();
        }

        [HttpPost]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> CompleteRegistration(
    string licenseNo,
    string phone,
    string username,
    string password,
    IFormFile drivingLicenseFile)
        {
            string email = HttpContext.Session.GetString("PendingDriverEmail");
            string driverName = HttpContext.Session.GetString("PendingDriverName");
            string otpVerified = HttpContext.Session.GetString("OtpVerified");

            if (otpVerified != "true" || string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Session expired. Please restart registration.";
                return RedirectToAction("RegisterDriver");
            }

            if (drivingLicenseFile == null || drivingLicenseFile.Length == 0)
            {
                TempData["Error"] = "Driving licence document is required.";
                ViewBag.Email = email;
                ViewBag.Name = driverName;
                return View();
            }

            string extension = Path.GetExtension(drivingLicenseFile.FileName).ToLower();
            HashSet<string> allowed = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".pdf" };

            if (!allowed.Contains(extension))
            {
                TempData["Error"] = "Invalid file type. Only JPG, PNG and PDF are allowed.";
                ViewBag.Email = email;
                ViewBag.Name = driverName;
                return View();
            }

            if (drivingLicenseFile.Length > 5 * 1024 * 1024)
            {
                TempData["Error"] = "File size cannot exceed 5 MB.";
                ViewBag.Email = email;
                ViewBag.Name = driverName;
                return View();
            }

            string webRoot = _fileUpload.GetWebRootPath();

            try
            {
                string folder = Path.Combine(webRoot, "uploads", "documents", "drivinglicences");
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string fileName = Guid.NewGuid() + extension;
                string fullPath = Path.Combine(folder, fileName);
                string dlPath = "/uploads/documents/drivinglicences/" + fileName;

                using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                {
                    await drivingLicenseFile.CopyToAsync(fileStream);
                }

                string passHash = BCrypt.Net.BCrypt.HashPassword(password);

                await _logisticRepo.RegisterDriverAsync(
                    driverName, licenseNo, phone,
                    email, username, passHash, dlPath);

                HttpContext.Session.Remove("PendingDriverEmail");
                HttpContext.Session.Remove("PendingDriverName");
                HttpContext.Session.Remove("OtpVerified");

                ViewBag.Success = "Registration submitted. Please wait for admin approval.";
                return View("RegisterDriver");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                ViewBag.Email = email;
                ViewBag.Name = driverName;
                return View();
            }
        }

        [HttpPost]
        public IActionResult SendOtp(string email, string driverName)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(driverName))
            {
                TempData["Error"] = "Name and email are required to send OTP.";
                return RedirectToAction("RegisterDriver");
            }

            string otp = new Random().Next(100000, 999999).ToString();
            _logisticRepo.SaveEmailOtp(email, otp);

            _ = Task.Run(async () =>
            {
                try { await _emailService.SendOtpEmailAsync(email, otp); }
                catch (Exception ex) { Console.WriteLine($"[OTP Email] Failed: {ex.Message}"); }
            });

            HttpContext.Session.SetString("PendingDriverEmail", email);
            HttpContext.Session.SetString("PendingDriverName", driverName);

            TempData["Info"] = $"OTP sent to {email}. Please check your inbox.";
            return RedirectToAction("VerifyOtp");
        }

        [HttpGet]
        public IActionResult VerifyOtp()
        {
            ViewBag.Email = HttpContext.Session.GetString("PendingDriverEmail");
            return View();
        }

        [HttpPost]
        public IActionResult VerifyOtp(string otpCode)
        {
            string email = HttpContext.Session.GetString("PendingDriverEmail");
            if (string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Session expired. Please start registration again.";
                return RedirectToAction("RegisterDriver");
            }

            bool valid = _logisticRepo.VerifyEmailOtp(email, otpCode?.Trim());
            if (!valid)
            {
                TempData["Error"] = "Invalid or expired OTP. Please try again.";
                ViewBag.Email = email;
                return View();
            }

            HttpContext.Session.SetString("OtpVerified", "true");
            TempData["Success"] = "Email verified! Please complete your registration.";
            return RedirectToAction("CompleteRegistration");
        }

        [SessionAuthorize("Driver")]
        [HttpGet]
        public IActionResult RegisterVehicle()
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var driver = _logisticRepo.GetDriverByUserId(userId);
            ViewBag.DriverStatus = driver?.Status ?? "Pending";
            return View();
        }

        [SessionAuthorize("Driver")]
        [HttpPost]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> RegisterVehicle(
            string vehicleNumber,
            decimal capacity,
            IFormFile vehicleRcFile)
        {
            int driverId = HttpContext.Session.GetInt32("DriverId") ?? 0;

            if (driverId == 0)
            {
                TempData["Error"] = "Session expired. Please login again.";
                return RedirectToAction("Login", "Admin");
            }

            if (string.IsNullOrWhiteSpace(vehicleNumber))
            {
                TempData["Error"] = "Vehicle number is required.";
                return View();
            }

            if (capacity <= 0)
            {
                TempData["Error"] = "Invalid capacity.";
                return View();
            }

            if (vehicleRcFile == null || vehicleRcFile.Length == 0)
            {
                TempData["Error"] = "Vehicle RC document is required.";
                return View();
            }

            string extension = Path.GetExtension(vehicleRcFile.FileName).ToLower();
            HashSet<string> allowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".pdf" };

            if (!allowedExtensions.Contains(extension))
            {
                TempData["Error"] = "Only JPG, PNG and PDF files are allowed.";
                return View();
            }

            if (vehicleRcFile.Length > 5 * 1024 * 1024)
            {
                TempData["Error"] = "File size cannot exceed 5 MB.";
                return View();
            }

            string webRoot = _fileUpload.GetWebRootPath();

            try
            {
                string folderPath = Path.Combine(webRoot, "uploads", "documents", "vehiclercs");
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                string fileName = Guid.NewGuid().ToString() + extension;
                string fullPath = Path.Combine(folderPath, fileName);
                string dbPath = "/uploads/documents/vehiclercs/" + fileName;

                using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                {
                    await vehicleRcFile.CopyToAsync(fileStream);
                }

                await _logisticRepo.AddVehicleAsync(driverId, vehicleNumber, capacity, dbPath);

                TempData["Success"] = "Vehicle registered successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return View();
            }
        }

        [SessionAuthorize("Admin")]
        public IActionResult AllDrivers()
            => View(_logisticRepo.GetAllDrivers());

        [SessionAuthorize("Admin")]
        [HttpPost]
        public async Task<IActionResult> UpdateDriverStatus(int driverId, string status)
        {
            _logisticRepo.UpdateDriverStatus(driverId, status);
            try
            {
                var contact = _logisticRepo.GetDriverContactInfo(driverId);
                if (contact != null && !string.IsNullOrEmpty(contact.Email))
                {
                    await _logisticRepo.SendDriverStatusEmailAsync(
                        contact.Email, contact.DriverName, contact.Username, status);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DriverStatus Email] Failed: {ex.Message}");
            }

            TempData["Success"] = status == "Active"
                ? "Driver approved and notified by email."
                : $"Driver status updated to {status}.";

            return RedirectToAction("AllDrivers");
        }

        [SessionAuthorize("Admin")]
        public IActionResult AllVehicles()
            => View(_logisticRepo.GetAllVehicles());

        [SessionAuthorize("Admin")]
        [HttpPost]
        public async Task<IActionResult> UpdateVehicleStatus(int vehicleId, string status)
        {
            _logisticRepo.UpdateVehicleStatus(vehicleId, status);
            try
            {
                var contact = _logisticRepo.GetDriverContactInfoByVehicleId(vehicleId);
                if (contact != null && !string.IsNullOrEmpty(contact.Email))
                {
                    await _logisticRepo.SendVehicleStatusEmailAsync(
                        contact.Email, contact.DriverName, contact.VehicleNumber, status);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VehicleStatus Email] Failed: {ex.Message}");
            }

            TempData["Success"] = status == "Approved"
                ? "Vehicle approved and driver notified by email."
                : $"Vehicle status updated to {status}.";

            return RedirectToAction("AllVehicles");
        }

        [SessionAuthorize("Driver")]
        public IActionResult MyTransfers()
        {
            int driverId = HttpContext.Session.GetInt32("DriverId") ?? 0;
            if (driverId == 0)
            {
                TempData["Error"] = "Session expired. Please login again.";
                return RedirectToAction("Login", "Admin");
            }
            return View(_logisticRepo.GetDriverTransfers(driverId));
        }
    }
}