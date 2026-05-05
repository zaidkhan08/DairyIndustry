using DairyIndustry.Filters;
using DairyIndustry.Models.Admin;
using DairyIndustry.Models.Collection;
using DairyIndustry.Models.FarmerModel;
using DairyIndustry.Repositories;
using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DairyIndustry.Controllers
{
    [ServiceFilter(typeof(ActionLogFilter))]
    public class CollectionCenterController : Controller
    {
        private readonly IConverter _converter;
        private readonly ICollectionCenterRepository _collectionCenterRepo;

        private readonly IFarmerRepository _farmerRepo;

     

        public CollectionCenterController(ICollectionCenterRepository repository, IConverter converter, IFarmerRepository farmerRepo)
        {
            _collectionCenterRepo = repository;
            _converter = converter;
            _farmerRepo = farmerRepo;
          
        }

        // ─────────────────────────────────────────────────────────────
        // DASHBOARD
        // Calls usp_Staff_Dashboard — returns staff info, both batch
        // statuses, and today's summary numbers in one SP call.
        // ─────────────────────────────────────────────────────────────
        public IActionResult Dashboard()
        {
            var staffId = HttpContext.Session.GetInt32("StaffId");

            if (staffId == null || staffId == 0)
                return RedirectToAction("Login", "Auth");

            var model = _collectionCenterRepo.GetStaffDashboard(staffId.Value);

            if (model == null)
            {
                TempData["Error"] = "Unable to load dashboard.";
                return RedirectToAction("Login", "Auth");
            }

            return View(model);
        }

        // ─────────────────────────────────────────────────────────────
        // BATCH STATUS PAGE
        // Shows Morning and Evening batch rows with status, qty, entries.
        // Batches open/close automatically via SQL Agent jobs — no
        // manual open/close buttons on this page.
        // Each row has a "View Entries" link → ViewCollectionBatch
        // ─────────────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult BatchStatus()
        {
            var staffId = HttpContext.Session.GetInt32("StaffId");

            if (staffId == null || staffId == 0)
                return RedirectToAction("Login", "Auth");

            var batches = _collectionCenterRepo.GetTodayBatchStatus(staffId.Value);

            return View(batches);
        }

        // ─────────────────────────────────────────────────────────────
        // VIEW COLLECTION BATCH — entries for a specific shift today
        // Reached from BatchStatus by clicking "View Entries" on a row.
        //
        // Route: /CollectionCenter/ViewCollectionBatch?shift=Morning
        //                                           (or Evening)
        //
        // Calls usp_GetTodayEntries with @StaffId + @Shift.
        // Only shows entries if BatchId is not null (batch exists).
        // ─────────────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult ViewCollectionBatch(string shift, int? batchId)
        {
            var staffId = HttpContext.Session.GetInt32("StaffId");

            if (staffId == null || staffId == 0)
                return RedirectToAction("Login", "Auth");

            // Validate shift — only Morning and Evening allowed
            if (shift != "Morning" && shift != "Evening")
            {
                TempData["Error"] = "Invalid shift selected.";
                return RedirectToAction("BatchStatus");
            }

            // If no batch exists yet for this shift, show empty state
            if (batchId == null)
            {
                TempData["Info"] = $"{shift} batch has not started yet.";
                ViewBag.Shift = shift;
                ViewBag.BatchId = null;
                return View(new List<MilkCollectionViewModel>());
            }

            var entries = _collectionCenterRepo.GetBatchEntries(staffId.Value, shift);

            ViewBag.Shift = shift;
            ViewBag.BatchId = batchId;

            if (entries == null || entries.Count == 0)
                TempData["Info"] = $"No entries recorded for the {shift} batch yet.";

            return View(entries);
        }
       

        [HttpGet]
        public IActionResult Create()
        {
            var staffId = HttpContext.Session.GetInt32("StaffId");

            if (staffId == null || staffId == 0)
                return RedirectToAction("Login", "Auth");

            int centerId = _collectionCenterRepo.GetCenterIdByStaffId(staffId.Value);

            var model = new MilkCollectionViewModel
            {
                CollectionDate = DateTime.Today,
                Shift = _collectionCenterRepo.GetCurrentShift(),
                CenterId = centerId,
                CenterName = _collectionCenterRepo
                                    .GetStaffDashboard(staffId.Value)
                                    ?.CenterName
            };

            ViewBag.Farmers = _collectionCenterRepo.GetFarmers(centerId);
            ViewBag.MilkTypes = _collectionCenterRepo.GetMilkTypes(); //  raw list
            ViewBag.RateCharts = _collectionCenterRepo.GetRateCharts();

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(
         MilkCollectionViewModel model,
         string submitAction,
         string RejectionReason,
         string Remarks)
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

            // RELOAD model display fields
            void ReloadModel()
            {
                model.Shift = _collectionCenterRepo.GetCurrentShift();
                model.CenterName = _collectionCenterRepo
                                    .GetStaffDashboard(staffId.Value)
                                    ?.CenterName;
            }

            // SHIFT CHECK
            var shift = _collectionCenterRepo.GetCurrentShift();
            if (shift == "No Active Shift")
            {
                TempData["Error"] = "No active shift right now.";
                ReloadViewBag();
                ReloadModel();
                return View("Create", model); //  keep data
            }

            // FARMER CHECK
            if (model.FarmerId == 0)
            {
                TempData["Error"] = "Please select a farmer.";
                ReloadViewBag();
                ReloadModel();
                return View("Create", model); //  keep data
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
                    return View("Create", model); //  keep data
                }

                if (model.Quantity <= 0)
                {
                    TempData["Error"] = "Please enter valid quantity.";
                    ReloadViewBag();
                    ReloadModel();
                    return View("Create", model); //  keep data
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
                    return RedirectToAction("Create"); // redirect on success
                }
                catch (Exception ex)
                {
                    TempData["Error"] = ex.Message;
                    ReloadViewBag();
                    ReloadModel();
                    return View("Create", model); //  keep data on error
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
                    return View("Create", model); // keep data
                }

                if (string.IsNullOrEmpty(RejectionReason))
                {
                    TempData["Error"] = "Please select a rejection reason.";
                    ReloadViewBag();
                    ReloadModel();
                    return View("Create", model); // keep data
                }

                try
                {
                    var rejectionModel = new MilkRejectionViewModel
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
                    return RedirectToAction("Create"); // redirect on success
                }
                catch (Exception ex)
                {
                    TempData["Error"] = ex.Message;
                    ReloadViewBag();
                    ReloadModel();
                    return View("Create", model); // keep data on error
                }
            }

            return RedirectToAction("Create");
        }
        [HttpGet]
        public IActionResult RateChart()
        {
            ViewBag.RateCharts = _collectionCenterRepo.GetRateCharts();
            return View();
        }

        // In CollectionCenterController.cs — add these 3 actions
        // ─────────────────────────────────────────────
        // REJECT MILK — GET
        // No changes needed here
        // ─────────────────────────────────────────────
        [HttpGet]
        public IActionResult RejectMilk()
        {
            var staffId = HttpContext.Session.GetInt32("StaffId");

            if (staffId == null || staffId == 0)
                return RedirectToAction("Login", "Auth");

            int centerId = _collectionCenterRepo.GetCenterIdByStaffId(staffId.Value);

            var model = new MilkRejectionViewModel
            {
                Farmers = _collectionCenterRepo.GetFarmers(centerId),
                MilkTypes = _collectionCenterRepo.GetMilkTypes()
            };

            return View(model);
        }


        // ─────────────────────────────────────────────
        // REJECT MILK — POST
        //
        // CHANGES:
        // 1. Removed [FromBody]     → was for AJAX/JSON
        // 2. Removed return Json()  → was for AJAX
        // 3. Removed return BadRequest() → was for AJAX
        // 4. Added [ValidateAntiForgeryToken] → form POST security
        // 5. Added model validation
        // 6. Added redirect to Create page after success
        // ─────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RejectMilk(MilkRejectionViewModel model)
        {
            var staffId = HttpContext.Session.GetInt32("StaffId");

            if (staffId == null || staffId == 0)
                return RedirectToAction("Login", "Auth");

            try
            {
                var rejectionId = _collectionCenterRepo.RejectMilkEntry(model, staffId.Value);

                TempData["RejectionSuccess"] = $"Milk rejected successfully! Rejection ID: {rejectionId}";

                // Go back to Create page
                // Staff is ready for next farmer immediately
                return RedirectToAction("Create");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

                // Go back to Create page with error message
                return RedirectToAction("Create");
            }
        }


        [HttpGet]
        public IActionResult RejectionHistoryCenter()
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

            var list = _collectionCenterRepo.GetRejectionsByCenter(centerId);

            if (list.Count == 0)
                TempData["Info"] = "No rejection entries found.";

            return View(list);
        }

        [HttpGet]
        public IActionResult EntryDetail(int id)
        {
            var staffId = HttpContext.Session.GetInt32("StaffId");

            if (staffId == null || staffId == 0)
                return RedirectToAction("Login", "Auth");

            var entry = _collectionCenterRepo.GetMilkEntryById(staffId.Value, id);

            if (entry == null)
            {
                TempData["Error"] = "Entry not found at your center.";
                return RedirectToAction("BatchStatus");
            }

            return View(entry);
        }

        public IActionResult DateWiseEntries(string search, string shift, DateTime? date)
        {
            int centerId = HttpContext.Session.GetInt32("CenterId") ?? 0;

            var data = _collectionCenterRepo.GetAllEntries(centerId);

            //  Search filter
            if (!string.IsNullOrEmpty(search))
            {
                data = data.Where(x =>
                    x.FarmerName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    x.FarmerCode.Contains(search, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            //  Shift filter
            if (!string.IsNullOrEmpty(shift))
            {
                data = data.Where(x => x.Shift == shift).ToList();
            }

            //  Date filter
            if (date.HasValue)
            {
                data = data.Where(x => x.CollectionDate.Date == date.Value.Date).ToList();
            }

            return View(data);
        }

        //inventory

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



        // ─────────────────────────────────────────────────────────────
        // PENDING APPROVALS — shows all pending farmers for staff's center
        // ─────────────────────────────────────────────────────────────
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
                return View(new List<PendingApprovalViewModel>());
            }
        }

        // ─────────────────────────────────────────────────────────────
        // APPROVE FARMER — POST only (button on PendingApprovals table)
        // On success, shows FarmerCode + Password in TempData banner
        // so staff can hand it to the farmer.
        // ─────────────────────────────────────────────────────────────
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

                TempData["Success"] = $"Farmer approved! " +
                                      $"Farmer Code: {result.FarmerCode} | " +
                                      $"Default Password: {result.DefaultPassword} " +
                                      $"(last 4 digits of phone). Please share with the farmer.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("PendingApprovals");
        }

        // ─────────────────────────────────────────────────────────────
        // REJECT FARMER — GET
        // Staff clicks Reject on PendingApprovals → comes here to
        // enter a rejection reason before confirming.
        // ─────────────────────────────────────────────────────────────
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
        // Shows form with batches that still have milk to dispatch.
        // Dropdown label shows  "Remaining: 500L of 800L"  so staff
        // can see what is already sent vs what is left.
        // ─────────────────────────────────────────────────────────────
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
                MilkTypes = _collectionCenterRepo.GetMilkTypes(), // ✅ NEW
                Vehicles = _collectionCenterRepo.GetActiveVehicles(),
                Plants = _collectionCenterRepo.GetAllPlants()
            };

            if (model.ClosedBatches.Count == 0)
                TempData["Info"] = "No batches available for dispatch right now.";

            return View(model);
        }
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
                model.MilkTypes = _collectionCenterRepo.GetMilkTypes(); // ✅ NEW
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
                    milkTypeId: model.MilkTypeId,  // ✅ NEW
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

        // stays exactly same
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

