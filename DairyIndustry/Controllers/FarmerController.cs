using DairyIndustry.Filters;
using DairyIndustry.Models.Admin;
using DairyIndustry.Models.FarmerModel;
using DairyIndustry.Repositories;
using DairyIndustry.Services;
using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace DairyIndustry.Controllers
{
    public class FarmerController : Controller
    {
        private readonly IFarmerRepository _farmerRepo;
        private readonly IAdminRepository _adminRepo;
        private readonly IWebHostEnvironment _env;
        private readonly IConverter _converter;
        private readonly ICollectionCenterRepository _collectionCenter;
        private readonly EmailService _emailService;
        private readonly FileUploadService _fileUploadService;

        public FarmerController(IFarmerRepository farmerRepo,IAdminRepository adminRepo,IWebHostEnvironment env,IConverter converter,ICollectionCenterRepository collectionRepo,EmailService emailService,FileUploadService fileUploadService)
        {
            _farmerRepo = farmerRepo;
            _adminRepo = adminRepo;
            _collectionCenter = collectionRepo;
            _env = env;
            _converter = converter;
            _emailService = emailService;
            _fileUploadService = fileUploadService;
        }

        //dashboard

        public IActionResult Dashboard()
        {
            int farmerId = HttpContext.Session.GetInt32("FarmerId") ?? 0;

            if (farmerId == 0)
                return RedirectToAction("Login");

            ViewData["Title"] = "Dashboard";

            var vm = _farmerRepo.GetDashboard(farmerId);

            return View(vm);
        }

        //For Session
        private int GetFarmerId()
        {
            return HttpContext.Session.GetInt32("FarmerId") ?? 0;
        }
        private int GetStaffId()
        {
            return HttpContext.Session.GetInt32("StaffId") ?? 0;
        }

        [HttpGet]
        public JsonResult GetCities(int stateId)
        {
            var cities = _farmerRepo.GetCitiesByState(stateId);
            return Json(cities);
        }

        [HttpGet]
        public JsonResult GetVillages(int cityId)
        {
            var villages = _farmerRepo.GetVillagesByCity(cityId);
            return Json(villages);
        }

        // List Of All Farmers After Approved from Center
        public IActionResult ListAllFarmers()
        {
            int staffId = GetStaffId();
            if (staffId == 0)
                return RedirectToAction("Login", "Auth");

            var data = _farmerRepo.GetAllFarmers(staffId);
            return View(data);
        }

        // Details of farmer
        public async Task<IActionResult> CenterFarmerDetails(int id)
        {
            int staffId = Convert.ToInt32(HttpContext.Session.GetInt32("StaffId"));
            var farmer = await _farmerRepo.GetFarmerByIdAsync(id, staffId);
            if (farmer == null)
                return NotFound();
            return View(farmer);
        }

        // Toggle Status - Activate/Decativate of farmer
        public IActionResult ToggleStatus(int id, bool isActive)
        {
            try
            {
                _farmerRepo.ToggleFarmerStatus(GetStaffId(), id, isActive);
                TempData["Success"] = "Farmer status updated successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("ListAllfarmers");
        }

        [HttpPost]
        public async Task<JsonResult> SendEmailOTP(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return Json(new { success = false, message = "Email address is required." });

            // Check if email already exists in an active/pending farmer
            bool isDuplicate = await _farmerRepo.IsEmailAlreadyRegisteredAsync(email);
            if (isDuplicate)
            {
                return Json(new
                {
                    success = false,
                    message = "Unable to send OTP. Please try a different email or contact support."
                });
            }

            string otp = await _farmerRepo.GenerateOtpAsync(email);
            await _emailService.SendOtpEmailAsync(email, otp);
            return Json(new { success = true, message = "OTP sent successfully" });
        }

        [HttpPost]
        public async Task<JsonResult> VerifyEmailOTP(string email, string otp)
        {

            bool isValid = await _farmerRepo.VerifyOtpAsync(email, otp);
            return Json(new
            {
                success = isValid,
                message = isValid ? "Email verified successfully.": "Invalid or expired OTP. Please try again."
            });
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // STEP 1 — GET: Show Farmer Code entry page
        // ─────────────────────────────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // STEP 1 — POST (fetch/AJAX): Validate code, generate OTP, send email
        // ─────────────────────────────────────────────────────────────────────────────
        [HttpPost]
        public async Task<JsonResult> SendForgotPasswordOTP(string farmerCode)
        {
            if (string.IsNullOrWhiteSpace(farmerCode))
                return Json(new { success = false, message = "Please enter your Farmer Code." });

            var farmer = _farmerRepo.GetFarmerEmailByCode(farmerCode.Trim());
            if (farmer == null)
                return Json(new { success = false, message = "Farmer Code not found or account is inactive." });

            if (string.IsNullOrWhiteSpace(farmer.Value.Email))
                return Json(new { success = false, message = "No email registered. Please contact your collection center." });

            string otp = await _farmerRepo.GenerateOtpAsync(farmer.Value.Email);
            await _emailService.SendForgotPasswordOtpAsync(farmer.Value.Email, farmer.Value.FarmerName, otp);

            string email = farmer.Value.Email;
            int atIdx = email.IndexOf('@');
            string masked = atIdx > 2? email[..2] + new string('*', atIdx - 2) + email[atIdx..]: email[..1] + "***" + email[atIdx..];

            return Json(new { success = true, maskedEmail = masked });
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // STEP 2 — GET: Show 6-box OTP verification page
        // ─────────────────────────────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult VerifyOtp(string farmerCode)
        {
            if (string.IsNullOrWhiteSpace(farmerCode))
                return RedirectToAction("ForgotPassword");

            var farmer = _farmerRepo.GetFarmerEmailByCode(farmerCode.Trim());
            if (farmer == null)
            {
                TempData["Error"] = "Invalid Farmer Code. Please try again.";
                return RedirectToAction("ForgotPassword");
            }

            string email = farmer.Value.Email ?? "";
            int atIdx = email.IndexOf('@');
            string masked = atIdx > 2? email[..2] + new string('*', atIdx - 2) + email[atIdx..]: email[..1] + "***" + email[atIdx..];

            ViewBag.FarmerCode = farmerCode;
            ViewBag.MaskedEmail = masked;
            return View();
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // STEP 2 — POST: Verify OTP → if valid, show ResetPassword view
        // ─────────────────────────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string farmerCode, string otp)
        {
            if (string.IsNullOrWhiteSpace(farmerCode) || string.IsNullOrWhiteSpace(otp))
            {
                TempData["Error"] = "Invalid request. Please start over.";
                return RedirectToAction("ForgotPassword");
            }

            var farmer = _farmerRepo.GetFarmerEmailByCode(farmerCode.Trim());
            if (farmer == null)
            {
                TempData["Error"] = "Invalid Farmer Code. Please start over.";
                return RedirectToAction("ForgotPassword");
            }

            // Verify OTP — marks IsUsed = 1 after success
            bool otpValid = await _farmerRepo.VerifyOtpAsync(farmer.Value.Email, otp.Trim());
            if (!otpValid)
            {
                string email = farmer.Value.Email ?? "";
                int atIdx = email.IndexOf('@');
                string masked = atIdx > 2? email[..2] + new string('*', atIdx - 2) + email[atIdx..]: email[..1] + "***" + email[atIdx..];

                TempData["Error"] = "Invalid or expired OTP. Please try again.";
                ViewBag.FarmerCode = farmerCode;
                ViewBag.MaskedEmail = masked;
                return View("VerifyOtp");
            }

            // OTP verified — show password reset form
            // We pass farmerCode only; OTP is no longer needed (already verified above)
            ViewBag.FarmerCode = farmerCode;
            ViewBag.Otp = otp;   // carried as hidden field, but NOT re-verified in step 3
            return View("ResetPassword");
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // STEP 3 — POST: Save new password directly (OTP already verified in step 2)
        // ─────────────────────────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ConfirmResetPassword(
            string farmerCode, string otp, string newPassword, string confirmPassword)
        {
            // Guard: must have farmer code
            if (string.IsNullOrWhiteSpace(farmerCode))
            {
                TempData["Error"] = "Invalid request. Please start over.";
                return RedirectToAction("ForgotPassword");
            }

            // Server-side password validation
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                TempData["Error"] = "Password must be at least 6 characters.";
                ViewBag.FarmerCode = farmerCode;
                ViewBag.Otp = otp;
                return View("ResetPassword");
            }

            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "Passwords do not match.";
                ViewBag.FarmerCode = farmerCode;
                ViewBag.Otp = otp;
                return View("ResetPassword");
            }

            // Reset password in DB — calls usp_Farmer_ResetPassword
            // which does HASHBYTES('SHA2_256', @NewPassword) and sets IsFirstLogin = 0
            bool reset = _farmerRepo.ResetFarmerPassword(farmerCode.Trim(), newPassword);
            if (!reset)
            {
                TempData["Error"] = "Password reset failed. Please try again.";
                ViewBag.FarmerCode = farmerCode;
                ViewBag.Otp = otp;
                return View("ResetPassword");
            }

            TempData["Success"] = "Password reset successfully! Please log in with your new password.";
            return RedirectToAction("Login");
        }


        [SessionAuthorize("Collection Agent")]
        [HttpGet]
        public IActionResult FarmerRegistrationByCenter()
        {
            var model = new RegByCenterModel
            {
                States = _farmerRepo.GetStates(),
                Cities = new List<CityModel>(),
                Villages = new List<VillageModel>()
            };
            return View(model);
        }

        //[SessionAuthorize("Collection Agent")]
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> FarmerRegistrationByCenter(RegByCenterModel model)
        //{
        //    int staffId = GetStaffId();

        //    if (staffId == 0)
        //        return RedirectToAction("Login", "Auth");

        //    // Reload dropdowns
        //    model.States = _farmerRepo.GetStates();
        //    model.Cities = model.StateId > 0? _farmerRepo.GetCitiesByState(model.StateId.Value): new List<CityModel>();
        //    model.Villages = model.CityId > 0? _farmerRepo.GetVillagesByCity(model.CityId.Value): new List<VillageModel>();

        //    // Email verification check
        //    if (!string.IsNullOrEmpty(model.Email) && !model.IsEmailVerified)
        //    {
        //        ModelState.AddModelError("Email", "Please verify the email address first.");
        //    }

        //    if (!ModelState.IsValid)
        //    {
        //        return View(model);
        //    }

        //    try
        //    {
        //        // Upload files
        //        model.ProfilePhoto = _fileUploadService.SaveFile(model.PhotoFile, "profile");
        //        model.AadhaarCardPath = _fileUploadService.SaveFile(model.AadhaarFile, "aadhaar");
        //        model.PassbookPath = _fileUploadService.SaveFile(model.PassbookFile, "passbook");

        //        // Register farmer
        //        var result = await _farmerRepo.AddFarmerAsync(model, staffId);

        //        string emailNote = string.Empty;

        //        if (!string.IsNullOrWhiteSpace(model.Email))
        //        {
        //            try
        //            {
        //                string loginUrl = $"{Request.Scheme}://{Request.Host}/Farmer/Login";

        //                await _emailService.SendApprovalEmailAsync(
        //                    toEmail: model.Email,
        //                    farmerCode: result.FarmerCode,
        //                    defaultPassword: result.DefaultPassword,
        //                    loginUrl: loginUrl);

        //                emailNote = $" Credentials emailed to {model.Email}.";
        //            }
        //            catch (Exception mailEx)
        //            {
        //                emailNote = $" (Email could not be sent: {mailEx.Message}. Share credentials manually.)";
        //            }
        //        }

        //        TempData["FarmerCode"] = result.FarmerCode;
        //        TempData["Password"] = result.DefaultPassword;
        //        TempData["Success"] = "Farmer registered successfully!" + emailNote;

        //        return RedirectToAction("FarmerRegistrationByCenter");
        //    }
        //    catch (SqlException ex)
        //    {
        //        // Show SP errors in validation alert

        //        if (ex.Message.Contains("phone", StringComparison.OrdinalIgnoreCase))
        //        {
        //            ModelState.AddModelError("", "Farmer with this phone number already exists or is pending approval.");
        //        }
        //        else if (ex.Message.Contains("email", StringComparison.OrdinalIgnoreCase))
        //        {
        //            ModelState.AddModelError("", "This email is already registered.");
        //        }
        //        else if (ex.Message.Contains("aadhaar", StringComparison.OrdinalIgnoreCase))
        //        {
        //            ModelState.AddModelError("", "Aadhaar number already exists.");
        //        }
        //        else
        //        {
        //            ModelState.AddModelError("", ex.Message);
        //        }

        //        return View(model);
        //    }
        //    catch (Exception ex)
        //    {
        //        ModelState.AddModelError("", "An unexpected error occurred: " + ex.Message);
        //        return View(model);
        //    }
        //}
        [SessionAuthorize("Collection Agent")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FarmerRegistrationByCenter(RegByCenterModel model)
        {
            int staffId = GetStaffId();
            if (staffId == 0)
                return RedirectToAction("Login", "Auth");

            // Reload dropdowns always
            model.States = _farmerRepo.GetStates();
            model.Cities = model.StateId > 0 ? _farmerRepo.GetCitiesByState(model.StateId.Value) : new List<CityModel>();
            model.Villages = model.CityId > 0 ? _farmerRepo.GetVillagesByCity(model.CityId.Value) : new List<VillageModel>();

            // Clear all model-annotation errors (we only want our custom ones)
            //ModelState.Clear();

            // ── SERVER-SIDE DUPLICATE CHECKS ONLY ──
            if (!string.IsNullOrWhiteSpace(model.Phone))
            {
                bool phoneDupe = await _farmerRepo.IsPhoneAlreadyRegisteredAsync(model.Phone);
                if (phoneDupe)
                    ModelState.AddModelError("Phone", "A farmer with this phone number already exists or is pending approval.");
            }

            if (!string.IsNullOrWhiteSpace(model.AadhaarNumber))
            {
                bool aadhaarDupe = await _farmerRepo.IsAadhaarAlreadyRegisteredAsync(model.AadhaarNumber);
                if (aadhaarDupe)
                    ModelState.AddModelError("AadhaarNumber", "This Aadhaar number is already registered.");
            }

            if (!string.IsNullOrWhiteSpace(model.Email) && !model.IsEmailVerified)
            {
                ModelState.AddModelError("Email", "Please verify the email address first.");
            }

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                model.ProfilePhoto = _fileUploadService.SaveFile(model.PhotoFile, "profile");
                model.AadhaarCardPath = _fileUploadService.SaveFile(model.AadhaarFile, "aadhaar");
                model.PassbookPath = _fileUploadService.SaveFile(model.PassbookFile, "passbook");

                var result = await _farmerRepo.AddFarmerAsync(model, staffId);

                string emailNote = string.Empty;
                if (!string.IsNullOrWhiteSpace(model.Email))
                {
                    try
                    {
                        string loginUrl = $"{Request.Scheme}://{Request.Host}/Farmer/Login";
                        await _emailService.SendApprovalEmailAsync(
                            toEmail: model.Email,
                            farmerCode: result.FarmerCode,
                            defaultPassword: result.DefaultPassword,
                            loginUrl: loginUrl);
                        emailNote = $" Credentials emailed to {model.Email}.";
                    }
                    catch (Exception mailEx)
                    {
                        emailNote = $" (Email could not be sent: {mailEx.Message}. Share credentials manually.)";
                    }
                }

                TempData["FarmerCode"] = result.FarmerCode;
                TempData["Password"] = result.DefaultPassword;
                TempData["Success"] = "Farmer registered successfully!" + emailNote;

                return RedirectToAction("FarmerRegistrationByCenter");
            }
            catch (SqlException ex)
            {
                // SP-level duplicate errors → show next to the relevant field
                if (ex.Message.Contains("phone", StringComparison.OrdinalIgnoreCase))
                    ModelState.AddModelError("Phone", "Farmer with this phone number already exists or is pending approval.");
                else if (ex.Message.Contains("email", StringComparison.OrdinalIgnoreCase))
                    ModelState.AddModelError("Email", "This email is already registered.");
                else if (ex.Message.Contains("aadhaar", StringComparison.OrdinalIgnoreCase))
                    ModelState.AddModelError("AadhaarNumber", "Aadhaar number already exists.");
                else
                    ModelState.AddModelError("", ex.Message);

                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
        }

        [SessionAuthorize("Collection Agent")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            int staffId = GetStaffId();
            if (staffId == 0)
                return RedirectToAction("Login", "Auth");

            var model = await _farmerRepo.GetFarmerByIdAsync(id, staffId);
            if (model == null)
                return NotFound();

            model.States = _farmerRepo.GetStates();
            model.Cities = model.StateId > 0 ? _farmerRepo.GetCitiesByState(model.StateId) : new List<CityModel>();
            model.Villages = model.CityId > 0 ? _farmerRepo.GetVillagesByCity(model.CityId) : new List<VillageModel>();

            return View(model);
        }

        [SessionAuthorize("Collection Agent")]
        [HttpPost]
        public async Task<IActionResult> Edit(FarmerEditModel model)
        {
            int staffId = GetStaffId();
            if (staffId == 0)
                return RedirectToAction("Login", "Auth");

            var existing = await _farmerRepo.GetFarmerByIdAsync(model.FarmerId, staffId);
            if (existing == null)
                return NotFound();

            // =========================
            // LOCK SENSITIVE FIELDS
            // =========================
            model.AadhaarNumber = existing.AadhaarNumber;
            model.Email = existing.Email;

            // =========================
            // PROFILE PHOTO — stays sync (local disk)
            // =========================
            model.ProfilePhoto = (model.PhotoFile != null && model.PhotoFile.Length > 0)
                ? _fileUploadService.SaveFile(model.PhotoFile, "profile")
                : existing.ProfilePhoto;

            try
            {
                // =========================
                // UPDATE FARMER
                // =========================
                int result = await _farmerRepo.UpdateFarmerAsync(model, staffId);

                // =========================
                // DOCUMENTS — stays sync (local disk)
                // =========================
                if (model.AadhaarFile != null && model.AadhaarFile.Length > 0)
                {
                    var path = _fileUploadService.SaveFile(model.AadhaarFile, "aadhaar");
                    await _farmerRepo.UpdateFarmerDocumentAsync(staffId, model.FarmerId, "AadhaarCard", path);
                }

                if (model.PassbookFile != null && model.PassbookFile.Length > 0)
                {
                    var path = _fileUploadService.SaveFile(model.PassbookFile, "passbook");
                    await _farmerRepo.UpdateFarmerDocumentAsync(staffId, model.FarmerId, "BankPassbook", path);
                }
                if (result > 0)
                {
                    TempData["Success"] = "Farmer details updated successfully.";
                    return RedirectToAction("CenterFarmerDetails",
                        new { id = model.FarmerId });
                }

                //if (result > 0)
                //    return RedirectToAction("CenterFarmerDetails", new { id = model.FarmerId });

                TempData["Error"] = "Update failed.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            // =========================
            // RELOAD DROPDOWNS ON ERROR
            // =========================
            model.States = _farmerRepo.GetStates();
            model.Cities = model.StateId > 0 ? _farmerRepo.GetCitiesByState(model.StateId) : new List<CityModel>();
            model.Villages = model.CityId > 0 ? _farmerRepo.GetVillagesByCity(model.CityId) : new List<VillageModel>();

            return View(model);
        }

       

        //farmer login
        public IActionResult Login()
        {
            return View();
        }

        //  Login POST
        [HttpPost]
        public IActionResult Login(FarmerLoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var farmer = _farmerRepo.FarmerLogin(model.FarmerCode, model.Password);

            if (farmer == null)
            {
                ModelState.AddModelError("", "Invalid Farmer Code or Password.");
                return View(model);
            }

            // Store core session keys
            HttpContext.Session.SetInt32("FarmerId", farmer.FarmerId);
            HttpContext.Session.SetString("FarmerCode", farmer.FarmerCode);
            HttpContext.Session.SetString("FarmerName", farmer.FarmerName);

            if (farmer.IsFirstLogin)
                return RedirectToAction("ChangePassword");

            return RedirectToAction("Dashboard");
        }


        [HttpGet]
        public IActionResult ChangePassword()
        {
            int farmerId = HttpContext.Session.GetInt32("FarmerId") ?? 0;
            if (farmerId == 0)
                return RedirectToAction("Login");

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            int farmerId = HttpContext.Session.GetInt32("FarmerId") ?? 0;
            if (farmerId == 0)
                return RedirectToAction("Login");

            // Client-side guard (browser usually catches these first)
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                TempData["Error"] = "New password must be at least 6 characters.";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "New password and confirmation do not match.";
                return View();
            }

            if (currentPassword == newPassword)
            {
                TempData["Error"] = "New password must be different from the current password.";
                return View();
            }

            var result = _farmerRepo.ChangePassword(farmerId, currentPassword, newPassword);

            if (result == "Success")
            {
                TempData["Success"] = "Password changed successfully. Welcome!";
                return RedirectToAction("Dashboard");
            }

            // SP returned "InvalidPassword"
            TempData["Error"] = "Current password is incorrect. " +
                                "Your default password is the last 4 digits of your registered mobile number.";
            return View();
        }

        // =========================
        // LOGOUT
        // =========================
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

       

        //Tody's milk Entries
        public IActionResult TodayMilk()
        {
            int farmerId = HttpContext.Session.GetInt32("FarmerId") ?? 0;

            if (farmerId == 0)
                return RedirectToAction("Login", "Farmer");

            var data = _farmerRepo.GetTodayMilkEntries(farmerId);

            return View(data);
        }

        //all milk Entries for farmer
        public IActionResult AllMilkEntriesFarmer()
        {
            int farmerId = HttpContext.Session.GetInt32("FarmerId") ?? 0;

            if (farmerId == 0)
                return RedirectToAction("Login", "Farmer");

            var data = _farmerRepo.GetAllMilkEntriesFarmer(farmerId);

            return View(data);
        }

        // VIEW RECEIPT
        public IActionResult ViewReceipt(int id)
        {
            var data = _farmerRepo.GetReceiptByCollectionId(id);

            if (data == null)
                return NotFound();

            return View(data);
        }

        // DOWNLOAD PDF
        public IActionResult DownloadReceipt(int id)
        {
            var r = _farmerRepo.GetReceiptByCollectionId(id);
            if (r == null) return NotFound();

            // Read HTML file
            
            string path = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/templates/Receipt.html");
            string html = System.IO.File.ReadAllText(path);

            // Replace placeholders
            html = html.Replace("{{CenterName}}", r.CenterName)
                       .Replace("{{ReceiptNumber}}", r.ReceiptNumber ?? "N/A")
                       .Replace("{{Date}}", r.CollectionDate.ToString("dd-MM-yyyy"))
                       .Replace("{{Shift}}", r.Shift)
                       .Replace("{{FarmerName}}", r.FarmerName)
                       .Replace("{{FarmerCode}}", r.FarmerCode)
                       .Replace("{{CollectionId}}", r.CollectionId.ToString())
                        
                       .Replace("{{MilkType}}", r.MilkTypeName)
                       .Replace("{{Quantity}}", r.Quantity.ToString("0.00"))
                       .Replace("{{Fat}}", r.AppliedFat?.ToString("0.0") ?? "0")
                       .Replace("{{CLR}}", r.AppliedCLR?.ToString("0.0") ?? "0")
                       .Replace("{{Rate}}", r.RatePerLiter?.ToString("0.00"))
                       .Replace("{{Amount}}", r.Amount?.ToString("0.00"));

            // Convert HTML → PDF
            var doc = new HtmlToPdfDocument()
            {
                GlobalSettings = new GlobalSettings
                {
                    ColorMode = ColorMode.Color,
                    Orientation = Orientation.Portrait,
                    PaperSize = new PechkinPaperSize("170mm", "100mm"),
                    Margins = new MarginSettings { Top = 6, Bottom = 6, Left = 8, Right = 8 }
                },
                Objects =
                {
                    new ObjectSettings()
                    {
                        HtmlContent = html,
                        WebSettings = { DefaultEncoding = "utf-8" }
                    }
                }
            };


            var pdf = _converter.Convert(doc);

            return File(pdf, "application/pdf", "MilkReceipt.pdf");
        }

      
        //farmer profile
        public IActionResult Profile()
        {
            int farmerId = HttpContext.Session.GetInt32("FarmerId") ?? 0;

            if (farmerId == 0)
                return RedirectToAction("Login");

            var model = _farmerRepo.GetFarmerProfile(farmerId);

            return View(model);
        }

        [HttpGet]
        public IActionResult FarmerSelfRegistration()
        {
            var model = new SelfRegisterViewModel
            {
                States = _farmerRepo.GetStates()
            };
            return View(model);
        }

       

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FarmerSelfRegistration(SelfRegisterViewModel model)
        {
            // Always reload dropdown data in case we return the view
            model.States = _farmerRepo.GetStates();
            if (model.StateId != null)
                model.Cities = _farmerRepo.GetCitiesByState(model.StateId.Value);
            if (model.CityId != null)
                model.Villages = _farmerRepo.GetVillagesByCity(model.CityId.Value);
            if (model.VillageId != null)
                model.Centers = _collectionCenter.GetCentersByVillage(model.VillageId.Value);

            // ── Email verification gate ──────────────────────────────────────
            if (!model.IsEmailVerified)
            {
                TempData["Error"] = "Please verify your email address before submitting.";
                return View(model);
            }

            // ── File uploads ─────────────────────────────────────────────────
            // NOTE: Files are required — model annotations enforce this,
            // but we guard here in case binding silently skips them.
            if (model.ProfilePhotoFile == null || model.ProfilePhotoFile.Length == 0)
            {
                ModelState.AddModelError("ProfilePhotoFile", "Profile photo is required.");
                return View(model);
            }
            if (model.AadhaarFile == null || model.AadhaarFile.Length == 0)
            {
                ModelState.AddModelError("AadhaarFile", "Aadhaar document is required.");
                return View(model);
            }
            if (model.PassbookFile == null || model.PassbookFile.Length == 0)
            {
                ModelState.AddModelError("PassbookFile", "Bank passbook document is required.");
                return View(model);
            }

            model.ProfilePhotoPath = _fileUploadService.SaveFile(model.ProfilePhotoFile, "profile");
            model.AadhaarDocPath = _fileUploadService.SaveFile(model.AadhaarFile, "aadhaar");
            model.PassbookDocPath = _fileUploadService.SaveFile(model.PassbookFile, "passbook");

            try
            {
                await _farmerRepo.SelfRegisterFarmerAsync(model);
                return RedirectToAction("RegisterSuccess");
            }
            catch (Exception ex)
            {
               
                var msg = ex.Message ?? "";

                bool isDuplicate =
                    msg.Contains("Phone already", StringComparison.OrdinalIgnoreCase) ||
                    msg.Contains("Email already", StringComparison.OrdinalIgnoreCase) ||
                    msg.Contains("Aadhaar already", StringComparison.OrdinalIgnoreCase);

                bool isInvalidLocation =
                    msg.Contains("Invalid village", StringComparison.OrdinalIgnoreCase) ||
                    msg.Contains("does not belong", StringComparison.OrdinalIgnoreCase);

                if (isDuplicate)
                {
                    // Generic — does NOT reveal which field is duplicate
                    TempData["Error"] =
                        "Registration could not be completed. " +
                        "If you have already registered, please check your status or contact support.";
                }
                else if (isInvalidLocation)
                {
                    TempData["Error"] = "Invalid location selected. Please go back and reselect your village and center.";
                }
                else
                {
                    TempData["Error"] = "An unexpected error occurred. Please try again or contact support.";
                }

                return View(model);
            }
        }

        private IActionResult Error(SelfRegisterViewModel model, string msg)
        {
            TempData["Error"] = msg;
            return View(model);
        }

        [HttpPost]
        public async Task<JsonResult> CheckPhoneDuplicate(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return Json(new { duplicate = false });

            bool exists = await _farmerRepo.IsPhoneAlreadyRegisteredAsync(phone);
            return Json(new { duplicate = exists });
        }

        [HttpPost]
        public async Task<JsonResult> CheckEmailDuplicate(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return Json(new { duplicate = false });

            bool exists = await _farmerRepo.IsEmailAlreadyRegisteredAsync(email);
            return Json(new { duplicate = exists });
        }

        [HttpPost]
        public async Task<JsonResult> CheckAadhaarDuplicate(string aadhaar)
        {
            if (string.IsNullOrWhiteSpace(aadhaar))
                return Json(new { duplicate = false });

            bool exists = await _farmerRepo.IsAadhaarAlreadyRegisteredAsync(aadhaar);
            return Json(new { duplicate = exists });
        }

        // REGISTER SUCCESS — static thank-you page
        [HttpGet]
        public IActionResult RegisterSuccess()
        {
            return View();
        }

        // CHECK STATUS — GET
        [HttpGet]
        public IActionResult CheckStatus()
        {
            return View(new FarmerStatusViewModel());
        }

        // CHECK STATUS — POST
        // Looks up registration by phone and shows result.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CheckStatus(FarmerStatusViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Phone))
            {
                TempData["Error"] = "Please enter your phone number.";
                model.Searched = false;
                return View(model);
            }

            var result = _farmerRepo.GetFarmerStatusByPhone(model.Phone.Trim());

            model.Searched = true;

            if (result != null)
            {
                model.FarmerId = result.FarmerId;
                model.FarmerName = result.FarmerName;
                model.FarmerCode = result.FarmerCode;
                model.ApprovalStatus = result.ApprovalStatus;
                model.ApprovalRemark = result.ApprovalRemark;
                model.CenterName = result.CenterName;
            }

            return View(model);
        }
        [HttpGet]
        public JsonResult GetCenters(int villageId)
        {
            var centers = _collectionCenter.GetCentersByVillage(villageId);
            return Json(centers);
        }


        //milk rejection entries (history) for farmer
        public IActionResult RejectionHistory(DateTime? fromDate, DateTime? toDate)
        {
            int farmerId = HttpContext.Session.GetInt32("FarmerId") ?? 0;

            if (farmerId == 0)
                return RedirectToAction("Login");

            var data = _farmerRepo.GetRejectionHistory(farmerId, fromDate, toDate);

            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            if (data.Count == 0)
                TempData["Info"] = "No rejections found for this period.";

            return View(data);
        }
    }

}
