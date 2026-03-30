using DairyIndustry.Filters;
using DairyIndustry.Models.Production;
using DairyIndustry.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace DairyIndustry.Controllers
{
    public class ProductionController : Controller
    {
        private readonly IProductionRepository _productionRepo;
        private readonly IAdminRepository _adminRepo;
        // ── No ILogisticsRepository needed — vehicles fetched via _productionRepo ──

        public ProductionController(IProductionRepository productionRepo,
                                    IAdminRepository adminRepo)
        {
            _productionRepo = productionRepo;
            _adminRepo = adminRepo;
        }

        // ════════════════════════════════════════════════════════
        // INDEX — list all transfers
        // GET /Production/Index
        // ════════════════════════════════════════════════════════

        public IActionResult Index()
        {
            var transfers = _productionRepo.GetAllTransfers();
            return View(transfers);
        }

        // ════════════════════════════════════════════════════════
        // CREATE — show dispatch form
        // GET /Production/Create
        // ════════════════════════════════════════════════════════

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Batches = _productionRepo.GetClosedBatches();
            ViewBag.Plants = _adminRepo.GetAllPlants();
            ViewBag.Vehicles = _productionRepo.GetAllVehicles(); // ✅ ADD THIS                                                                 // ✅ from ProductionRepository
            return View();
        }

        // ════════════════════════════════════════════════════════
        // CREATE — submit dispatch form
        // POST /Production/Create
        // ════════════════════════════════════════════════════════

        [HttpPost]
        public IActionResult Create(int batchId, int vehicleId, int plantId,
                                    decimal dispatchQty, DateTime dispatchDate)
        {
            _productionRepo.DispatchMilkTransfer(batchId, vehicleId, plantId, dispatchQty, dispatchDate);
            TempData["Success"] = "Milk batch dispatched successfully.";
            return RedirectToAction("Index");
        }

        // ════════════════════════════════════════════════════════
        // RECEIVE — show receive form
        // GET /Production/Receive/5
        // ════════════════════════════════════════════════════════

        [HttpGet]
        public IActionResult Receive(int id)
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

        // ════════════════════════════════════════════════════════
        // RECEIVE — submit receive form
        // POST /Production/Receive
        // ════════════════════════════════════════════════════════

        [HttpPost]
        public IActionResult Receive(int transferId, decimal receivedQty, DateTime receivedDate)
        {
            _productionRepo.ReceiveMilkTransfer(transferId, receivedQty, receivedDate);
            TempData["Success"] = "Transfer marked as received. Raw milk inventory updated.";
            return RedirectToAction("Index");
        }

        // ════════════════════════════════════════════════════════
        // DETAIL — view one transfer's full info
        // GET /Production/Detail/5
        // ════════════════════════════════════════════════════════

        [HttpGet]
        public IActionResult Detail(int id)
        {
            var transfer = _productionRepo.GetTransferById(id);

            if (transfer == null)
                return NotFound();

            return View(transfer);
        }
    }
}