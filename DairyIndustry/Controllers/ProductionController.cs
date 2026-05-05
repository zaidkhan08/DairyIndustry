using DairyIndustry.Filters;
using DairyIndustry.Models.Production;
using DairyIndustry.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace DairyIndustry.Controllers
{
    [ServiceFilter(typeof(ActionLogFilter))]
    public class ProductionController : Controller
    {
        private readonly IProductionRepository _productionRepo;
        private readonly IAdminRepository _adminRepo;
        private readonly ILogisticsRepository _logisticsRepo;

        public ProductionController(IProductionRepository productionRepo,
                                    IAdminRepository adminRepo,
                                    ILogisticsRepository logisticsRepo)
        {
            _productionRepo = productionRepo;
            _adminRepo = adminRepo;
            _logisticsRepo = logisticsRepo;
        }

        // ════════════════════════════════════════════════════════
        // INDEX — list transfers (scoped by plant for Plant Manager)
        // ════════════════════════════════════════════════════════
        public IActionResult Index()
        {
            var roleName = HttpContext.Session.GetString("RoleName");

            if (roleName == "Plant Manager" || roleName == "Collection Agent")
            {
                int? plantId = null;
                if (roleName == "Plant Manager")
                    plantId = HttpContext.Session.GetInt32("PlantId");

                var transfers = _productionRepo.GetAllTransfers(plantId);
                return View(transfers);
            }

            return RedirectToAction("AccessDenied", "Home");
        }

        // ════════════════════════════════════════════════════════
        // CREATE — show dispatch form
        // ════════════════════════════════════════════════════════
        [HttpGet]
        public IActionResult Create()
        {
            var roleName = HttpContext.Session.GetString("RoleName");

            if (roleName == "Plant Manager" || roleName == "Collection Agent")
            {
                ViewBag.Batches = _productionRepo.GetClosedBatches();
                ViewBag.Plants = _adminRepo.GetAllPlants();
                ViewBag.Vehicles = _productionRepo.GetAllVehicles();
                return View();
            }

            return RedirectToAction("AccessDenied", "Home");
        }

        // ════════════════════════════════════════════════════════
        // CREATE — submit dispatch form
        // ════════════════════════════════════════════════════════
        [HttpPost]
        public IActionResult Create(int batchId, int vehicleId, int plantId,
                                    decimal dispatchQty, DateTime dispatchDate)
        {
            var roleName = HttpContext.Session.GetString("RoleName");

            if (roleName == "Plant Manager" || roleName == "Collection Agent")
            {
                _productionRepo.DispatchMilkTransfer(batchId, vehicleId, plantId, dispatchQty, dispatchDate);
                TempData["Success"] = "Milk batch dispatched successfully.";
                return RedirectToAction("Index");
            }

            return RedirectToAction("AccessDenied", "Home");
        }

        // ════════════════════════════════════════════════════════
        // RECEIVE — show receive form
        // ════════════════════════════════════════════════════════
        [HttpGet]
        [SessionAuthorize("Plant Manager")]
        public IActionResult Receive(int id)
        {
            var roleName = HttpContext.Session.GetString("RoleName");

            if (roleName == "Plant Manager" || roleName == "Collection Agent")
            {
                var transfer = _productionRepo.GetTransferById(id);

                if (transfer == null)
                    return NotFound();

                if (transfer.ReceivedDate.HasValue)
                {
                    TempData["Error"] = "This transfer has already been received.";
                    return RedirectToAction("Index");
                }

                return View(transfer);
            }

            return RedirectToAction("AccessDenied", "Home");
        }

        // ════════════════════════════════════════════════════════
        // RECEIVE — submit receive form
        // ════════════════════════════════════════════════════════
        [HttpPost]
        [SessionAuthorize("Plant Manager")]
        public IActionResult Receive(int transferId, decimal receivedQty, DateTime receivedDate)
        {
            var roleName = HttpContext.Session.GetString("RoleName");

            if (roleName == "Plant Manager" || roleName == "Collection Agent")
            {
                _productionRepo.ReceiveMilkTransfer(transferId, receivedQty, receivedDate);
                TempData["Success"] = "Transfer marked as received. Raw milk inventory updated.";
                return RedirectToAction("Index");
            }

            return RedirectToAction("AccessDenied", "Home");
        }

        // ════════════════════════════════════════════════════════
        // DETAIL — view one transfer's full info
        // ════════════════════════════════════════════════════════
        [HttpGet]
        [SessionAuthorize("Plant Manager")]
        public IActionResult Detail(int id)
        {
            var roleName = HttpContext.Session.GetString("RoleName");

            if (roleName == "Plant Manager" || roleName == "Collection Agent")
            {
                var transfer = _productionRepo.GetTransferById(id);

                if (transfer == null)
                    return NotFound();

                return View(transfer);
            }

            return RedirectToAction("AccessDenied", "Home");
        }

        // ════════════════════════════════════════════════════════
        // RAW MILK INVENTORY — VIEW ONLY
        // ════════════════════════════════════════════════════════
        public IActionResult RawMilkInventory()
        {
            var roleName = HttpContext.Session.GetString("RoleName");
            int? plantId = null;
            if (roleName == "Plant Manager")
                plantId = HttpContext.Session.GetInt32("PlantId");

            var inventory = _productionRepo.GetRawMilkInventory(plantId);
            return View(inventory);
        }

        // ════════════════════════════════════════════════════════
        // PRODUCTS — LIST
        // ════════════════════════════════════════════════════════
        public IActionResult Products()
        {
            var products = _productionRepo.GetAllProducts();
            return View(products);
        }

        // ════════════════════════════════════════════════════════
        // PRODUCTS — ADD
        // ════════════════════════════════════════════════════════
        [HttpPost]
        public IActionResult AddProduct(string productName, string productType,
                                        decimal mrp, string unit, int? shelfLifeDays)
        {
            _productionRepo.AddProduct(productName, productType, mrp, unit, shelfLifeDays);
            TempData["Success"] = "Product added successfully.";
            return RedirectToAction("Products");
        }

        // ════════════════════════════════════════════════════════
        // PRODUCTS — EDIT GET
        // ════════════════════════════════════════════════════════
        [HttpGet]
        public IActionResult EditProduct(int id)
        {
            var product = _productionRepo.GetProductById(id);
            if (product == null) return NotFound();
            return View(product);
        }

        // ════════════════════════════════════════════════════════
        // PRODUCTS — EDIT POST
        // ════════════════════════════════════════════════════════
        [HttpPost]
        public IActionResult EditProduct(ProductModel product)
        {
            _productionRepo.UpdateProduct(product);
            TempData["Success"] = "Product updated successfully.";
            return RedirectToAction("Products");
        }

        // ════════════════════════════════════════════════════════
        // PRODUCTS — DELETE
        // ════════════════════════════════════════════════════════
        [HttpPost]
        public IActionResult DeleteProduct(int id)
        {
            _productionRepo.DeleteProduct(id);
            TempData["Success"] = "Product deleted.";
            return RedirectToAction("Products");
        }

        // ════════════════════════════════════════════════════════
        // PRODUCTION BATCHES — LIST
        // ════════════════════════════════════════════════════════
        public IActionResult Batches()
        {
            var roleName = HttpContext.Session.GetString("RoleName");
            int? plantId = null;
            if (roleName == "Plant Manager")
                plantId = HttpContext.Session.GetInt32("PlantId");

            var batches = _productionRepo.GetAllProductionBatches(plantId);

            ViewBag.Plants = _adminRepo.GetAllPlants();
            ViewBag.Products = _productionRepo.GetAllProducts();
            ViewBag.MilkTypes = _adminRepo.GetAllMilkTypes();

            return View(batches);
        }

        // ════════════════════════════════════════════════════════
        // PRODUCTION BATCHES — START (GET)
        // ════════════════════════════════════════════════════════
        [HttpGet]
        public IActionResult StartBatch()
        {
            ViewBag.Plants = _adminRepo.GetAllPlants();
            ViewBag.Products = _productionRepo.GetAllProducts();
            ViewBag.MilkTypes = _adminRepo.GetAllMilkTypes();
            return View();
        }

        // ════════════════════════════════════════════════════════
        // PRODUCTION BATCHES — START (POST)
        // ════════════════════════════════════════════════════════
        [HttpPost]
        public IActionResult StartBatch(int plantId, int productId,
                                        decimal milkUsedQuantity, DateTime productionDate, int milkTypeId)
        {
            var roleName = HttpContext.Session.GetString("RoleName");
            if (roleName == "Plant Manager")
            {
                var sessionPlantId = HttpContext.Session.GetInt32("PlantId");
                if (sessionPlantId.HasValue)
                    plantId = sessionPlantId.Value;
            }

            try
            {
                _productionRepo.StartProductionBatch(plantId, productId, milkUsedQuantity, productionDate, milkTypeId);
                TempData["Success"] = "Production batch started successfully.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("Batches");
        }

        // ════════════════════════════════════════════════════════
        // PRODUCTION BATCHES — DETAIL
        // ════════════════════════════════════════════════════════
        [HttpGet]
        public IActionResult BatchDetail(int id)
        {
            var batch = _productionRepo.GetProductionBatchById(id);
            if (batch == null) return NotFound();

            // Pass milk types for the manual process wastage drawer
            ViewBag.MilkTypes = _adminRepo.GetAllMilkTypes();

            return View(batch);
        }

        // ════════════════════════════════════════════════════════
        // PRODUCTION BATCHES — UPDATE STATUS
        // Auto-logs QCFailed wastage in the repository layer.
        // ════════════════════════════════════════════════════════
        [HttpPost]
        public IActionResult UpdateBatchStatus(int productionBatchId, string batchStatus)
        {
            _productionRepo.UpdateBatchStatus(productionBatchId, batchStatus);

            string message = batchStatus == "QCFailed"
                ? $"Batch marked as QC Failed. Milk wastage has been automatically recorded."
                : $"Batch status updated to {batchStatus}.";

            TempData["Success"] = message;
            return RedirectToAction("BatchDetail", new { id = productionBatchId });
        }

        // ════════════════════════════════════════════════════════
        // PRODUCT WASTAGE — LIST  (finished-goods wastage)
        // ════════════════════════════════════════════════════════
        public IActionResult ProductWastage()
        {
            var roleName = HttpContext.Session.GetString("RoleName");
            int? plantId = null;
            if (roleName == "Plant Manager")
                plantId = HttpContext.Session.GetInt32("PlantId");

            var wastage = _productionRepo.GetAllProductWastage(plantId);

            ViewBag.Batches = _productionRepo.GetBatchesForWastage();
            ViewBag.Products = _productionRepo.GetAllProducts();

            return View(wastage);
        }

        // ════════════════════════════════════════════════════════
        // PRODUCT WASTAGE — ADD (GET)
        // ════════════════════════════════════════════════════════
        [HttpGet]
        public IActionResult AddWastage()
        {
            ViewBag.Batches = _productionRepo.GetBatchesForWastage();
            ViewBag.Products = _productionRepo.GetAllProducts();
            return View();
        }

        // ════════════════════════════════════════════════════════
        // PRODUCT WASTAGE — ADD (POST)
        // ════════════════════════════════════════════════════════
        [HttpPost]
        public IActionResult AddWastage(int batchId, int productId,
                                        decimal quantity, string reason)
        {
            try
            {
                _productionRepo.AddProductWastage(batchId, productId, quantity, reason);
                TempData["Success"] = "Wastage recorded successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("ProductWastage");
        }

        // ════════════════════════════════════════════════════════
        // PRODUCT WASTAGE — BY BATCH
        // ════════════════════════════════════════════════════════
        [HttpGet]
        public IActionResult WastageByBatch(int id)
        {
            var wastage = _productionRepo.GetWastageByBatch(id);
            ViewBag.BatchId = id;
            return View(wastage);
        }

        // ════════════════════════════════════════════════════════
        // MILK PROCESS WASTAGE — LIST  (raw-milk wastage)
        // Shows both QCFailed (auto) and ProcessWastage (manual) entries.
        // GET /Production/MilkProcessWastage
        // ════════════════════════════════════════════════════════
        public IActionResult MilkProcessWastage()
        {
            var roleName = HttpContext.Session.GetString("RoleName");

            if (roleName != "Plant Manager")
                return RedirectToAction("AccessDenied", "Home");

            int? plantId = HttpContext.Session.GetInt32("PlantId");
            var wastage = _productionRepo.GetAllMilkProcessWastage(plantId);

            return View(wastage);
        }

        // ════════════════════════════════════════════════════════
        // MILK PROCESS WASTAGE — ADD MANUAL ENTRY (POST)
        // Called from the drawer on BatchDetail page.
        // GET /Production/AddMilkProcessWastage
        // ════════════════════════════════════════════════════════
        [HttpPost]
        public IActionResult AddMilkProcessWastage(int productionBatchId, int milkTypeId,
                                                    decimal wastageQuantity, string reason)
        {
            var roleName = HttpContext.Session.GetString("RoleName");

            if (roleName != "Plant Manager")
                return RedirectToAction("AccessDenied", "Home");

            int plantId = HttpContext.Session.GetInt32("PlantId") ?? 0;

            if (plantId == 0)
            {
                TempData["Error"] = "Session expired. Please login again.";
                return RedirectToAction("Login", "Admin");
            }

            try
            {
                _productionRepo.AddMilkProcessWastage(productionBatchId, plantId, milkTypeId,
                                                       wastageQuantity, reason);
                TempData["Success"] = $"Process wastage of {wastageQuantity} L recorded successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("BatchDetail", new { id = productionBatchId });
        }

        // ════════════════════════════════════════════════════════
        // QUALITY TESTS — LIST
        // ════════════════════════════════════════════════════════
        public IActionResult QualityTests()
        {
            var roleName = HttpContext.Session.GetString("RoleName");

            if (roleName != "Plant Manager")
                return RedirectToAction("AccessDenied", "Home");

            int? plantId = HttpContext.Session.GetInt32("PlantId");
            var tests = _productionRepo.GetAllQualityTests(plantId);
            return View(tests);
        }

        // ════════════════════════════════════════════════════════
        // QUALITY TESTS — ADD FORM (GET)
        // ════════════════════════════════════════════════════════
        [HttpGet]
        public IActionResult AddQualityTest(int id)
        {
            var roleName = HttpContext.Session.GetString("RoleName");

            if (roleName != "Plant Manager")
                return RedirectToAction("AccessDenied", "Home");

            var transfer = _productionRepo.GetTransferById(id);
            if (transfer == null)
                return NotFound();

            if (!transfer.ReceivedDate.HasValue)
            {
                TempData["Error"] = "Quality test can only be recorded after the transfer is received.";
                return RedirectToAction("Index");
            }

            var existing = _productionRepo.GetQualityTestByTransfer(id);
            if (existing != null)
            {
                TempData["Error"] = $"A quality test for Transfer #{id} already exists.";
                return RedirectToAction("QualityTestDetail", new { id });
            }

            return View(transfer);
        }

        // ════════════════════════════════════════════════════════
        // QUALITY TESTS — SUBMIT (POST)
        // ════════════════════════════════════════════════════════
        [HttpPost]
        public IActionResult AddQualityTest(int transferId, decimal testedFat,
                                             decimal testedCLR, DateTime testDate)
        {
            var roleName = HttpContext.Session.GetString("RoleName");

            if (roleName != "Plant Manager")
                return RedirectToAction("AccessDenied", "Home");

            try
            {
                _productionRepo.AddQualityTest(transferId, testedFat, testedCLR, testDate);
                TempData["Success"] = $"Quality test for Transfer #{transferId} recorded successfully.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("QualityTests");
        }

        // ════════════════════════════════════════════════════════
        // QUALITY TESTS — DETAIL
        // ════════════════════════════════════════════════════════
        [HttpGet]
        public IActionResult QualityTestDetail(int id)
        {
            var roleName = HttpContext.Session.GetString("RoleName");

            if (roleName != "Plant Manager")
                return RedirectToAction("AccessDenied", "Home");

            var test = _productionRepo.GetQualityTestByTransfer(id);
            if (test == null)
                return NotFound();

            return View(test);
        }

        // ════════════════════════════════════════════════════════
        // TRANSFER LOSS LOG
        // ════════════════════════════════════════════════════════
        public IActionResult TransferLossLog()
        {
            var roleName = HttpContext.Session.GetString("RoleName");

            if (roleName != "Plant Manager" && roleName != "Collection Agent")
                return RedirectToAction("AccessDenied", "Home");

            int? plantId = null;
            if (roleName == "Plant Manager")
                plantId = HttpContext.Session.GetInt32("PlantId");

            var logs = _productionRepo.GetTransferLossLog(plantId);
            var summary = _productionRepo.GetLossSummary(plantId);

            ViewBag.Summary = summary;
            return View(logs);
        }
    }
}