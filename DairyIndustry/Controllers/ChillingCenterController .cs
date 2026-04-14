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
                // Show only this plant's recent entries if plant manager
                // Show all if no plant assigned (admin)
                RecentEntries = plant != null
                    ? _repo.GetByPlant(plant.PlantId, null, null).Take(5).ToList()
                    : _repo.GetAll(null, null).Take(5).ToList(),
                ActiveAlerts = _repo.GetTemperatureAlerts(DateTime.Today, DateTime.Today)
            };

            ViewBag.PlantName = plant?.DisplayText ?? "All Plants";
            return View(viewModel);
        }


        // ═══════════════════════════════════════════════════════════
        //  INDEX — /ChillingCenter/Index
        //  Plant Manager sees ONLY their plant's entries
        //  No plant filter shown — filtered automatically by session
        // ═══════════════════════════════════════════════════════════
        public IActionResult Index(DateTime? fromDate, DateTime? toDate)
        {
            var plant = GetSessionPlant();

            List<ChillingStorageModel> entries;

            if (plant != null)
            {
                // Plant Manager
                entries = _repo.GetByPlant(plant.PlantId, fromDate, toDate);
                ViewBag.PlantName = plant.DisplayText;
            }
            else
            {
                // Admin
                entries = _repo.GetAll(fromDate, toDate);
                ViewBag.PlantName = "All Plants";

                // ✅ FIX: populate dropdown
                ViewBag.Plants = new SelectList(
                    _repo.GetPlants(),
                    "PlantId",
                    "DisplayText"
                );
            }

            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

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

            int newId = _repo.StoreItem(model);

            if (newId > 0)
                TempData["Success"] = "Storage entry added successfully.";
            else
                TempData["Error"] = "Something went wrong. Please try again.";

            return RedirectToAction("Index");
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
                StoredDate = entry.StoredDate
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

            // Filter to plant manager's plant if assigned
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

            // Plant Manager sees only their plant's capacity row
            if (plant != null)
                data = data.Where(d => d.PlantId == plant.PlantId).ToList();

            ViewBag.PlantName = plant?.DisplayText ?? "All Plants";
            return View(data);
        }
    }
}