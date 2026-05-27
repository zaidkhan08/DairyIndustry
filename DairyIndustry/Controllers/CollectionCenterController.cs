using DairyIndustry.Filters;
using DairyIndustry.Models.Collection;
using DairyIndustry.Models.FarmerModel;
using DairyIndustry.Repositories;
using DairyIndustry.Services;
using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace DairyIndustry.Controllers
{
    [ServiceFilter(typeof(ActionLogFilter))]
    public class CollectionCenterController : Controller
    {
        private readonly IConverter _converter;
        private readonly ICollectionCenterRepository _collectionCenterRepo;
        private readonly IFarmerRepository _farmerRepo;
        private readonly EmailService _emailService;


        public CollectionCenterController(ICollectionCenterRepository repository, IConverter converter, IFarmerRepository farmerRepo,EmailService emailService)
        {
            _collectionCenterRepo = repository;
            _converter = converter;
            _farmerRepo = farmerRepo;
            _emailService = emailService;
          
        }


        [SessionAuthorize("Collection Agent")]
        public IActionResult Dashboard()
        {
            var staffId = HttpContext.Session.GetInt32("StaffId");
            if (staffId == null)
                return RedirectToAction("Login", "Admin");


            var model = new CollectionAgentDashboardViewModel
            {
                StaffCenter = _collectionCenterRepo.GetStaffCenter(staffId.Value),
                TodaySummary = _collectionCenterRepo.GetTodaySummary(staffId.Value),
                Shifts = _collectionCenterRepo.GetShiftStatus(staffId.Value),
                Inventory = _collectionCenterRepo.GetInventory(staffId.Value),
                FarmerStats = _collectionCenterRepo.GetFarmerStats(staffId.Value)
            };

            if (model == null)
            {
                TempData["Error"] = "Unable to load dashboard.";
                return RedirectToAction("Login", "Auth");
            }

            return View(model);

        }

        // ─────────────────────────────────────────────────────────────
        // BATCH STATUS PAGE
        // ─────────────────────────────────────────────────────────────

        [SessionAuthorize("Collection Agent")]
        [HttpGet]
        public IActionResult BatchStatus()
        {
            int staffId =
                HttpContext.Session.GetInt32("StaffId") ?? 0;

            if (staffId == 0)
            {
                return RedirectToAction("Login", "Auth");
            }

            var model =
                _collectionCenterRepo.GetTodayBatchStatus(staffId);

            return View(model);
        }


        [SessionAuthorize("Collection Agent")]
        [HttpGet]
        public IActionResult AllBatchDetails()
        {
            int staffId =
                HttpContext.Session.GetInt32("StaffId") ?? 0;

            if (staffId == 0)
                return RedirectToAction("Login", "Auth");

            var model =
                _collectionCenterRepo.GetAllBatchDetails(staffId);

            return View(model);
        }

        // ─────────────────────────────────────────────────────────────
        // VIEW COLLECTION BATCH — entries for a specific shift today
        // ─────────────────────────────────────────────────────────────



        [SessionAuthorize("Collection Agent")]
        [HttpGet]
        public IActionResult RejectedFarmersByCenter()
        {
            var staffId = HttpContext.Session.GetInt32("StaffId");

            if (staffId == null || staffId == 0)
                return RedirectToAction("Login", "Auth");

            var list = _farmerRepo.GetRejectedFarmersByCenter(staffId.Value);

            if (!list.Any())
                TempData["Info"] = "No rejected farmers found.";

            return View(list);
        }

        [SessionAuthorize("Collection Agent")]
        [HttpGet]
        public IActionResult AddMilkCollection()
        {
            var staffId = HttpContext.Session.GetInt32("StaffId");

            if (staffId == null || staffId == 0)
                return RedirectToAction("Login", "Auth");

            int centerId = _collectionCenterRepo.GetCenterIdByStaffId(staffId.Value);

            var model = new MilkCollectionModel
            {
                CollectionDate = DateTime.Today,
                Shift = _collectionCenterRepo.GetCurrentShift(),
                CenterId = centerId,
                CenterName = _collectionCenterRepo.GetStaffCenter(staffId.Value)?.CenterName
            };

            ViewBag.Farmers = _collectionCenterRepo.GetFarmers(centerId);
            ViewBag.MilkTypes = _collectionCenterRepo.GetMilkTypes(); 
            ViewBag.RateCharts = _collectionCenterRepo.GetRateCharts();

            return View(model);
        }

        [SessionAuthorize("Collection Agent")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddMilkCollection(MilkCollectionModel model,string submitAction,string RejectionReason,string Remarks)
        {
            var staffId = HttpContext.Session.GetInt32("StaffId");

            if (staffId == null || staffId == 0)
                return RedirectToAction("Login", "Auth");

            int centerId = _collectionCenterRepo.GetCenterIdByStaffId(staffId.Value);

            void ReloadViewBag()
            {
                ViewBag.Farmers = _collectionCenterRepo.GetFarmers(centerId);
                ViewBag.MilkTypes = _collectionCenterRepo.GetMilkTypes();
                ViewBag.RateCharts = _collectionCenterRepo.GetRateCharts();
            }

            void ReloadModel()
            {
                model.Shift = _collectionCenterRepo.GetCurrentShift();
                model.CenterName = _collectionCenterRepo.GetStaffCenter(staffId.Value)?.CenterName;
            }

            var shift = _collectionCenterRepo.GetCurrentShift();
            if (shift == "No Active Shift")
            {
                TempData["Error"] = "No active shift right now.";
                ReloadViewBag();
                ReloadModel();
                return View("AddMilkCollection", model); 
            }

            
            if (model.FarmerId == 0)
            {
                TempData["Error"] = "Please select a farmer.";
                ReloadViewBag();
                ReloadModel();
                return View("AddMilkCollection", model); 
            }

            // ─────────────────────────────────────
            // ACCEPT
            // ─────────────────────────────────────
            if (submitAction == "Accept")
            {
                ModelState.Remove("RejectionReason");
                ModelState.Remove("Remarks");
                ModelState.Remove("Shift");
                ModelState.Remove("CollectionDate");

                if (model.MilkTypeId == 0)
                {
                    TempData["Error"] = "Please select milk type.";
                    ReloadViewBag();
                    ReloadModel();
                    return View("AddMilkCollection", model); 
                }

                if (model.Quantity <= 0)
                {
                    TempData["Error"] = "Please enter valid quantity.";
                    ReloadViewBag();
                    ReloadModel();
                    return View("AddMilkCollection", model);
                }

                try
                {
                    _collectionCenterRepo.AddMilkCollection(
                        staffId: staffId.Value,
                        farmerId: model.FarmerId,
                        milkTypeId: model.MilkTypeId,
                        quantity: model.Quantity,
                        appliedFat: model.AppliedFat ?? 0,
                        appliedCLR: model.AppliedCLR ?? 0
                    );

                    TempData["Success"] = "Milk entry saved successfully!";
                    return RedirectToAction("AddMilkCollection"); 
                }
                catch (Exception ex)
                {
                    TempData["Error"] = ex.Message;
                    ReloadViewBag();
                    ReloadModel();
                    return View("AddMilkCollection", model); 
                }
            }

            // ─────────────────────────────────────
            // REJECT
            // ─────────────────────────────────────
            if (submitAction == "Reject")
            {
                if (model.FarmerId == 0)
                {
                    TempData["Error"] = "Please select a farmer.";
                    ReloadViewBag();
                    ReloadModel();
                    return View("AddMilkCollection", model); 
                }

                if (string.IsNullOrEmpty(RejectionReason))
                {
                    TempData["Error"] = "Please select a rejection reason.";
                    ReloadViewBag();
                    ReloadModel();
                    return View("AddMilkCollection", model); 
                }

                try
                {
                    var rejectionModel = new MilkRejectionModel
                    {
                        FarmerId = model.FarmerId,
                        MilkTypeId = model.MilkTypeId,
                        AppliedFat = model.AppliedFat,
                        AppliedCLR = model.AppliedCLR,
                        Quantity = model.Quantity,
                        RejectionReason = RejectionReason,
                        Remarks = Remarks
                    };

                    _collectionCenterRepo.RejectMilkEntry(rejectionModel, staffId.Value);

                    TempData["RejectionSuccess"] = "Milk rejected successfully!";
                    return RedirectToAction("AddMilkCollection"); 
                }
                catch (Exception ex)
                {
                    TempData["Error"] = ex.Message;
                    ReloadViewBag();
                    ReloadModel();
                    return View("AddMilkCollection", model); 
                }
            }

            return RedirectToAction("Create");
        }

        [SessionAuthorize("Collection Agent")]
        [HttpGet]
        public IActionResult RateChart()
        {
            ViewBag.RateCharts = _collectionCenterRepo.GetRateCharts();
            return View();
        }

        // ─────────────────────────────────────────────
        // REJECT MILK — GET
        // ─────────────────────────────────────────────

        [SessionAuthorize("Collection Agent")]
        [HttpGet]
        public IActionResult RejectMilk()
        {
            var staffId = HttpContext.Session.GetInt32("StaffId");

            if (staffId == null || staffId == 0)
                return RedirectToAction("Login", "Auth");

            int centerId = _collectionCenterRepo.GetCenterIdByStaffId(staffId.Value);

            var model = new MilkRejectionModel
            {
                Farmers = _collectionCenterRepo.GetFarmers(centerId),
                MilkTypes = _collectionCenterRepo.GetMilkTypes()
            };

            return View(model);
        }


        // ─────────────────────────────────────────────
        // REJECT MILK — POST
        // ─────────────────────────────────────────────

        [SessionAuthorize("Collection Agent")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RejectMilk(MilkRejectionModel model)
        {
            var staffId = HttpContext.Session.GetInt32("StaffId");

            if (staffId == null || staffId == 0)
                return RedirectToAction("Login", "Auth");

            try
            {
                var rejectionId = _collectionCenterRepo.RejectMilkEntry(model, staffId.Value);

                TempData["RejectionSuccess"] = $"Milk rejected successfully! Rejection ID: {rejectionId}";
                return RedirectToAction("Create");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

                return RedirectToAction("Create");
            }
        }



        [SessionAuthorize("Collection Agent")]
        public IActionResult AllMilkEntriesCenter()
        {
            int centerId = HttpContext.Session.GetInt32("CenterId") ?? 0;

            var result = _collectionCenterRepo.GetAllEntries(centerId);

            return View(result);
        }

        //inventory
        [SessionAuthorize("Collection Agent")]
        [HttpGet]
        public IActionResult Inventory()
        {
            var staffId = HttpContext.Session.GetInt32("StaffId");

            if (staffId == null || staffId == 0)
                return RedirectToAction("Login", "Auth");

            int centerId = _collectionCenterRepo.GetCenterIdByStaffId(staffId.Value);

            if (centerId == 0)
            {
                TempData["Error"] = "You are not assigned to any center.";
                return RedirectToAction("Dashboard");
            }

            var inventory = _collectionCenterRepo.GetInventoryByCenter(centerId);

            return View(inventory);
        }

        //milk receipt to farmer
        [SessionAuthorize("Collection Agent")]
        [HttpGet]
        public IActionResult MilkReceipt(int id)
        {
            var staffId = HttpContext.Session.GetInt32("StaffId");
            if (staffId == null)
                return RedirectToAction("Login", "Auth");

            var receipt = _collectionCenterRepo.GetReceiptByCollectionId(id);

            if (receipt == null)
            {
                TempData["Error"] = "Receipt not found.";
                return RedirectToAction("AllMilkEntries");
            }

            return View(receipt);
        }

        [SessionAuthorize("Collection Agent")]
        public IActionResult DownloadReceipt(int id)
        {
            var r = _collectionCenterRepo.GetReceiptByCollectionId(id);
            if (r == null) return NotFound();

            // Read HTML file
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/templates/Receipt.html");
            var html = System.IO.File.ReadAllText(path);

            // Replace placeholders
            html = html.Replace("{{CenterName}}", r.CenterName ?? "MILK COLLECTION CENTER")
                       .Replace("{{ReceiptNumber}}", r.ReceiptNumber ?? "")
                       .Replace("{{Date}}", r.CollectionDate.ToString("dd-MM-yyyy"))
                       .Replace("{{Shift}}", r.Shift ?? "")
                       .Replace("{{FarmerName}}", r.FarmerName ?? "")
                       .Replace("{{FarmerCode}}", r.FarmerCode ?? "")
                       .Replace("{{MilkType}}", r.MilkTypeName ?? "")
                       .Replace("{{Quantity}}", $"{r.Quantity:F2} L")
                       .Replace("{{Fat}}", r.AppliedFat?.ToString("F2") ?? "-")
                       .Replace("{{CLR}}", r.AppliedCLR?.ToString("F2") ?? "-")
                       .Replace("{{Rate}}", $"₹{r.RatePerLiter?.ToString("F2") ?? "-"}")
                       .Replace("{{Amount}}", $"₹{r.Amount?.ToString("F2") ?? "0.00"}");

            var doc = new HtmlToPdfDocument()
            {
                GlobalSettings = new GlobalSettings
                {
                    ColorMode = ColorMode.Color,
                    Orientation = Orientation.Portrait,
                    // 80mm is approx 3.15 inches. Set height long (e.g., 200mm/7.87in) to fit data.
                    PaperSize = new PechkinPaperSize("80mm", "200mm"),
                    Margins = new MarginSettings { Top = 2, Bottom = 2, Left = 2, Right = 2 }
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

            return File(pdf, "application/pdf", $"Receipt_{r.ReceiptNumber}.pdf");
        }

        [HttpGet]
        public IActionResult ToggleStatus(int id, bool isActive)
        {
            int staffId = HttpContext.Session.GetInt32("StaffId") ?? 0;

            if (staffId == 0)
                return RedirectToAction("Login", "Admin");

            try
            {
                _farmerRepo.ToggleFarmerStatus(staffId, id, isActive);
                TempData["Success"] = isActive? "Farmer activated successfully.": "Farmer deactivated successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("ListAllFarmers");
        }

        // ─────────────────────────────────────────────────────────────
        // PENDING APPROVALS — shows all pending farmers for staff's center
        // ─────────────────────────────────────────────────────────────
        [SessionAuthorize("Collection Agent")]
        [HttpGet]
        public IActionResult PendingApprovals()
        {
            var staffId = HttpContext.Session.GetInt32("StaffId");

            if (staffId == null || staffId == 0)
                return RedirectToAction("Login", "Auth");

            try
            {
                var list = _farmerRepo.GetPendingApprovals(staffId.Value);
                return View(list);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return View(new List<PendingApprovalModel>());
            }
        }
       

        //Approve Farmer
        [SessionAuthorize("Collection Agent")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApproveFarmer(int farmerId)
        {
            var staffId = HttpContext.Session.GetInt32("StaffId");
            if (staffId == null || staffId == 0)
                return RedirectToAction("Login", "Auth");

            try
            {
                var result = _farmerRepo.ApproveFarmer(staffId.Value, farmerId);

                // Send approval email if farmer provided an email
                string emailNote;
                if (!string.IsNullOrWhiteSpace(result.Email))
                {
                    try
                    {
                        var loginUrl = $"{Request.Scheme}://{Request.Host}/Farmer/Login";
                        _emailService.SendApprovalEmailAsync(
                            result.Email,
                            result.FarmerCode,
                            result.DefaultPassword,
                            loginUrl);

                        emailNote = $"Credentials emailed to {result.Email}.";
                    }
                    catch (Exception mailEx)
                    {
                        // Email failure must NOT block the approval
                        emailNote = $"Email could not be sent ({mailEx.Message}). Share credentials manually.";
                    }
                }
                else
                {
                    emailNote = "No email on file — share credentials manually.";
                }
                // ─────────────────────────────────────────────────────────────

                TempData["Success"] = $"Farmer approved! " +
                                      $"Code: {result.FarmerCode} | " +
                                      $"Default Password: {result.DefaultPassword} (last 4 of phone). " +
                                      emailNote;
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("PendingApprovals");
        }


        // REJECT FARMER — GET
        // Staff clicks Reject on PendingApprovals - comes here to
        [SessionAuthorize("Collection Agent")]
        [HttpGet]
        public IActionResult RejectFarmer(int farmerId)
        {
            var staffId = HttpContext.Session.GetInt32("StaffId");

            if (staffId == null || staffId == 0)
                return RedirectToAction("Login", "Auth");

            // Get farmer name + phone to show on the confirmation page
            var pending = _farmerRepo.GetPendingApprovals(staffId.Value)
                                     .FirstOrDefault(f => f.FarmerId == farmerId);

            if (pending == null)
            {
                TempData["Error"] = "Farmer not found in your pending list.";
                return RedirectToAction("PendingApprovals");
            }

            var model = new RejectFarmerViewModel
            {
                FarmerId = pending.FarmerId,
                FarmerName = pending.FarmerName,
                Phone = pending.Phone
            };

            return View(model);
        }

        // ─────────────────────────────────────────────────────────────
        // REJECT FARMER — POST
        // Staff submits the rejection form with remark.
        // ─────────────────────────────────────────────────────────────

        [SessionAuthorize("Collection Agent")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RejectFarmer(RejectFarmerViewModel model)
        {
            var staffId = HttpContext.Session.GetInt32("StaffId");

            if (staffId == null || staffId == 0)
                return RedirectToAction("Login", "Auth");

            try
            {
                _farmerRepo.RejectFarmer(staffId.Value, model.FarmerId, model.ApprovalRemark);
                TempData["Success"] = "Farmer registration rejected.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("PendingApprovals");
        }
        // ─────────────────────────────────────────────────────────────
        // DISPATCH — GET
        // ─────────────────────────────────────────────────────────────

        [SessionAuthorize("Collection Agent")]
        [HttpGet]
        public IActionResult Dispatch()
        {
            var staffId = HttpContext.Session.GetInt32("StaffId");
            if (staffId == null || staffId == 0)
                return RedirectToAction("Login", "Auth");

            int centerId = _collectionCenterRepo.GetCenterIdByStaffId(staffId.Value);
            if (centerId == 0)
            {
                TempData["Error"] = "You are not assigned to any center.";
                return RedirectToAction("Dashboard");
            }

            var model = new DispatchMilkViewModel
            {
                DispatchDate = DateTime.Today,
                ClosedBatches = _collectionCenterRepo.GetClosedBatchesForDispatch(centerId),
                MilkTypes = _collectionCenterRepo.GetMilkTypes(),
                Vehicles = _collectionCenterRepo.GetActiveVehicles(),
                Plants = _collectionCenterRepo.GetAllPlants()
            };

            if (model.ClosedBatches.Count == 0)
                TempData["Info"] = "No batches available for dispatch right now.";

            return View(model);
        }

   
        [SessionAuthorize("Collection Agent")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Dispatch(DispatchMilkViewModel model)
        {
            var staffId = HttpContext.Session.GetInt32("StaffId");
            if (staffId == null || staffId == 0)
                return RedirectToAction("Login", "Auth");

            int centerId = _collectionCenterRepo.GetCenterIdByStaffId(staffId.Value);

            // Helper to reload dropdowns
            void ReloadDropdowns()
            {
                model.ClosedBatches = _collectionCenterRepo.GetClosedBatchesForDispatch(centerId);
                model.Vehicles = _collectionCenterRepo.GetActiveVehicles();
                model.Plants = _collectionCenterRepo.GetAllPlants();
                model.MilkTypes = _collectionCenterRepo.GetMilkTypes();
            }

            if (!ModelState.IsValid)
            {
                ReloadDropdowns();
                return View(model);
            }

            try
            {
                var transferId = _collectionCenterRepo.DispatchMilkTransfer(
                    batchId: model.BatchId,
                    milkTypeId: model.MilkTypeId,  
                    vehicleId: model.VehicleId,
                    plantId: model.PlantId,
                    dispatchQty: model.DispatchQty,
                    dispatchDate: model.DispatchDate
                );

                TempData["Success"] =
                    $"Dispatched {model.DispatchQty:F2} L successfully! " +
                    $"Transfer ID: {transferId}. " +
                    $"Center inventory updated.";

                return RedirectToAction("DispatchHistory");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                ReloadDropdowns();
                return View(model);
            }
        }
        
        [HttpGet]
        public JsonResult GetMilkTypeDetails(int batchId,int milkTypeId)
        {
            var result =
                _collectionCenterRepo.GetMilkTypeBatchDetails(
                    batchId,
                    milkTypeId);

            return Json(new
            {
                totalQty = result.totalQty,
                availableQty = result.availableQty
            });
        }

        // stays exactly same

        [SessionAuthorize("Collection Agent")]
        [HttpGet]
        public IActionResult DispatchHistory()
        {
            var staffId = HttpContext.Session.GetInt32("StaffId");
            if (staffId == null || staffId == 0)
                return RedirectToAction("Login", "Auth");

            int centerId = _collectionCenterRepo.GetCenterIdByStaffId(staffId.Value);
            if (centerId == 0)
            {
                TempData["Error"] = "You are not assigned to any center.";
                return RedirectToAction("Dashboard");
            }

            var history = _collectionCenterRepo.GetDispatchHistory(centerId);

            if (history.Count == 0)
                TempData["Info"] = "No dispatches recorded yet.";

            return View(history);
        }

    }
}

