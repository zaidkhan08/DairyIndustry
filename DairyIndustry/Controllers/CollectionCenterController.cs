using DairyIndustry.Filters;
using DairyIndustry.Models.Collection;
using DairyIndustry.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

public class CollectionCenterController : Controller
{
    private readonly ICollectionCenterRepository _collectionCenterRepo;
    private readonly IFarmerRepository _farmerRepo;

    public CollectionCenterController(ICollectionCenterRepository repository, IFarmerRepository farmerRepo)
    {
        _collectionCenterRepo = repository;
        _farmerRepo = farmerRepo;
    }

    // ================= DASHBOARD =================
    [HttpGet]
    [SessionAuthorize("Collection Agent")]
    public IActionResult Dashboard()
    {
        int? staffId = HttpContext.Session.GetInt32("StaffId");

        if (!staffId.HasValue || staffId == 0)
            return RedirectToAction("Login", "Admin");

        var dashboard = _collectionCenterRepo.GetCollectionCenterByStaff(staffId.Value);

        if (dashboard == null)
        {
            ViewBag.Error = "No collection center assigned to you.";
            return View();
        }

        // 🔥 Get current batch
        int batchId = _collectionCenterRepo.GetCurrentBatchId(dashboard.CenterId);

        dashboard.BatchId = batchId;

        // 🔥 STORE IN SESSION (IMPORTANT)
        HttpContext.Session.SetInt32("BatchId", batchId);

        return View(dashboard);
    }

    // ================= OPEN BATCH =================
    public IActionResult Open()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Open(OpenBatchRequest request)
    {
        try
        {
            int staffId = HttpContext.Session.GetInt32("StaffId") ?? 0;

            var center = _collectionCenterRepo.GetCollectionCenterByStaff(staffId);

            int batchId = _collectionCenterRepo.OpenBatch(
                center.CenterId,
                request.Shift,
                request.BatchDate
            );

            // 🔥 STORE IN SESSION
            HttpContext.Session.SetInt32("BatchId", batchId);

            TempData["Message"] = "Batch opened successfully. ID: " + batchId;
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction("Dashboard");
    }

    // ================= CLOSE BATCH =================
    public IActionResult Close()
    {
        return View();
    }

    //[HttpPost]
    //public IActionResult Close(int batchId)
    //{
    //    try
    //    {
    //        _collectionCenterRepo.CloseBatch(batchId);

    //        // 🔥 REMOVE FROM SESSION (optional)
    //        HttpContext.Session.Remove("BatchId");

    //        TempData["Message"] = "Batch closed successfully.";
    //    }
    //    catch (Exception ex)
    //    {
    //        TempData["Error"] = ex.Message;
    //    }

    //    return RedirectToAction("Dashboard");
    //}

    [HttpPost]
    public IActionResult Close(int batchId)
    {
        try
        {
            _collectionCenterRepo.CloseBatch(batchId);

            // SUCCESS MESSAGE
            TempData["Success"] = $"Batch {batchId} closed successfully!";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        // Redirect back to SAME page
        return RedirectToAction("BatchStatus");
    }

    // ================= CREATE COLLECTION =================
    public IActionResult Create()
    {
        var model = new MilkCollectionViewModel();

        int staffId = HttpContext.Session.GetInt32("StaffId") ?? 0;

        var center = _collectionCenterRepo.GetCollectionCenterByStaff(staffId);

        model.CenterName = center.CenterName;

        // 🔥 GET BatchId from Session
        model.BatchId = HttpContext.Session.GetInt32("BatchId") ?? 0;

        LoadFarmers(model);
        LoadMilkTypes(model);
        LoadBatches(model, center.CenterId);

        return View(model);
    }

    [HttpPost]
    public IActionResult Create(MilkCollectionViewModel model)
    {
        try
        {
            int staffId = HttpContext.Session.GetInt32("StaffId") ?? 0;

            var center = _collectionCenterRepo.GetCollectionCenterByStaff(staffId);

            model.CenterName = center.CenterName;

            //  Ensure BatchId from Session
            model.BatchId = HttpContext.Session.GetInt32("BatchId") ?? 0;

            var result = _collectionCenterRepo.RecordMilk(
                model.FarmerId,
                center.CenterId,
                model.MilkTypeId,
                model.BatchId,
                model.Quantity,
                model.Shift,
                model.CollectionDate,
                model.AppliedFat,
                model.AppliedCLR
            );

            TempData["Message"] = $"Saved! ID: {result.collectionId}, Amount: {result.amount}";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction("Dashboard");
    }
    public IActionResult BatchCollections(int? batchId)
    {
        // ❌ If batchId NOT coming from URL → don't rely on old session silently
        if (batchId == null || batchId == 0)
        {
            TempData["Error"] = "Invalid batch selected.";
            return RedirectToAction("BatchStatus"); // 👈 go back safely
        }

        // ✅ Always update session with current batch
        HttpContext.Session.SetInt32("BatchId", batchId.Value);

        // ✅ For display purpose
        ViewBag.BatchId = batchId.Value;

        // ✅ Get data
        var data = _collectionCenterRepo.GetBatchCollections(batchId.Value);

        // ✅ Handle no data case (optional)
        if (data == null || data.Count == 0)
        {
            TempData["Error"] = "No records found for this batch.";
        }

        return View(data);
    }
    private void LoadMilkTypes(MilkCollectionViewModel model)
    {
        var types = _collectionCenterRepo.GetMilkTypes();

        model.MilkTypes = types.Select(t => new SelectListItem
        {
            Value = t.MilkTypeId.ToString(),
            Text = t.MilkTypeName
        }).ToList();
    }
    private void LoadBatches(MilkCollectionViewModel model, int centerId)
    {
        var batches = _collectionCenterRepo.GetOpenBatches(centerId);

        model.Batches = batches.Select(b => new SelectListItem
        {
            Value = b.BatchId.ToString(),
            Text = $"Batch {b.BatchId} - {b.Shift} ({b.BatchDate:dd MMM})"
        }).ToList();
    }

    // ================= HELPERS =================
    private void LoadFarmers(MilkCollectionViewModel model)
    {
        var farmers = _collectionCenterRepo.GetFarmers();

        model.Farmers = farmers.Select(f => new SelectListItem
        {
            Value = f.FarmerId.ToString(),
            Text = f.FarmerName
        }).ToList();
    }

    public IActionResult BatchStatus()
    {
        int staffId = HttpContext.Session.GetInt32("StaffId") ?? 0;

        var center = _collectionCenterRepo.GetCollectionCenterByStaff(staffId);

        var batches = _collectionCenterRepo.GetBatchesByCenter(center.CenterId);

        return View(batches);
    }
    public IActionResult Inventory(int? centerId)
    {
        int staffId = HttpContext.Session.GetInt32("StaffId") ?? 0;

        var center = _collectionCenterRepo.GetCollectionCenterByStaff(staffId);

        // If no centerId passed → use logged-in user's center
        int selectedCenterId = centerId ?? center.CenterId;

        var inventory = _collectionCenterRepo.GetCenterInventory(selectedCenterId);

        // For dropdown (optional if multi-center access)
        ViewBag.CenterList = new List<SelectListItem>
    {
        new SelectListItem { Value = center.CenterId.ToString(), Text = center.CenterName }
    };

        ViewBag.SelectedCenter = selectedCenterId;

        return View(inventory);
    }

}