using DairyIndustry.Models.Admin;
using DairyIndustry.Models.FarmerModel;
using DairyIndustry.Repositories;
using DairyIndustry.Services;
using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.AspNetCore.Mvc;

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

        // =========================
        //  AJAX FOR CASCADING
        // =========================
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
        public IActionResult ListAllFarmers(bool? isActive, string search)
        {
            int staffId = GetStaffId();
            if (staffId == 0)
                return RedirectToAction("Login", "Auth");

            var data = _farmerRepo.GetAllFarmers(staffId, isActive, search);
            return View(data);
        }

        ////Details of farmer 
        //public IActionResult CenterFarmerDetails(int id)
        //{
        //    int staffId = Convert.ToInt32(HttpContext.Session.GetInt32("StaffId"));

        //    var farmer = _farmerRepo.GetFarmerByIdAsync(id, staffId);

        //    if (farmer == null)
        //        return NotFound();

        //    return View(farmer);
        //}


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
                message = isValid ? "OTP verified" : "Invalid or expired OTP"
            });
        }

        // GET
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

        // POST
        [HttpPost]
        public async Task<IActionResult> FarmerRegistrationByCenter(RegByCenterModel model)
        {
            int staffId = GetStaffId();
            if (staffId == 0)
                return RedirectToAction("Login", "Auth");

            model.States = _farmerRepo.GetStates();
            model.Cities = model.StateId > 0 ? _farmerRepo.GetCitiesByState(model.StateId.Value) : new List<CityModel>();
            model.Villages = model.CityId > 0 ? _farmerRepo.GetVillagesByCity(model.CityId.Value) : new List<VillageModel>();

            if (!string.IsNullOrEmpty(model.Email) && !model.IsEmailVerified)
                ModelState.AddModelError("Email", "Please verify the email address first.");

            if (!ModelState.IsValid)
                return View(model);

            // file uploads stay sync — local disk, no need for async
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
                    emailNote = $" (Email could not be sent: {mailEx.Message} — share credentials manually.)";
                }
            }

            TempData["FarmerCode"] = result.FarmerCode;
            TempData["Password"] = result.DefaultPassword;
            TempData["Success"] = "Farmer registered successfully!" + emailNote;
            return RedirectToAction("ListAllFarmers");
        }
        //Send OTP for both self registration or center registration of farmer
        //[HttpPost]
        //public JsonResult SendEmailOTP(string email)
        //{
        //    // 1. Generate OTP from DB (Stored Procedure)
        //    string otp = _farmerRepo.GenerateOtp(email);

        //    // 2. Send OTP via Email
        //    _emailService.SendOtpEmail(email, otp);

        //    return Json(new
        //    {
        //        success = true,
        //        message = "OTP sent successfully"
        //    });
        //}

        ////verifiaction of OTP
        //[HttpPost]
        //public JsonResult VerifyEmailOTP(string email, string otp)
        //{
        //    bool isValid = _farmerRepo.VerifyOtp(email, otp);

        //    return Json(new
        //    {
        //        success = isValid,
        //        message = isValid ? "OTP verified" : "Invalid or expired OTP"
        //    });
        //}

        // =========================
        // CREATE (GET)
        // =========================
        //public IActionResult FarmerRegistrationByCenter()
        //{
        //    var model = new RegByCenterModel
        //    {
        //        States = _farmerRepo.GetStates(),
        //        Cities = new List<CityModel>(),
        //        Villages = new List<VillageModel>()
        //    };

        //    return View(model);
        //}
        // =========================
        // CREATE (POST)
        // =========================
        //[HttpPost]
        //public IActionResult FarmerRegistrationByCenter(RegByCenterModel model)
        //{
        //    int staffId = GetStaffId();

        //    if (staffId == 0)
        //        return RedirectToAction("Login", "Auth");

        //    model.States = _farmerRepo.GetStates();

        //    model.Cities = model.StateId > 0? _farmerRepo.GetCitiesByState(model.StateId.Value): new List<CityModel>();

        //    model.Villages = model.CityId > 0? _farmerRepo.GetVillagesByCity(model.CityId.Value): new List<VillageModel>();

        //    if (!string.IsNullOrEmpty(model.Email) && !model.IsEmailVerified)
        //    {
        //        ModelState.AddModelError("Email", "Please verify the email address first.");
        //    }

        //    if (!ModelState.IsValid)
        //        return View(model);

        //    model.ProfilePhoto =_fileUploadService.SaveFile(model.PhotoFile,"profile");

        //    model.AadhaarCardPath = _fileUploadService.SaveFile(model.AadhaarFile,"aadhaar");

        //    model.PassbookPath =_fileUploadService.SaveFile(model.PassbookFile,"passbook");

        //    var result = _farmerRepo.AddFarmer(model, staffId);

        //    //After Registartion Email will be sent to Farmer that It is Approved
        //    string emailNote = string.Empty;

        //    if (!string.IsNullOrWhiteSpace(model.Email))
        //    {
        //        try
        //        {
        //            string loginUrl = $"{Request.Scheme}://{Request.Host}/Farmer/Login";

        //            _emailService.SendApprovalEmail(
        //                toEmail: model.Email,
        //                farmerCode: result.FarmerCode,
        //                defaultPassword: result.DefaultPassword,
        //                loginUrl: loginUrl);

        //            emailNote = $" Credentials emailed to {model.Email}.";
        //        }
        //        catch (Exception mailEx)
        //        {
        //            // SMTP failure — log it but do not crash the registration
        //            emailNote = $" (Email could not be sent: {mailEx.Message} — share credentials manually.)";
        //        }
        //    }
        //    // ─────────────────────────────────────────────────────────────────

        //    TempData["FarmerCode"] = result.FarmerCode;
        //    TempData["Password"] = result.DefaultPassword;
        //    TempData["Success"] = "Farmer registered successfully!" + emailNote;

        //    return RedirectToAction("ListAllFarmers");
        //}


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
                    return RedirectToAction("CenterFarmerDetails", new { id = model.FarmerId });

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

        //[HttpPost]
        //public IActionResult Edit(FarmerEditModel model)
        //{
        //    int staffId = GetStaffId();
        //    if (staffId == 0)
        //        return RedirectToAction("Login", "Auth");

        //    var existing = _farmerRepo.GetFarmerById(model.FarmerId, staffId);

        //    if (existing == null)
        //        return NotFound();

        //    // =========================
        //    // LOCK SENSITIVE FIELDS
        //    // =========================
        //    model.AadhaarNumber = existing.AadhaarNumber;
        //    model.Email = existing.Email;

        //    // =========================
        //    // PROFILE PHOTO
        //    // =========================
        //    if (model.PhotoFile != null && model.PhotoFile.Length > 0)
        //    {
        //        model.ProfilePhoto =
        //            _fileUploadService.SaveFile(model.PhotoFile, "profile");
        //    }
        //    else
        //    {
        //        model.ProfilePhoto = existing.ProfilePhoto;
        //    }

        //    try
        //    {
        //        // =========================
        //        // UPDATE FARMER
        //        // =========================
        //        int result = _farmerRepo.UpdateFarmer(model, staffId);

        //        // =========================
        //        // DOCUMENTS
        //        // =========================

        //        if (model.AadhaarFile != null && model.AadhaarFile.Length > 0)
        //        {
        //            var path = _fileUploadService.SaveFile(model.AadhaarFile, "aadhaar");
        //            _farmerRepo.UpdateFarmerDocument(staffId,model.FarmerId,"AadhaarCard",path);
        //        }

        //        if (model.PassbookFile != null && model.PassbookFile.Length > 0)
        //        {
        //            var path = _fileUploadService.SaveFile(model.PassbookFile, "passbook");
        //            _farmerRepo.UpdateFarmerDocument(staffId,model.FarmerId,"BankPassbook",path);
        //        }

        //        if (result > 0)
        //            return RedirectToAction("CenterFarmerDetails",new { id = model.FarmerId });

        //        TempData["Error"] = "Update failed.";
        //    }
        //    catch (Exception ex)
        //    {
        //        TempData["Error"] = ex.Message;
        //    }

        //    // =========================
        //    // RELOAD DROPDOWNS ON ERROR
        //    // =========================
        //    model.States = _farmerRepo.GetStates();

        //    model.Cities = model.StateId > 0? _farmerRepo.GetCitiesByState(model.StateId): new List<CityModel>();

        //    model.Villages = model.CityId > 0? _farmerRepo.GetVillagesByCity(model.CityId): new List<VillageModel>();

        //    return View(model);
        //}



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
                    PaperSize = PaperKind.A4,
                    Orientation = Orientation.Portrait,
                    Margins = new MarginSettings { Top = 10, Bottom = 10 }
                },
                Objects =
                {
                    new ObjectSettings
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
        public async Task<IActionResult> FarmerSelfRegistration(SelfRegisterViewModel model, IFormFile AadhaarDoc, IFormFile BankDoc, IFormFile ProfilePhotoFile)
        {
            model.States = _farmerRepo.GetStates();
            if (model.StateId != null)
                model.Cities = _farmerRepo.GetCitiesByState(model.StateId.Value);
            if (model.CityId != null)
                model.Villages = _farmerRepo.GetVillagesByCity(model.CityId.Value);
            if (model.VillageId != null)
                model.Centers = _collectionCenter.GetCentersByVillage(model.VillageId.Value);

            if (string.IsNullOrWhiteSpace(model.FarmerName))
                return Error(model, "Farmer name required");
            if (model.CenterId == null)
                return Error(model, "Select center");
            if (!model.IsEmailVerified)
            {
                TempData["Error"] = "Please verify your email first.";
                return View(model);
            }

            // file uploads stay sync — local disk
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
                TempData["Error"] = ex.Message;
                return View(model);
            }
        }

        // SELF REGISTRATION — GET
        //[HttpGet]
        //public IActionResult FarmerSelfRegistration()
        //{
        //    var model = new SelfRegisterViewModel
        //    {
        //        States = _farmerRepo.GetStates()
        //    };
        //    return View(model);
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public IActionResult FarmerSelfRegistration(SelfRegisterViewModel model,IFormFile AadhaarDoc,IFormFile BankDoc,IFormFile ProfilePhotoFile)
        //{
        //    model.States = _farmerRepo.GetStates();

        //    if (model.StateId != null)
        //        model.Cities = _farmerRepo.GetCitiesByState(model.StateId.Value);

        //    if (model.CityId != null)
        //        model.Villages = _farmerRepo.GetVillagesByCity(model.CityId.Value);

        //    if (model.VillageId != null)
        //        model.Centers = _collectionCenter.GetCentersByVillage(model.VillageId.Value);

        //    if (string.IsNullOrWhiteSpace(model.FarmerName))
        //        return Error(model, "Farmer name required");

        //    if (model.CenterId == null)
        //        return Error(model, "Select center");

        //    if (!model.IsEmailVerified)
        //    {
        //        TempData["Error"] = "Please verify your email first.";
        //        return View(model);
        //    }
        //    model.ProfilePhotoPath =_fileUploadService.SaveFile(model.ProfilePhotoFile,"profile");

        //    model.AadhaarDocPath =_fileUploadService.SaveFile(model.AadhaarFile,"aadhaar");

        //    model.PassbookDocPath =_fileUploadService.SaveFile(model.PassbookFile,"passbook");

        //    try
        //    {
        //        _farmerRepo.SelfRegisterFarmer(model);
        //        return RedirectToAction("RegisterSuccess");
        //    }
        //    catch (Exception ex)
        //    {
        //        TempData["Error"] = ex.Message;
        //        return View(model);
        //    }
        //}

        private IActionResult Error(SelfRegisterViewModel model, string msg)
        {
            TempData["Error"] = msg;
            return View(model);
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
