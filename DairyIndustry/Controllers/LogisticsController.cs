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
            var driver = _logisticRepo.GetDriverByUserId(userId);
            var vehicles = _logisticRepo.GetVehiclesByDriverId(driverId);
            ViewBag.Vehicles = vehicles;
            return View(driver);
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

        // FIX #2: Removed MemoryStream double-buffer. Now streams directly to disk
        // via FileStream + CopyToAsync — no thread pool blocking, no double memory copy.
        [HttpPost]
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
                // FIX #2 APPLIED HERE:
                // OLD: Read entire file into MemoryStream → Task.Run → WriteAllBytes (blocking)
                // NEW: Build path first (sync, cheap), then stream directly to disk with
                //      FileStream(Async flag) + CopyToAsync — truly async, no double-buffer.
                string folder = Path.Combine(webRoot, "uploads", "documents", "drivinglicences");
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string fileName = Guid.NewGuid() + extension;
                string fullPath = Path.Combine(folder, fileName);
                string dlPath = "/uploads/documents/drivinglicences/" + fileName;

                using (var fileStream = new FileStream(
                    fullPath, FileMode.Create, FileAccess.Write,
                    FileShare.None, bufferSize: 4096, useAsync: true))
                {
                    await drivingLicenseFile.CopyToAsync(fileStream);
                }

                // Password hashing is CPU-bound — Task.Run is correct here
                string passHash = await Task.Run(() =>
                    BCrypt.Net.BCrypt.HashPassword(password));

                // Async DB call
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

        // FIX #2: Same fix applied — direct FileStream instead of MemoryStream + WriteAllBytes.
        [SessionAuthorize("Driver")]
        [HttpPost]
        public async Task<IActionResult> RegisterVehicle(
            string vehicleNumber,
            decimal capacity,
            IFormFile vehicleRcFile)
        {
            // Read session BEFORE any await
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
                // FIX #2 APPLIED HERE:
                // OLD: Read entire file into MemoryStream → Task.Run → WriteAllBytes (blocking)
                // NEW: Build path first (sync, cheap), then stream directly to disk with
                //      FileStream(Async flag) + CopyToAsync — truly async, no double-buffer.
                string folderPath = Path.Combine(webRoot, "uploads", "documents", "vehiclercs");
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                string fileName = Guid.NewGuid().ToString() + extension;
                string fullPath = Path.Combine(folderPath, fileName);
                string dbPath = "/uploads/documents/vehiclercs/" + fileName;

                using (var fileStream = new FileStream(
                    fullPath, FileMode.Create, FileAccess.Write,
                    FileShare.None, bufferSize: 4096, useAsync: true))
                {
                    await vehicleRcFile.CopyToAsync(fileStream);
                }

                _logisticRepo.AddVehicle(driverId, vehicleNumber, capacity, dbPath);

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
        public IActionResult UpdateDriverStatus(int driverId, string status)
        {
            _logisticRepo.UpdateDriverStatus(driverId, status);
            try
            {
                var contact = _logisticRepo.GetDriverContactInfo(driverId);
                if (contact != null && !string.IsNullOrEmpty(contact.Email))
                    _logisticRepo.SendDriverStatusEmail(
                        contact.Email, contact.DriverName, contact.Username, status);
            }
            catch { }

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
        public IActionResult UpdateVehicleStatus(int vehicleId, string status)
        {
            _logisticRepo.UpdateVehicleStatus(vehicleId, status);
            try
            {
                var contact = _logisticRepo.GetDriverContactInfoByVehicleId(vehicleId);
                if (contact != null && !string.IsNullOrEmpty(contact.Email))
                    _logisticRepo.SendVehicleStatusEmail(
                        contact.Email, contact.DriverName, contact.VehicleNumber, status);
            }
            catch { }

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
