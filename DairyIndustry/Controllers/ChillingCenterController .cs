using DairyIndustry.Models;
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
        //  DASHBOARD — /ChillingCenter/Dashboard
        //  Shows: summary cards + plant capacity table + recent entries
        // ═══════════════════════════════════════════════════════════
        public IActionResult Dashboard()
        {
            var viewModel = new ChillingDashboardViewModel
            {
                Summary = _repo.GetDashboardSummary(),
                PlantCapacity = _repo.GetPlantCapacitySummary(),
                RecentEntries = _repo.GetAll(null, null).Take(5).ToList(),
                ActiveAlerts = _repo.GetTemperatureAlerts(DateTime.Today, DateTime.Today)
            };
            return View(viewModel);
        }


        // ═══════════════════════════════════════════════════════════
        //  INDEX — /ChillingCenter/Index
        //  Shows: all storage entries with optional date filter
        // ═══════════════════════════════════════════════════════════
        public IActionResult Index(DateTime? fromDate, DateTime? toDate, int? plantId)
        {
            // If plantId filter is selected use GetByPlant, otherwise GetAll
            List<ChillingStorageModel> entries;

            if (plantId.HasValue)
                entries = _repo.GetByPlant(plantId.Value, fromDate, toDate);
            else
                entries = _repo.GetAll(fromDate, toDate);

            // Pass plants to view for the filter dropdown
            ViewBag.Plants = new SelectList(_repo.GetPlants(), "PlantId", "DisplayText", plantId);
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.PlantId = plantId;

            return View(entries);
        }


        // ═══════════════════════════════════════════════════════════
        //  DETAILS — /ChillingCenter/Details/5
        //  Shows: single entry full detail
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
        //  Shows: empty Add form with dropdowns populated
        // ═══════════════════════════════════════════════════════════
        public IActionResult Create()
        {
            LoadDropdowns();
            return View(new ChillingStoreItemModel());
        }

        // CREATE POST — receives submitted form data
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ChillingStoreItemModel model)
        {
            if (!ModelState.IsValid)
            {
                LoadDropdowns();
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
        //  Shows: pre-filled Edit form
        // ═══════════════════════════════════════════════════════════
        public IActionResult Edit(int id)
        {
            var entry = _repo.GetById(id);
            if (entry == null)
            {
                TempData["Error"] = "Storage entry not found.";
                return RedirectToAction("Index");
            }

            // Map ChillingStorageModel → ChillingStoreItemModel for the form
            var model = new ChillingStoreItemModel
            {
                StorageId = entry.StorageId,
                PlantId = entry.PlantId,
                ProductId = entry.ProductId,
                MilkQuantity = entry.MilkQuantity,
                Temperature = entry.Temperature,
                StoredDate = entry.StoredDate
            };

            LoadDropdowns(model.PlantId, model.ProductId);
            return View(model);
        }

        // EDIT POST — saves edited form data
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ChillingStoreItemModel model)
        {
            if (!ModelState.IsValid)
            {
                LoadDropdowns(model.PlantId, model.ProductId);
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
        //  Shows: delete confirmation page
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

        // DELETE POST — confirmed delete
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
        //  Shows: all entries where temperature > 5°C
        // ═══════════════════════════════════════════════════════════
        public IActionResult Alerts(DateTime? fromDate, DateTime? toDate)
        {
            var alerts = _repo.GetTemperatureAlerts(fromDate, toDate);

            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            return View(alerts);
        }


        // ═══════════════════════════════════════════════════════════
        //  CAPACITY — /ChillingCenter/Capacity
        //  Shows: per-plant capacity monitoring table
        // ═══════════════════════════════════════════════════════════
        public IActionResult Capacity()
        {
            var data = _repo.GetPlantCapacitySummary();
            return View(data);
        }


        // ═══════════════════════════════════════════════════════════
        //  PRIVATE HELPER — loads Plant and Product dropdowns
        //  Called before returning any Create or Edit view
        // ═══════════════════════════════════════════════════════════
        private void LoadDropdowns(int? selectedPlantId = null, int? selectedProductId = null)
        {
            ViewBag.Plants = new SelectList(
                _repo.GetPlants(), "PlantId", "DisplayText", selectedPlantId);

            // Add a "Raw Milk" option at the top of the product dropdown
            var products = _repo.GetProducts();
            ViewBag.Products = new SelectList(
                products, "ProductId", "DisplayText", selectedProductId);
        }
    }
}
