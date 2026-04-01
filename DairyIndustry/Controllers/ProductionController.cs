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
        // GET /Production/Index
        // ════════════════════════════════════════════════════════
        public IActionResult Index()
        {
            var roleName = HttpContext.Session.GetString("RoleName");

            if (roleName == "Plant Manager" || roleName == "Collection Agent")
            {
                // Plant Manager sees only their plant's transfers
                // Collection Agent sees all transfers
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
        // GET /Production/Create
        // ════════════════════════════════════════════════════════
        [HttpGet]
        public IActionResult Create()
        {
            var RoleName = HttpContext.Session.GetString("RoleName");

            if (RoleName == "Plant Manager" || RoleName == "Collection Agent")
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
        // POST /Production/Create
        // ════════════════════════════════════════════════════════
        [HttpPost]
        public IActionResult Create(int batchId, int vehicleId, int plantId,
                                    decimal dispatchQty, DateTime dispatchDate)
        {
            var RoleName = HttpContext.Session.GetString("RoleName");

            if (RoleName == "Plant Manager" || RoleName == "Collection Agent")
            {
                _productionRepo.DispatchMilkTransfer(batchId, vehicleId, plantId, dispatchQty, dispatchDate);
                TempData["Success"] = "Milk batch dispatched successfully.";
                return RedirectToAction("Index");
            }

            return RedirectToAction("AccessDenied", "Home");
        }

        // ════════════════════════════════════════════════════════
        // RECEIVE — show receive form
        // GET /Production/Receive/5
        // ════════════════════════════════════════════════════════
        [HttpGet]
        [SessionAuthorize("Plant Manager")]
        public IActionResult Receive(int id)
        {
            var RoleName = HttpContext.Session.GetString("RoleName");

            if (RoleName == "Plant Manager" || RoleName == "Collection Agent")
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
        // POST /Production/Receive
        // ════════════════════════════════════════════════════════
        [HttpPost]
        [SessionAuthorize("Plant Manager")]
        public IActionResult Receive(int transferId, decimal receivedQty, DateTime receivedDate)
        {
            var RoleName = HttpContext.Session.GetString("RoleName");

            if (RoleName == "Plant Manager" || RoleName == "Collection Agent")
            {
                _productionRepo.ReceiveMilkTransfer(transferId, receivedQty, receivedDate);
                TempData["Success"] = "Transfer marked as received. Raw milk inventory updated.";
                return RedirectToAction("Index");
            }

            return RedirectToAction("AccessDenied", "Home");
        }

        // ════════════════════════════════════════════════════════
        // DETAIL — view one transfer's full info
        // GET /Production/Detail/5
        // ════════════════════════════════════════════════════════
        [HttpGet]
        [SessionAuthorize("Plant Manager")]
        public IActionResult Detail(int id)
        {
            var RoleName = HttpContext.Session.GetString("RoleName");

            if (RoleName == "Plant Manager" || RoleName == "Collection Agent")
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
        // GET /Production/RawMilkInventory
        // ════════════════════════════════════════════════════════
        public IActionResult RawMilkInventory()
        {
            var inventory = _productionRepo.GetRawMilkInventory();
            return View(inventory);
        }

        // ════════════════════════════════════════════════════════
        // PRODUCTS — LIST
        // GET /Production/Products
        // ════════════════════════════════════════════════════════
        public IActionResult Products()
        {
            var products = _productionRepo.GetAllProducts();
            return View(products);
        }

        // ════════════════════════════════════════════════════════
        // PRODUCTS — ADD
        // POST /Production/AddProduct
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
        // GET /Production/EditProduct/5
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
        // POST /Production/EditProduct
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
        // POST /Production/DeleteProduct
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
        // GET /Production/Batches
        // ════════════════════════════════════════════════════════
        public IActionResult Batches()
        {
            var batches = _productionRepo.GetAllProductionBatches();
            return View(batches);
        }

        // ════════════════════════════════════════════════════════
        // PRODUCTION BATCHES — START (GET)
        // GET /Production/StartBatch
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
        // POST /Production/StartBatch
        // ════════════════════════════════════════════════════════
        [HttpPost]
        public IActionResult StartBatch(int plantId, int productId,
                                        decimal milkUsedQuantity, DateTime productionDate, int milkTypeId)
        {
            _productionRepo.StartProductionBatch(plantId, productId, milkUsedQuantity, productionDate, milkTypeId);
            TempData["Success"] = "Production batch started successfully.";
            return RedirectToAction("Batches");
        }

        // ════════════════════════════════════════════════════════
        // PRODUCTION BATCHES — DETAIL
        // GET /Production/BatchDetail/5
        // ════════════════════════════════════════════════════════
        [HttpGet]
        public IActionResult BatchDetail(int id)
        {
            var batch = _productionRepo.GetProductionBatchById(id);
            if (batch == null) return NotFound();
            return View(batch);
        }

        // ════════════════════════════════════════════════════════
        // PRODUCTION BATCHES — UPDATE STATUS
        // POST /Production/UpdateBatchStatus
        // ════════════════════════════════════════════════════════
        [HttpPost]
        public IActionResult UpdateBatchStatus(int productionBatchId, string batchStatus)
        {
            _productionRepo.UpdateBatchStatus(productionBatchId, batchStatus);
            TempData["Success"] = $"Batch status updated to {batchStatus}.";
            return RedirectToAction("BatchDetail", new { id = productionBatchId });
        }

        // ════════════════════════════════════════════════════════
        // PRODUCT WASTAGE — LIST
        // GET /Production/ProductWastage
        // ════════════════════════════════════════════════════════
        public IActionResult ProductWastage()
        {
            var wastage = _productionRepo.GetAllProductWastage();
            return View(wastage);
        }

        // ════════════════════════════════════════════════════════
        // PRODUCT WASTAGE — ADD (GET)
        // GET /Production/AddWastage
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
        // POST /Production/AddWastage
        // ════════════════════════════════════════════════════════
        [HttpPost]
        public IActionResult AddWastage(int batchId, int productId,
                                        decimal quantity, string reason)
        {
            _productionRepo.AddProductWastage(batchId, productId, quantity, reason);
            TempData["Success"] = "Wastage recorded successfully.";
            return RedirectToAction("ProductWastage");
        }

        // ════════════════════════════════════════════════════════
        // PRODUCT WASTAGE — BY BATCH
        // GET /Production/WastageByBatch/5
        // ════════════════════════════════════════════════════════
        [HttpGet]
        public IActionResult WastageByBatch(int id)
        {
            var wastage = _productionRepo.GetWastageByBatch(id);
            ViewBag.BatchId = id;
            return View(wastage);
        }
    }
}