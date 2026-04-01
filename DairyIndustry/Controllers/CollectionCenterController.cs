using DairyIndustry.Filters;
using DairyIndustry.Models.Collection;
using DairyIndustry.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DairyIndustry.Controllers
{
    [ServiceFilter(typeof(ActionLogFilter))]
    public class CollectionCenterController : Controller
    {
        private readonly ICollectionCenterRepository _collectionCenterRepo;

        public CollectionCenterController(ICollectionCenterRepository repository)
        {
            _collectionCenterRepo = repository;
        }

        // ─────────────────────────────────────────────────────────────
        // DASHBOARD
        // ─────────────────────────────────────────────────────────────
        [HttpGet]
        [SessionAuthorize("Collection Agent")]
        public IActionResult Dashboard()
        {
            int? staffId = HttpContext.Session.GetInt32("StaffId");

            if (!staffId.HasValue)
                return RedirectToAction("Login", "Admin");

            var dashboard = _collectionCenterRepo.GetCollectionCenterByStaff(staffId.Value);

            if (dashboard == null)
            {
                ViewBag.Error = "No collection center assigned to you.";
                return View();
            }

            return View(dashboard);
        }

        // ─────────────────────────────────────────────────────────────
        // BATCH STATUS PAGE
        // Batches open/close automatically by time via usp_SyncBatches
        // Staff can only VIEW status here — no manual open/close
        // ─────────────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult BatchStatus()
        {
            int staffId = HttpContext.Session.GetInt32("StaffId") ?? 0;

            var center = _collectionCenterRepo.GetCollectionCenterByStaff(staffId);

            if (center == null || center.CenterId == 0)
            {
                TempData["Error"] = "No collection center assigned to you.";
                return View(new List<BatchStatusViewModel>());
            }

            // GetBatchStatus internally calls usp_GetBatchStatus
            // which calls usp_SyncBatches — so batches are auto-synced on every page load
            var batches = _collectionCenterRepo.GetBatchStatus(center.CenterId);

            ViewBag.CenterName = center.CenterName;

            return View(batches);
        }

        // ─────────────────────────────────────────────────────────────
        // MILK ENTRY — CREATE
        // CollectionDate = today only (enforced here & in SP)
        // Shift          = auto-detected in SP from current time
        // BatchId        = resolved in SP from open batch
        // ─────────────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult Create()
        {
            int staffId = HttpContext.Session.GetInt32("StaffId") ?? 0;

            var center = _collectionCenterRepo.GetCollectionCenterByStaff(staffId);

            if (center == null || center.CenterId == 0)
            {
                TempData["Error"] = "No collection center assigned to you.";
                return RedirectToAction("Dashboard");
            }

            var shift = GetCurrentShift();   //  ADD THIS LINE

            var model = new MilkCollectionViewModel
            {
                CenterName = center.CenterName,
                CollectionDate = DateTime.Today,
                CurrentShift = shift,   //  for UI
                Shift = shift,          //  for POST
                Farmers = LoadFarmers(),
                MilkTypes = LoadMilkTypes()
            };

            return View(model);
        }
        [HttpPost]
        public IActionResult Create(MilkCollectionViewModel model)
        {
            int staffId = HttpContext.Session.GetInt32("StaffId") ?? 0;

            var center = _collectionCenterRepo.GetCollectionCenterByStaff(staffId);

            if (center == null || center.CenterId == 0)
            {
                TempData["Error"] = "No collection center assigned to you.";
                return RedirectToAction("Dashboard");
            }

            // Always enforce today — never trust client date
            model.CollectionDate = DateTime.Today;

            try
            {
                var result = _collectionCenterRepo.RecordMilk(
                 farmerId: model.FarmerId,
                 centerId: center.CenterId,
                 milkTypeId: model.MilkTypeId,
                 shift: model.Shift,   // ✅ ADD THIS
                 quantity: model.Quantity,
                 fat: model.AppliedFat,
                 clr: model.AppliedCLR
             );
                TempData["Success"] = $"Entry saved! Collection ID: {result.collectionId} | " +
                                      $"Rate: ₹{result.rate}/L | Amount: ₹{result.amount}";

                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                // SP raises clear messages like "Morning shift is closed. It closed at 10:00 AM."
                TempData["Error"] = ex.Message;

                // Reload dropdowns before returning view
                model.CenterName = center.CenterName;
                model.CollectionDate = DateTime.Today;
                model.Shift = GetCurrentShift();
                model.Farmers = LoadFarmers();
                model.MilkTypes = LoadMilkTypes();

                return View(model);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // BATCH COLLECTIONS  —  view entries for a specific batch
        // ─────────────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult BatchCollections(int? batchId)
        {
            if (batchId == null || batchId == 0)
            {
                TempData["Error"] = "Invalid batch selected.";
                return RedirectToAction("BatchStatus");
            }

            ViewBag.BatchId = batchId.Value;

            var data = _collectionCenterRepo.GetBatchCollections(batchId.Value);

            if (data == null || data.Count == 0)
                TempData["Info"] = "No entries found for this batch yet.";

            return View(data);
        }

        // ─────────────────────────────────────────────────────────────
        // INVENTORY
        // ─────────────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult Inventory(int? centerId)
        {
            int staffId = HttpContext.Session.GetInt32("StaffId") ?? 0;

            var center = _collectionCenterRepo.GetCollectionCenterByStaff(staffId);

            int selectedCenterId = centerId ?? center.CenterId;

            var inventory = _collectionCenterRepo.GetCenterInventory(selectedCenterId);

            ViewBag.CenterList = new List<SelectListItem>
            {
                new SelectListItem { Value = center.CenterId.ToString(), Text = center.CenterName }
            };

            ViewBag.SelectedCenter = selectedCenterId;

            return View(inventory);
        }

        // ─────────────────────────────────────────────────────────────
        // PRIVATE HELPERS
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the shift name based on current server time.
        /// Used only for display on the Create page — actual enforcement is in the SP.
        /// </summary>
        private string GetCurrentShift()
        {
            var now = DateTime.Now.TimeOfDay;

            if (now >= TimeSpan.FromHours(6) && now < TimeSpan.FromHours(10))
                return "Morning";

            if (now >= TimeSpan.FromHours(15) && now < TimeSpan.FromHours(19))
                return "Evening";

            if (now >= TimeSpan.FromHours(21) || now < TimeSpan.FromHours(1))
                return "Night";

            return "No Active Shift";
        }

        private List<SelectListItem> LoadFarmers()
        {
            return _collectionCenterRepo.GetFarmers()
                .Select(f => new SelectListItem
                {
                    Value = f.FarmerId.ToString(),
                    Text = f.FarmerName
                }).ToList();
        }

        private List<SelectListItem> LoadMilkTypes()
        {
            return _collectionCenterRepo.GetMilkTypes()
                .Select(t => new SelectListItem
                {
                    Value = t.MilkTypeId.ToString(),
                    Text = t.MilkTypeName
                }).ToList();
        }
    }
}