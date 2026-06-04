using DairyIndustry.Models.ChillingStorage;
using DairyIndustry.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DairyIndustry.Controllers
{
    public class ChillingCenterController : Controller
    {
        private readonly IChillingCenterRepository _repo;

        public ChillingCenterController(IChillingCenterRepository repo)
        {
            _repo = repo;
        }


        // ═══════════════════════════════════════════════════════════
        //  PRIVATE HELPER — gets PlantId + PlantName for the
        //  logged-in user from Admin.UserPlants via session UserId.
        //  Returns null if user has no plant assigned.
        // ═══════════════════════════════════════════════════════════
        private PlantDropdownModel? GetSessionPlant()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return null;
            return _repo.GetPlantByUserId(userId.Value);
        }

        // ═══════════════════════════════════════════════════════════
        //  PRIVATE HELPER — loads form data for Create / Edit
        //  Sets ViewBag.PlantName and ViewBag.Products
        //  No plant dropdown — plant is fixed from session
        // ═══════════════════════════════════════════════════════════
        private void LoadFormData(int plantId, int? selectedProductId = null)
        {
            var plants = _repo.GetPlants();
            var plant = plants.FirstOrDefault(p => p.PlantId == plantId);
            ViewBag.PlantName = plant?.DisplayText ?? "Unknown Plant";

            ViewBag.Products = new SelectList(
                _repo.GetProducts(), "ProductId", "DisplayText", selectedProductId);
        }


        // ═══════════════════════════════════════════════════════════
        //  DASHBOARD — /ChillingCenter/Dashboard
        //  Shows summary for ALL plants (admin-level view)
        // ═══════════════════════════════════════════════════════════
        public IActionResult Dashboard()
        {
            var plant = GetSessionPlant();

            var viewModel = new ChillingDashboardViewModel
            {
                Summary = _repo.GetDashboardSummary(),
                PlantCapacity = _repo.GetPlantCapacitySummary(),
                RecentEntries = plant != null
                    ? _repo.GetByPlant(plant.PlantId, null, null).Take(5).ToList()
                    : _repo.GetAll(null, null).Take(5).ToList(),
                ActiveAlerts = _repo.GetTemperatureAlerts(DateTime.Today, DateTime.Today),
                WeeklyTrend = _repo.GetWeeklyTrend(plant?.PlantId)
            };

            ViewBag.PlantName = plant?.DisplayText ?? "All Plants";
            return View(viewModel);
        }


        // ═══════════════════════════════════════════════════════════
        //  INDEX — /ChillingCenter/Index
        //  Plant Manager sees ONLY their plant's entries.
        //  When search is provided, uses filtered inline query.
        //  When no search, uses original SP (existing behaviour).
        // ═══════════════════════════════════════════════════════════
        public IActionResult Index(DateTime? fromDate, DateTime? toDate, string? search)
        {
            var plant = GetSessionPlant();
            List<ChillingStorageModel> entries;

            if (plant != null)
            {
                // Always use inline filtered query — includes Shift column.
                // SP (GetByPlant) does not return Shift so is no longer used for Index.
                entries = _repo.GetByPlantFiltered(plant.PlantId, fromDate, toDate,
                              string.IsNullOrWhiteSpace(search) ? null : search);

                ViewBag.PlantName = plant.DisplayText;
            }
            else
            {
                // Admin
                entries = string.IsNullOrWhiteSpace(search)
                    ? _repo.GetAll(fromDate, toDate)
                    : _repo.GetAllFiltered(fromDate, toDate, search);

                ViewBag.PlantName = "All Plants";
                ViewBag.Plants = new SelectList(_repo.GetPlants(), "PlantId", "DisplayText");
            }

            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.Search = search;

            return View(entries);
        }


        // ═══════════════════════════════════════════════════════════
        //  DETAILS — /ChillingCenter/Details/5
        // ═══════════════════════════════════════════════════════════
        public IActionResult Details(int id)
        {
            var entry = _repo.GetById(id);
            if (entry == null)
            {
                TempData["Error"] = "Storage entry not found.";
                return RedirectToAction("Index");
            }
            return View(entry);
        }


        // ═══════════════════════════════════════════════════════════
        //  CREATE GET — /ChillingCenter/Create
        //  PlantId comes from session — shown as label
        // ═══════════════════════════════════════════════════════════
        public IActionResult Create()
        {
            var plant = GetSessionPlant();

            if (plant == null)
            {
                TempData["Error"] = "Your account is not assigned to any plant. Contact Admin.";
                return RedirectToAction("Index");
            }

            LoadFormData(plant.PlantId);

            var model = new ChillingStoreItemModel
            {
                PlantId = plant.PlantId,
                StoredDate = DateTime.Today
            };

            return View(model);
        }

        // CREATE POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ChillingStoreItemModel model)
        {
            if (!ModelState.IsValid)
            {
                LoadFormData(model.PlantId, model.ProductId);
                return View(model);
            }

            // Use inline InsertWithShift if shift selected — SP untouched
            int newId = !string.IsNullOrEmpty(model.Shift)
                ? _repo.InsertWithShift(model)
                : _repo.StoreItem(model);

            if (newId > 0)
                TempData["Success"] = "Storage entry added successfully.";
            else
                TempData["Error"] = "Something went wrong. Please try again.";

            return RedirectToAction("Index");
        }


        // ═══════════════════════════════════════════════════════════
        //  NEW — DUPLICATE — /ChillingCenter/Duplicate/5
        //  Pre-fills the Create form with data from an existing
        //  entry. StorageId is set to 0 so it saves as a new record.
        //  StoredDate resets to today. Everything else copied.
        // ═══════════════════════════════════════════════════════════
        public IActionResult Duplicate(int id)
        {
            var source = _repo.GetById(id);

            if (source == null)
            {
                TempData["Error"] = "Entry not found. Cannot duplicate.";
                return RedirectToAction("Index");
            }

            LoadFormData(source.PlantId, source.ProductId);

            var model = new ChillingStoreItemModel
            {
                StorageId = 0,
                PlantId = source.PlantId,
                ProductId = source.ProductId,
                MilkQuantity = source.MilkQuantity,
                Temperature = source.Temperature,
                StoredDate = DateTime.Today,
                Shift = source.Shift
            };

            TempData["Info"] = $"Duplicated from Entry #{source.StorageId} — review and save.";
            return View("Create", model);
        }


        // ═══════════════════════════════════════════════════════════
        //  EDIT GET — /ChillingCenter/Edit/5
        //  PlantId comes from the existing record
        // ═══════════════════════════════════════════════════════════
        public IActionResult Edit(int id)
        {
            var entry = _repo.GetById(id);
            if (entry == null)
            {
                TempData["Error"] = "Storage entry not found.";
                return RedirectToAction("Index");
            }

            LoadFormData(entry.PlantId, entry.ProductId);

            var model = new ChillingStoreItemModel
            {
                StorageId = entry.StorageId,
                PlantId = entry.PlantId,
                ProductId = entry.ProductId,
                MilkQuantity = entry.MilkQuantity,
                Temperature = entry.Temperature,
                StoredDate = entry.StoredDate,
                Shift = entry.Shift
            };

            return View(model);
        }
        
        // EDIT POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ChillingStoreItemModel model)
        {
            if (!ModelState.IsValid)
            {
                LoadFormData(model.PlantId, model.ProductId);
                return View(model);
            }

            bool success = _repo.UpdateEntry(model);

            if (success)
                TempData["Success"] = "Storage entry updated successfully.";
            else
                TempData["Error"] = "Update failed. Please try again.";

            return RedirectToAction("Index");
        }


        // ═══════════════════════════════════════════════════════════
        //  DELETE GET — /ChillingCenter/Delete/5
        // ═══════════════════════════════════════════════════════════
        public IActionResult Delete(int id)
        {
            var entry = _repo.GetById(id);
            if (entry == null)
            {
                TempData["Error"] = "Storage entry not found.";
                return RedirectToAction("Index");
            }
            return View(entry);
        }

        // DELETE POST
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            bool success = _repo.DeleteEntry(id);

            if (success)
                TempData["Success"] = "Storage entry deleted successfully.";
            else
                TempData["Error"] = "Delete failed. Please try again.";

            return RedirectToAction("Index");
        }


        // ═══════════════════════════════════════════════════════════
        //  TEMPERATURE ALERTS — /ChillingCenter/Alerts
        //  Plant Manager sees only their plant's alerts
        // ═══════════════════════════════════════════════════════════
        public IActionResult Alerts(DateTime? fromDate, DateTime? toDate)
        {
            var allAlerts = _repo.GetTemperatureAlerts(fromDate, toDate);
            var plant = GetSessionPlant();

            var alerts = plant != null
                ? allAlerts.Where(a => a.PlantId == plant.PlantId).ToList()
                : allAlerts;

            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.PlantName = plant?.DisplayText ?? "All Plants";

            return View(alerts);
        }


        // ═══════════════════════════════════════════════════════════
        //  CAPACITY — /ChillingCenter/Capacity
        // ═══════════════════════════════════════════════════════════
        public IActionResult Capacity()
        {
            var data = _repo.GetPlantCapacitySummary();
            var plant = GetSessionPlant();

            if (plant != null)
                data = data.Where(d => d.PlantId == plant.PlantId).ToList();

            ViewBag.PlantName = plant?.DisplayText ?? "All Plants";
            return View(data);
        }


        // ═══════════════════════════════════════════════════════════
        //  QUICK EDIT — /ChillingCenter/QuickEdit
        //  Called via fetch() from Index page — no page reload.
        //  Only updates MilkQuantity and Temperature.
        //  Returns JSON { success, message }.
        // ═══════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult QuickEdit(int storageId, decimal milkQuantity, decimal? temperature)
        {
            if (milkQuantity <= 0)
                return Json(new { success = false, message = "Quantity must be greater than 0." });

            bool ok = _repo.QuickUpdateEntry(storageId, milkQuantity, temperature);

            return Json(new
            {
                success = ok,
                message = ok ? "Entry updated successfully." : "Update failed. Please try again.",
                milkQuantity = milkQuantity.ToString("N2"),
                temperature = temperature.HasValue ? temperature.Value.ToString("N1") + "°C" : "—"
            });
        }


        // ═══════════════════════════════════════════════════════════
        //  DAILY REPORT — /ChillingCenter/Report
        //  Now serves both By Date and By Product tabs via
        //  ChillingReportViewModel.
        // ═══════════════════════════════════════════════════════════
        public IActionResult Report(DateTime? fromDate, DateTime? toDate)
        {
            var plant = GetSessionPlant();
            var from = fromDate ?? DateTime.Today.AddDays(-29);
            var to = toDate ?? DateTime.Today;

            var viewModel = new ChillingReportViewModel
            {
                DailyData = _repo.GetDailyReport(plant?.PlantId, from, to),
                ProductData = _repo.GetProductReport(plant?.PlantId, from, to),
                FromDate = from,
                ToDate = to,
                PlantName = plant?.DisplayText ?? "All Plants"
            };

            return View(viewModel);
        }
    }
}