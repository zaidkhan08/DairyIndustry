using DairyIndustry.Models.Production;
using DairyIndustry.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace DairyIndustry.Controllers
{
    public class PlantController : Controller
    {
        private readonly IPlantRepository _plantRepo;

        public PlantController(IPlantRepository plantRepo)
        {
            _plantRepo = plantRepo;
        }


        private int GetStaffId() =>
            HttpContext.Session.GetInt32("StaffId") ?? 0;

        private int GetPlantId() =>
            _plantRepo.GetPlantIdByStaffId(GetStaffId());

        public IActionResult Index()
        {
            int staffId = HttpContext.Session.GetInt32("StaffId") ?? 0;
            int plantId = HttpContext.Session.GetInt32("PlantId") ?? 0;

            if (staffId == 0)
                return RedirectToAction("Login", "Auth");

            if (plantId == 0)
            {
                TempData["Error"] = "You are not assigned to any plant.";
                return RedirectToAction("Login", "Auth");
            }

            var plantName = HttpContext.Session.GetString("PlantName");
            var fullName = HttpContext.Session.GetString("StaffName");

            ViewBag.PlantName = plantName;
            ViewBag.StaffName = fullName;

            return View();
        }


        // ─────────────────────────────
        // INCOMING TRANSFERS LIST
        // ─────────────────────────────
        public IActionResult IncomingTransfers()
        {
            int staffId = GetStaffId();
            if (staffId == 0)
                return RedirectToAction("Login", "Auth");

            int plantId = GetPlantId();
            if (plantId == 0)
            {
                TempData["Error"] = "You are not assigned to any plant.";
                return RedirectToAction("Login", "Auth");
            }

            var transfers = _plantRepo.GetTransfersByPlant(plantId);

            if (transfers.Count == 0)
                TempData["Info"] = "No transfers found for your plant.";

            return View(transfers);
        }

        // ─────────────────────────────
        // RECEIVE MILK — GET
        // ─────────────────────────────
        [HttpGet]
        public IActionResult ReceiveMilk(int transferId)
        {
            int staffId = GetStaffId();
            if (staffId == 0)
                return RedirectToAction("Login", "Auth");

            var transfer = _plantRepo.GetTransferById(transferId);

            if (transfer == null)
            {
                TempData["Error"] = "Transfer not found.";
                return RedirectToAction("IncomingTransfers");
            }

            if (transfer.IsReceived)
            {
                TempData["Error"] = "This transfer is already received.";
                return RedirectToAction("IncomingTransfers");
            }

            var model = new ReceiveMilkViewModel
            {
                TransferId = transfer.TransferId,
                CenterName = transfer.CenterName,
                MilkTypeName = transfer.MilkTypeName,
                VehicleNumber = transfer.VehicleNumber,
                DriverName = transfer.DriverName,
                DispatchQty = transfer.DispatchQty,
                DispatchDate = transfer.DispatchDate,
                ReceivedDate = DateTime.Today
            };

            return View(model);
        }

        // ─────────────────────────────
        // RECEIVE MILK — POST
        // ─────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ReceiveMilk(ReceiveMilkViewModel model)
        {
            int staffId = GetStaffId();
            if (staffId == 0)
                return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                _plantRepo.ReceiveMilkTransfer(
                    model.TransferId,
                    model.ReceivedQty,
                    model.ReceivedDate
                );

                var loss = model.DispatchQty - model.ReceivedQty;

                TempData["Success"] =
                    $"Milk received successfully! " +
                    $"Received: {model.ReceivedQty:F2} L. " +
                    $"Loss: {loss:F2} L.";

                return RedirectToAction("IncomingTransfers");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return View(model);
            }
        }
    }
}
