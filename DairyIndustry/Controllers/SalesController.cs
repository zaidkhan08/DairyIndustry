using DairyIndustry.Filters;
using DairyIndustry.Models;
using DairyIndustry.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DairyIndustry.Controllers
{
    // ── Default: any logged-in user can reach this controller ─────────────
    // Individual actions narrow by role where needed.
    [SessionAuthorize]
    public class SalesController : Controller
    {
        private readonly ISalesRepository _repo;

        private static readonly List<string> ValidOrderStatuses = new()
            { "Pending", "Confirmed", "Dispatched", "Delivered", "Cancelled" };

        public SalesController(ISalesRepository repo) => _repo = repo;


        // ════════════════════════════════════════════════════════════════════
        //  DASHBOARD — Admin only
        //  GET /Sales/Dashboard
        // ════════════════════════════════════════════════════════════════════
        [SessionAuthorize("Admin")]
        public IActionResult Dashboard()
        {
            var vm = new SalesDashboardViewModel
            {
                Summary = _repo.GetDashboardSummary(),
                OrdersByStatus = _repo.GetOrdersByStatus(),
                RecentOrders = _repo.GetOrders(null, null, null, null).Take(5).ToList(),
                TopDistributors = _repo.GetDistributorSales().Take(5).ToList()
            };
            return View(vm);
        }


        // ════════════════════════════════════════════════════════════════════
        //  ALL ORDERS — Admin only
        //  GET /Sales/Index
        // ════════════════════════════════════════════════════════════════════
        [SessionAuthorize("Admin")]

        public IActionResult Index(int? distributorId, string? status,
                                   DateTime? fromDate, DateTime? toDate)
        {
            var orders = _repo.GetOrders(distributorId, status, fromDate, toDate);
            LoadOrderFilterDropdowns(distributorId, status);
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            return View(orders);
        }


        // ════════════════════════════════════════════════════════════════════
        //  ORDER DETAILS — Admin sees all; Distributor sees own only
        //  GET /Sales/Details/5
        // ════════════════════════════════════════════════════════════════════
        [SessionAuthorize("Admin","Distributor")]

        public IActionResult Details(int id)
        {
            var order = _repo.GetOrderById(id);
            if (order == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction("Index");
            }

            // Distributor can only view their own orders
            string role = HttpContext.Session.GetString("RoleName") ?? "";
            if (role == "Distributor")
            {
                int myId = HttpContext.Session.GetInt32("DistributorId") ?? 0;
                if (order.DistributorId != myId)
                    return View("AccessDenied");
            }

            order.OrderDetails = _repo.GetOrderDetails(id);

            // Product dropdown only for Admin on Pending orders
            //if (order.CanAddItems && role == "Admin" && role == "Distributor")
            LoadProductDropdown();

            return View(order);
        }


        // ════════════════════════════════════════════════════════════════════
        //  CREATE ORDER — Admin only
        //  GET /Sales/Create
        // ════════════════════════════════════════════════════════════════════
        [SessionAuthorize("Admin","Distributor")]
        public IActionResult Create()
        {
            LoadDistributorDropdown();
            LoadPlantDropdown();
            return View(new SalesOrderFormModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [SessionAuthorize("Admin","Distributor")]
        public IActionResult Create(SalesOrderFormModel model)
        {
            if (!ModelState.IsValid)
            {
                LoadDistributorDropdown(model.DistributorId);
                LoadPlantDropdown(model.PlantId);
                return View(model);
            }

            int newId = _repo.CreateOrder(model);
            if (newId > 0)
            {
                TempData["Success"] = "Order created. Now add products.";
                return RedirectToAction("Details", new { id = newId });
            }

            TempData["Error"] = "Could not create order. Ensure the distributor is Approved.";
            LoadDistributorDropdown(model.DistributorId);
            LoadPlantDropdown(model.PlantId);
            return View(model);
        }


        // ════════════════════════════════════════════════════════════════════
        //  ADD ORDER DETAIL — Admin only
        //  POST /Sales/AddOrderDetail
        // ════════════════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SessionAuthorize("Admin","Distributor")]
        public IActionResult AddOrderDetail(AddOrderDetailFormModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid product entry. Check quantity and price.";
                return RedirectToAction("Details", new { id = model.OrderId });
            }

            bool ok = _repo.AddOrderDetail(model);
            TempData[ok ? "Success" : "Error"] = ok ? "Product added." : "Failed to add product.";
            return RedirectToAction("Details", new { id = model.OrderId });
        }


        // ════════════════════════════════════════════════════════════════════
        //  UPDATE ORDER STATUS — Admin only
        //  POST /Sales/UpdateStatus
        // ════════════════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SessionAuthorize("Admin")]
        public IActionResult UpdateStatus(int orderId, string newStatus)
        {
            if (!ValidOrderStatuses.Contains(newStatus))
            {
                TempData["Error"] = "Invalid status.";
                return RedirectToAction("Details", new { id = orderId });
            }

            bool ok = _repo.UpdateOrderStatus(orderId, newStatus);
            TempData[ok ? "Success" : "Error"] = ok
                ? $"Order status updated to {newStatus}."
                : "Status update failed.";

            return RedirectToAction("Details", new { id = orderId });
        }


        // ════════════════════════════════════════════════════════════════════
        //  DISTRIBUTORS LIST — Admin only
        //  GET /Sales/Distributors
        // ════════════════════════════════════════════════════════════════════
        [SessionAuthorize("Admin")]
        public IActionResult Distributors(string? status)
        {
            var all = _repo.GetDistributors();
            var shown = status == null ? all : all.Where(d => d.Status == status).ToList();
            ViewBag.DistributorSales = _repo.GetDistributorSales();
            ViewBag.StatusFilter = status;
            return View(shown);
        }


        // ════════════════════════════════════════════════════════════════════
        //  DISTRIBUTOR DETAILS — Admin only
        //  GET /Sales/DistributorDetails/5
        // ════════════════════════════════════════════════════════════════════
        [SessionAuthorize("Admin")]
        public IActionResult DistributorDetails(int id)
        {
            var dist = _repo.GetDistributorById(id);
            if (dist == null)
            {
                TempData["Error"] = "Distributor not found.";
                return RedirectToAction("Distributors");
            }
            ViewBag.Orders = _repo.GetOrders(id, null, null, null);
            return View(dist);
        }


        // ════════════════════════════════════════════════════════════════════
        //  ADD DISTRIBUTOR (admin direct-add) — Admin only
        //  GET/POST /Sales/AddDistributor
        // ════════════════════════════════════════════════════════════════════
        [SessionAuthorize("Admin")]
        public IActionResult AddDistributor() => View(new DistributorFormModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        [SessionAuthorize("Admin")]
        public IActionResult AddDistributor(DistributorFormModel model)
        {
            if (!ModelState.IsValid) return View(model);

            int newId = _repo.AddDistributor(model);
            TempData[newId > 0 ? "Success" : "Error"] = newId > 0
                ? "Distributor added successfully."
                : "Something went wrong.";
            return RedirectToAction("Distributors");
        }


        // ════════════════════════════════════════════════════════════════════
        //  EDIT DISTRIBUTOR — Admin only
        //  GET/POST /Sales/EditDistributor/5
        // ════════════════════════════════════════════════════════════════════
        [SessionAuthorize("Admin","Distributor")]
        public IActionResult EditDistributor(int id)
        {
            var dist = _repo.GetDistributorById(id);
            if (dist == null)
            {
                TempData["Error"] = "Distributor not found.";
                return RedirectToAction("Distributors");
            }
            return View(new DistributorFormModel
            {
                DistributorId = dist.DistributorId,
                DistributorName = dist.DistributorName,
                Location = dist.Location,
                ContactNumber = dist.ContactNumber,
                Email = dist.Email,
                Address = dist.Address,
                GSTIN = dist.GSTIN
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [SessionAuthorize("Admin","Distributor")]
        public IActionResult EditDistributor(DistributorFormModel model)
        {
            if (!ModelState.IsValid) return View(model);

            bool ok = _repo.UpdateDistributor(model);
            TempData[ok ? "Success" : "Error"] = ok ? "Distributor updated." : "Update failed.";
            return RedirectToAction("DistributorDetails", new { id = model.DistributorId});
        }


        // ════════════════════════════════════════════════════════════════════
        //  APPROVE / REJECT — Admin only
        //  SP: usp_Sales_ApproveDistributor  (action = "Approve" | "Reject")
        //  POST /Sales/ApproveDistributor
        // ════════════════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SessionAuthorize("Admin")]
        public IActionResult ApproveDistributor(int distributorId, string action)
        {
            if (action != "Approve" && action != "Reject")
            {
                TempData["Error"] = "Invalid action.";
                return RedirectToAction("DistributorDetails", new { id = distributorId });
            }

            bool ok = _repo.ApproveOrRejectDistributor(distributorId, action);
            TempData[ok ? "Success" : "Error"] = ok
                ? $"Distributor {(action == "Approve" ? "approved" : "rejected")} successfully."
                : "Action failed.";

            return RedirectToAction("DistributorDetails", new { id = distributorId });
        }


        // ════════════════════════════════════════════════════════════════════
        //  SUSPEND / REINSTATE — Admin only (inline queries)
        //  POST /Sales/SuspendDistributor
        //  POST /Sales/ReinstateDistributor
        // ════════════════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SessionAuthorize("Admin")]
        public IActionResult SuspendDistributor(int distributorId)
        {
            bool ok = _repo.SuspendDistributor(distributorId);
            TempData[ok ? "Success" : "Error"] = ok ? "Distributor suspended." : "Action failed.";
            return RedirectToAction("DistributorDetails", new { id = distributorId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [SessionAuthorize("Admin")]
        public IActionResult ReinstateDistributor(int distributorId)
        {
            bool ok = _repo.ReinstateDistributor(distributorId);
            TempData[ok ? "Success" : "Error"] = ok ? "Distributor reinstated." : "Action failed.";
            return RedirectToAction("DistributorDetails", new { id = distributorId });
        }


        // ════════════════════════════════════════════════════════════════════
        //  DISTRIBUTOR PORTAL — own orders
        //  GET /Sales/MyOrders
        // ════════════════════════════════════════════════════════════════════
        [SessionAuthorize("Distributor")]
        public IActionResult MyOrders()
        {
            int distributorId = HttpContext.Session.GetInt32("DistributorId") ?? 0;
            var orders = _repo.GetOrders(distributorId, null, null, null);
            ViewBag.DistributorName = HttpContext.Session.GetString("DistributorName");
            return View(orders);
        }


        // ════════════════════════════════════════════════════════════════════
        //  REPORTS — Admin only
        //  GET /Sales/Reports
        // ════════════════════════════════════════════════════════════════════
        [SessionAuthorize("Admin")]
        public IActionResult Reports()
        {
            var vm = new SalesDashboardViewModel
            {
                Summary = _repo.GetDashboardSummary(),
                OrdersByStatus = _repo.GetOrdersByStatus(),
                TopDistributors = _repo.GetDistributorSales()
            };
            return View(vm);
        }


        // ════════════════════════════════════════════════════════════════════
        //  PRIVATE HELPERS
        // ════════════════════════════════════════════════════════════════════
        //private void LoadDistributorDropdown(int? selected = null) =>
        //    ViewBag.Distributors = new SelectList(
        //        _repo.GetDistributors(), "DistributorId", "DisplayText", selected);


        private void LoadDistributorDropdown(int? selected = null) =>
    ViewBag.Distributors = new SelectList(
        _repo.GetDistributors()
             .Where(d => d.Status == "Approved")
             .ToList(),
        "DistributorId", "DisplayText", selected);

        private void LoadPlantDropdown(int? selected = null) =>
            ViewBag.Plants = new SelectList(
                _repo.GetPlants(), "PlantId", "DisplayText", selected);

        private void LoadProductDropdown(int? selected = null) =>
            ViewBag.Products = new SelectList(
                _repo.GetProducts(), "ProductId", "DisplayText", selected);

        private void LoadOrderFilterDropdowns(int? selectedDist = null, string? selectedStatus = null)
        {
            ViewBag.Distributors = new SelectList(
                _repo.GetDistributors(), "DistributorId", "DisplayText", selectedDist);
            ViewBag.Statuses = new SelectList(ValidOrderStatuses, selectedStatus);
        }
    }


    // ════════════════════════════════════════════════════════════════════════
    //  SEPARATE PUBLIC CONTROLLER — No [SessionAuthorize] at all
    //  Handles distributor self-registration. Public page, no login needed.
    //  Route: /DistributorRegister
    //  Views: Views/DistributorRegister/Register.cshtml
    //         Views/DistributorRegister/RegisterSuccess.cshtml
    //  (Copy Register.cshtml and RegisterSuccess.cshtml from Views/Sales/
    //   into a new Views/DistributorRegister/ folder)
    // ════════════════════════════════════════════════════════════════════════
    public class DistributorRegisterController : Controller
    {
        private readonly ISalesRepository _repo;

        public DistributorRegisterController(ISalesRepository repo) => _repo = repo;

        // GET /DistributorRegister
        public IActionResult Index() => View("Register", new DistributorRegisterModel());

        // POST /DistributorRegister
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(DistributorRegisterModel model)
        {
            if (!ModelState.IsValid) return View("Register", model);

            try
            {
                model.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);
                model.ConfirmPassword = BCrypt.Net.BCrypt.HashPassword(model.ConfirmPassword);

                _repo.RegisterDistributor(model);
                TempData["RegSuccess"] =
                    "Registration submitted! Our team will review and approve your account. " +
                    "Once approved, use the common login page with your credentials.";
                return RedirectToAction("Success");
            }
            catch (Exception ex)
            {
                string msg = ex.Message.Contains("Username already taken")
                    ? "That username is already taken. Please choose another."
                    : ex.Message.Contains("GSTIN already registered")
                        ? "A distributor with this GSTIN is already registered."
                        : "Registration failed. Please check your details and try again.";

                ModelState.AddModelError("", msg);
                return View("Register", model);
            }
        }

        // GET /DistributorRegister/Success
        public IActionResult Success() => View("RegisterSuccess");
    }
}