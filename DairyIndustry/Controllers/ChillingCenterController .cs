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
        //  PRIVATE HELPER — session auth check
        //  Fix #12 — every action calls this first.
        //  Returns true if logged in, false + redirects if not.
        // ═══════════════════════════════════════════════════════════
        private bool IsAuthenticated()
        {
            return HttpContext.Session.GetInt32("UserId") != null;
        }


        // ═══════════════════════════════════════════════════════════
        //  PRIVATE HELPER — gets PlantId + PlantName for the
        //  logged-in user. Result is cached in HttpContext.Items
        //  Fix #1 — was calling DB on every GetSessionPlant() call.
        //  Now hits DB once per request, cached for all subsequent calls.
        // ═══════════════════════════════════════════════════════════
        private PlantDropdownModel? GetSessionPlant()
        {
            const string key = "SessionPlant";

            if (HttpContext.Items.TryGetValue(key, out var cached))
                return cached as PlantDropdownModel;

            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                HttpContext.Items[key] = null;
                return null;
            }

            var plant = _repo.GetPlantByUserId(userId.Value);
            HttpContext.Items[key] = plant;
            return plant;
        }


        // ═══════════════════════════════════════════════════════════
        //  PRIVATE HELPER — loads form dropdowns for Create / Edit
        //  Fix #5 — no longer calls GetPlants() just to get a name.
        //  Uses the plant already retrieved from session instead.
        // ═══════════════════════════════════════════════════════════
        private void LoadFormData(PlantDropdownModel? plant, int? selectedProductId = null)
        {
            ViewBag.PlantName = plant?.DisplayText ?? "Unknown Plant";
            ViewBag.Products = new SelectList(
                _repo.GetProducts(), "ProductId", "DisplayText", selectedProductId);
        }


        // ═══════════════════════════════════════════════════════════
        //  PRIVATE HELPER — ownership check
        //  Fix #6 #7 — verifies that a storage entry belongs to
        //  the session plant before allowing Edit/Delete/Details/etc.
        //  Admin (no plant assigned) can access any entry.
        // ═══════════════════════════════════════════════════════════
        private bool OwnsEntry(ChillingStorageModel entry, PlantDropdownModel? plant)
        {
            // Admin has no plant assigned — can access everything
            if (plant == null) return true;
            return entry.PlantId == plant.PlantId;
        }


        // ═══════════════════════════════════════════════════════════
        //  DASHBOARD
        // ═══════════════════════════════════════════════════════════
        public IActionResult Dashboard()
        {
            if (!IsAuthenticated())
                return RedirectToAction("Login", "Admin");

            var plant = GetSessionPlant();

            var viewModel = new ChillingDashboardViewModel
            {
                // Fix #3 — Summary now scoped to session plant
                Summary = _repo.GetDashboardSummary(plant?.PlantId),
                PlantCapacity = _repo.GetPlantCapacitySummary(),
                // Fix #9  — use GetByPlantFiltered (inline query) instead of SP
                //           so Shift column is included in RecentEntries
                // Fix #13 — take 10 instead of 5 so chart has more data points
                RecentEntries = plant != null
                    ? _repo.GetByPlantFiltered(plant.PlantId, null, null, null).Take(10).ToList()
                    : _repo.GetAll(null, null).Take(10).ToList(),
                // Fix #2 — plantId passed directly to repo, no C# filtering
                ActiveAlerts = _repo.GetTemperatureAlerts(plant?.PlantId, DateTime.Today, DateTime.Today),
                WeeklyTrend = _repo.GetWeeklyTrend(plant?.PlantId)
            };

            ViewBag.PlantName = plant?.DisplayText ?? "All Plants";
            return View(viewModel);
        }


        // ═══════════════════════════════════════════════════════════
        //  INDEX
        // ═══════════════════════════════════════════════════════════
        public IActionResult Index(DateTime? fromDate, DateTime? toDate, string? search)
        {
            if (!IsAuthenticated())
                return RedirectToAction("Login", "Admin");

            var plant = GetSessionPlant();
            List<ChillingStorageModel> entries;

            if (plant != null)
            {
                entries = _repo.GetByPlantFiltered(plant.PlantId, fromDate, toDate,
                              string.IsNullOrWhiteSpace(search) ? null : search);
                ViewBag.PlantName = plant.DisplayText;
            }
            else
            {
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
        //  DETAILS
        //  Fix #7 — ownership check added
        // ═══════════════════════════════════════════════════════════
        public IActionResult Details(int id)
        {
            if (!IsAuthenticated())
                return RedirectToAction("Login", "Admin");

            var entry = _repo.GetById(id);
            if (entry == null)
            {
                TempData["Error"] = "Storage entry not found.";
                return RedirectToAction("Index");
            }

            var plant = GetSessionPlant();
            if (!OwnsEntry(entry, plant))
            {
                TempData["Error"] = "You do not have permission to view this entry.";
                return RedirectToAction("Index");
            }

            return View(entry);
        }


        // ═══════════════════════════════════════════════════════════
        //  CREATE GET
        //  Fix #5 — LoadFormData now receives plant directly
        // ═══════════════════════════════════════════════════════════
        public IActionResult Create()
        {
            if (!IsAuthenticated())
                return RedirectToAction("Login", "Admin");

            var plant = GetSessionPlant();

            if (plant == null)
            {
                TempData["Error"] = "Your account is not assigned to any plant. Contact Admin.";
                return RedirectToAction("Index");
            }

            LoadFormData(plant);

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
            if (!IsAuthenticated())
                return RedirectToAction("Login", "Admin");

            if (!ModelState.IsValid)
            {
                var plant = GetSessionPlant();
                LoadFormData(plant, model.ProductId);
                return View(model);
            }

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
        //  DUPLICATE
        //  Fix #7 — ownership check added
        // ═══════════════════════════════════════════════════════════
        public IActionResult Duplicate(int id)
        {
            if (!IsAuthenticated())
                return RedirectToAction("Login", "Admin");

            var source = _repo.GetById(id);
            if (source == null)
            {
                TempData["Error"] = "Entry not found. Cannot duplicate.";
                return RedirectToAction("Index");
            }

            var plant = GetSessionPlant();
            if (!OwnsEntry(source, plant))
            {
                TempData["Error"] = "You do not have permission to duplicate this entry.";
                return RedirectToAction("Index");
            }

            LoadFormData(plant, source.ProductId);

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
        //  EDIT GET
        //  Fix #5 — LoadFormData receives plant directly
        //  Fix #7 — ownership check added
        // ═══════════════════════════════════════════════════════════
        public IActionResult Edit(int id)
        {
            if (!IsAuthenticated())
                return RedirectToAction("Login", "Admin");

            var entry = _repo.GetById(id);
            if (entry == null)
            {
                TempData["Error"] = "Storage entry not found.";
                return RedirectToAction("Index");
            }

            var plant = GetSessionPlant();
            if (!OwnsEntry(entry, plant))
            {
                TempData["Error"] = "You do not have permission to edit this entry.";
                return RedirectToAction("Index");
            }

            // For Edit, use the entry's plant (may differ from session plant for admin)
            var entryPlant = new PlantDropdownModel
            {
                PlantId = entry.PlantId,
                PlantName = entry.PlantName
            };
            LoadFormData(entryPlant, entry.ProductId);

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
            if (!IsAuthenticated())
                return RedirectToAction("Login", "Admin");

            if (!ModelState.IsValid)
            {
                var plant = GetSessionPlant();
                LoadFormData(plant, model.ProductId);
                return View(model);
            }

            // Ownership re-check on POST — prevent forged requests
            var existing = _repo.GetById(model.StorageId);
            if (existing == null)
            {
                TempData["Error"] = "Entry not found.";
                return RedirectToAction("Index");
            }

            var sessionPlant = GetSessionPlant();
            if (!OwnsEntry(existing, sessionPlant))
            {
                TempData["Error"] = "You do not have permission to edit this entry.";
                return RedirectToAction("Index");
            }

            bool success = _repo.UpdateEntry(model);

            if (success)
                TempData["Success"] = "Storage entry updated successfully.";
            else
                TempData["Error"] = "Update failed. Please try again.";

            return RedirectToAction("Index");
        }


        // ═══════════════════════════════════════════════════════════
        //  DELETE GET
        //  Fix #7 — ownership check added
        // ═══════════════════════════════════════════════════════════
        public IActionResult Delete(int id)
        {
            if (!IsAuthenticated())
                return RedirectToAction("Login", "Admin");

            var entry = _repo.GetById(id);
            if (entry == null)
            {
                TempData["Error"] = "Storage entry not found.";
                return RedirectToAction("Index");
            }

            var plant = GetSessionPlant();
            if (!OwnsEntry(entry, plant))
            {
                TempData["Error"] = "You do not have permission to delete this entry.";
                return RedirectToAction("Index");
            }

            return View(entry);
        }

        // DELETE POST
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            if (!IsAuthenticated())
                return RedirectToAction("Login", "Admin");

            // Ownership re-check on POST
            var existing = _repo.GetById(id);
            if (existing == null)
            {
                TempData["Error"] = "Entry not found.";
                return RedirectToAction("Index");
            }

            var plant = GetSessionPlant();
            if (!OwnsEntry(existing, plant))
            {
                TempData["Error"] = "You do not have permission to delete this entry.";
                return RedirectToAction("Index");
            }

            bool success = _repo.DeleteEntry(id);

            if (success)
                TempData["Success"] = "Storage entry deleted successfully.";
            else
                TempData["Error"] = "Delete failed. Please try again.";

            return RedirectToAction("Index");
        }


        // ═══════════════════════════════════════════════════════════
        //  TEMPERATURE ALERTS
        //  Fix #2 — plantId passed to repo, no C# LINQ filtering
        // ═══════════════════════════════════════════════════════════
        public IActionResult Alerts(DateTime? fromDate, DateTime? toDate)
        {
            if (!IsAuthenticated())
                return RedirectToAction("Login", "Admin");

            var plant = GetSessionPlant();
            // Fix #2 — pass plantId to DB query instead of fetching all then filtering
            var alerts = _repo.GetTemperatureAlerts(plant?.PlantId, fromDate, toDate);

            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.PlantName = plant?.DisplayText ?? "All Plants";

            return View(alerts);
        }


        // ═══════════════════════════════════════════════════════════
        //  CAPACITY
        // ═══════════════════════════════════════════════════════════
        public IActionResult Capacity()
        {
            if (!IsAuthenticated())
                return RedirectToAction("Login", "Admin");

            var data = _repo.GetPlantCapacitySummary();
            var plant = GetSessionPlant();

            if (plant != null)
                data = data.Where(d => d.PlantId == plant.PlantId).ToList();

            ViewBag.PlantName = plant?.DisplayText ?? "All Plants";
            return View(data);
        }


        // ═══════════════════════════════════════════════════════════
        //  QUICK EDIT
        //  Fix #6 — ownership check added before updating
        // ═══════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult QuickEdit(int storageId, decimal milkQuantity, decimal? temperature)
        {
            if (!IsAuthenticated())
                return Json(new { success = false, message = "Session expired. Please log in again." });

            if (milkQuantity <= 0)
                return Json(new { success = false, message = "Quantity must be greater than 0." });

            // Fix #6 — verify entry belongs to session plant before updating
            var existing = _repo.GetById(storageId);
            if (existing == null)
                return Json(new { success = false, message = "Entry not found." });

            var plant = GetSessionPlant();
            if (!OwnsEntry(existing, plant))
                return Json(new { success = false, message = "You do not have permission to edit this entry." });

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
        //  DAILY REPORT
        // ═══════════════════════════════════════════════════════════
        public IActionResult Report(DateTime? fromDate, DateTime? toDate)
        {
            if (!IsAuthenticated())
                return RedirectToAction("Login", "Admin");

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