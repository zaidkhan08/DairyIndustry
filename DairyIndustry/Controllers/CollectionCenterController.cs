using DairyIndustry.Filters;
using DairyIndustry.Models.Admin;
using DairyIndustry.Models.Collection;
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
        //private readonly IAdminRepository _adminRepository;

        public CollectionCenterController(ICollectionCenterRepository repository, IConverter converter, IAdminRepository adminRepository)
        {
            _collectionCenterRepo = repository;
            _converter = converter;
           // _adminRepository = adminRepository;
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

            //  GET CENTER ID
            int centerId = _collectionCenterRepo.GetCenterIdByStaffId(staffId.Value);

            //  LOAD DROPDOWNS
            ViewBag.Farmers = new SelectList(
                _collectionCenterRepo.GetFarmers(centerId),
                "FarmerId",
                "FarmerName"
            );

            ViewBag.MilkTypes = new SelectList(
                _collectionCenterRepo.GetMilkTypes(),
                "MilkTypeId",
                "MilkTypeName"
            );

            ViewBag.RateCharts = _collectionCenterRepo.GetRateCharts();

            var model = new MilkCollectionViewModel
            {
                CollectionDate = DateTime.Today,
                Shift = _collectionCenterRepo.GetCurrentShift()
            };

            return View(model);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(MilkCollectionViewModel model)
        {
            var staffId = HttpContext.Session.GetInt32("StaffId");

            if (staffId == null || staffId == 0)
                return RedirectToAction("Login", "Auth");

            int centerId = _collectionCenterRepo.GetCenterIdByStaffId(staffId.Value);

            // CHECK SHIFT BEFORE INSERT
            var shift = _collectionCenterRepo.GetCurrentShift();

            if (shift == "No Active Shift")
            {
                TempData["Error"] = "Milk entry allowed only during Morning (10:30–11 AM) or Evening (4–7 PM).";

                // reload dropdowns
                ViewBag.Farmers = new SelectList(
                    _collectionCenterRepo.GetFarmers(centerId),
                    "FarmerId",
                    "FarmerName"
                );

                ViewBag.MilkTypes = new SelectList(
                    _collectionCenterRepo.GetMilkTypes(),
                    "MilkTypeId",
                    "MilkTypeName"
                );

                model.Shift = shift;

                return View(model);
            }

            //  normal validation
            if (!ModelState.IsValid)
            {
                ViewBag.Farmers = new SelectList(
                    _collectionCenterRepo.GetFarmers(centerId),
                    "FarmerId",
                    "FarmerName"
                );

                ViewBag.MilkTypes = new SelectList(
                    _collectionCenterRepo.GetMilkTypes(),
                    "MilkTypeId",
                    "MilkTypeName"
                );

                return View(model);
            }

            try
            {
                var collectionId = _collectionCenterRepo.AddMilkCollection(
                    staffId: staffId.Value,
                    farmerId: model.FarmerId,
                    milkTypeId: model.MilkTypeId,
                    quantity: model.Quantity,
                    appliedFat: model.AppliedFat ?? 0,
                    appliedCLR: model.AppliedCLR ?? 0
                );

                TempData["Success"] = $"Milk entry saved! Collection ID: {collectionId}";
                return RedirectToAction("BatchStatus");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

                ViewBag.Farmers = new SelectList(
                    _collectionCenterRepo.GetFarmers(centerId),
                    "FarmerId",
                    "FarmerName"
                );

                ViewBag.MilkTypes = new SelectList(
                    _collectionCenterRepo.GetMilkTypes(),
                    "MilkTypeId",
                    "MilkTypeName"
                );
                ViewBag.RateCharts = _collectionCenterRepo.GetRateCharts();
                return View(model);
            }
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public IActionResult Create(MilkCollectionViewModel model)
        //{
        //    var staffId = HttpContext.Session.GetInt32("StaffId");

        //    if (staffId == null || staffId == 0)
        //        return RedirectToAction("Login", "Auth");

        //    if (!ModelState.IsValid)
        //    {
        //        int centerId = _collectionCenterRepo.GetCenterIdByStaffId(staffId.Value);

        //        ViewBag.Farmers = new SelectList(
        //            _collectionCenterRepo.GetFarmers(centerId),
        //            "FarmerId",
        //            "FarmerName"
        //        );

        //        ViewBag.MilkTypes = new SelectList(
        //            _collectionCenterRepo.GetMilkTypes(),
        //            "MilkTypeId",
        //            "MilkTypeName"
        //        );

        //        return View(model);
        //    }

        //    try
        //    {
        //        var collectionId = _collectionCenterRepo.AddMilkCollection(
        //            staffId: staffId.Value,
        //            farmerId: model.FarmerId,
        //            milkTypeId: model.MilkTypeId,
        //            quantity: model.Quantity,
        //            appliedFat: model.AppliedFat ?? 0,
        //            appliedCLR: model.AppliedCLR ?? 0
        //        );

        //        TempData["Success"] = $"Milk entry saved! Collection ID: {collectionId}";
        //        return RedirectToAction("BatchStatus");
        //    }
        //    catch (Exception ex)
        //    {
        //        TempData["Error"] = ex.Message;

        //        int centerId = _collectionCenterRepo.GetCenterIdByStaffId(staffId.Value);

        //        ViewBag.Farmers = new SelectList(
        //            _collectionCenterRepo.GetFarmers(centerId),
        //            "FarmerId",
        //            "FarmerName"
        //        );

        //        ViewBag.MilkTypes = new SelectList(
        //            _collectionCenterRepo.GetMilkTypes(),
        //            "MilkTypeId",
        //            "MilkTypeName"
        //        );

        //        return View(model);
        //    }
        //}

        // ─────────────────────────────────────────────────────────────
        // ENTRY DETAIL — view a single milk entry
        // ─────────────────────────────────────────────────────────────

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
        public IActionResult DateWiseEntries(DateTime? date)
        {
            int centerId = HttpContext.Session.GetInt32("CenterId") ?? 0;

            //  Default to today
            DateTime selectedDate = date ?? DateTime.Today;

            var model = new DateFilterViewModel
            {
                SelectedDate = selectedDate,
                Entries = _collectionCenterRepo.GetEntriesByDate(selectedDate, centerId)
            };

            return View(model);
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

    }
}

