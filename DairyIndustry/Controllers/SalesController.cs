using DairyIndustry.Filters;
using DairyIndustry.Models;
using DairyIndustry.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DairyIndustry.Controllers
{
    [SessionAuthorize]
    public class SalesController : Controller
    {
        private readonly ISalesRepository _repo;

        private static readonly List<string> ValidOrderStatuses = new()
            { "Pending", "Confirmed", "Dispatched", "Delivered", "Cancelled" };

        public SalesController(ISalesRepository repo) => _repo = repo;


        // ════════════════════════════════════════════════════════════════════
        //  DASHBOARD — Admin only
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
        // ════════════════════════════════════════════════════════════════════
        [SessionAuthorize("Admin", "Distributor")]
        public IActionResult Details(int id)
        {
            var order = _repo.GetOrderById(id);
            if (order == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction("Index");
            }

            string role = HttpContext.Session.GetString("RoleName") ?? "";
            if (role == "Distributor")
            {
                int myId = HttpContext.Session.GetInt32("DistributorId") ?? 0;
                if (order.DistributorId != myId)
                    return View("AccessDenied");
            }

            order.OrderDetails = _repo.GetOrderDetails(id);

            // Pass the full product list so the view can show MRP automatically
            ViewBag.ProductList = _repo.GetProducts();
            LoadProductDropdown();

            return View(order);
        }


        // ════════════════════════════════════════════════════════════════════
        //  CREATE ORDER
        //
        //  ADMIN path   → normal SP flow, distributor dropdown shown.
        //  DISTRIBUTOR path → on this single page choose plant + product + qty,
        //                     then POST calls PlaceDistributorOrder (inline,
        //                     smart merge, auto-MRP). We NEVER call the SP
        //                     usp_Sales_CreateOrder for distributors because that
        //                     SP validates Status='Approved' and throws the
        //                     "Distributor not found or not yet approved" error.
        // ════════════════════════════════════════════════════════════════════
        [SessionAuthorize("Admin", "Distributor")]
        public IActionResult Create()
        {
            string role = HttpContext.Session.GetString("RoleName") ?? "";

            if (role == "Distributor")
            {
                int distId = HttpContext.Session.GetInt32("DistributorId") ?? 0;
                ViewBag.IsDistributor = true;
                ViewBag.DistributorName = HttpContext.Session.GetString("DistributorName") ?? "";
                LoadPlantDropdown();
                ViewBag.ProductList = _repo.GetProducts(); // full list for MRP hint + select
                return View(new SalesOrderFormModel { DistributorId = distId });
            }

            ViewBag.IsDistributor = false;
            LoadDistributorDropdown();
            LoadPlantDropdown();
            return View(new SalesOrderFormModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [SessionAuthorize("Admin", "Distributor")]
        public IActionResult Create(SalesOrderFormModel model)
        {
            string role = HttpContext.Session.GetString("RoleName") ?? "";

            // ── DISTRIBUTOR POST ──────────────────────────────────────────
            // Read extra fields posted from the distributor form.
            // Never call usp_Sales_CreateOrder for distributors.
            if (role == "Distributor")
            {
                int distId = HttpContext.Session.GetInt32("DistributorId") ?? 0; // always from session

                bool productOk = int.TryParse(Request.Form["SelectedProductId"], out int productId) && productId > 0;
                bool qtyOk = decimal.TryParse(Request.Form["SelectedQuantity"],
                                              System.Globalization.NumberStyles.Any,
                                              System.Globalization.CultureInfo.InvariantCulture,
                                              out decimal quantity) && quantity > 0;

                if (!productOk || !qtyOk || model.PlantId <= 0)
                {
                    TempData["Error"] = "Please select a plant, a product, and enter a valid quantity.";
                    ViewBag.IsDistributor = true;
                    ViewBag.DistributorName = HttpContext.Session.GetString("DistributorName");
                    LoadPlantDropdown(model.PlantId);
                    ViewBag.ProductList = _repo.GetProducts();
                    return View(model);
                }

                try
                {
                    int orderId = _repo.PlaceDistributorOrder(distId, model.PlantId, productId, quantity);
                    TempData["Success"] = "Order placed! Unit price set automatically from product rate.";
                    return RedirectToAction("Details", new { id = orderId });
                }
                catch (Exception ex)
                {
                    string msg = ex.Message.Contains("not found or is inactive")
                        ? "Selected product is no longer available."
                        : "Could not place order. Please contact admin if your account is pending approval.";
                    TempData["Error"] = msg;
                    ViewBag.IsDistributor = true;
                    ViewBag.DistributorName = HttpContext.Session.GetString("DistributorName");
                    LoadPlantDropdown(model.PlantId);
                    ViewBag.ProductList = _repo.GetProducts();
                    return View(model);
                }
            }

            // ── ADMIN POST ────────────────────────────────────────────────
            if (!ModelState.IsValid)
            {
                ViewBag.IsDistributor = false;
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
            ViewBag.IsDistributor = false;
            LoadDistributorDropdown(model.DistributorId);
            LoadPlantDropdown(model.PlantId);
            return View(model);
        }


        // ════════════════════════════════════════════════════════════════════
        //  PLACE ORDER — quick-order posted from MyOrders page
        //  Distributor only. Same PlaceDistributorOrder logic.
        // ════════════════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SessionAuthorize("Distributor")]
        public IActionResult PlaceOrder(int productId, decimal quantity, int plantId)
        {
            if (productId <= 0 || quantity <= 0 || plantId <= 0)
            {
                TempData["Error"] = "Please select a valid product, plant, and enter a positive quantity.";
                return RedirectToAction("MyOrders");
            }

            int distributorId = HttpContext.Session.GetInt32("DistributorId") ?? 0;
            if (distributorId == 0)
            {
                TempData["Error"] = "Session expired. Please log in again.";
                return RedirectToAction("Index", "Login");
            }

            try
            {
                int orderId = _repo.PlaceDistributorOrder(distributorId, plantId, productId, quantity);
                TempData["Success"] = "Order placed! Unit price set automatically from product rate.";
                return RedirectToAction("Details", new { id = orderId });
            }
            catch (Exception ex)
            {
                string msg = ex.Message.Contains("not found or is inactive")
                    ? "Selected product is no longer available."
                    : "Could not place order. Please try again.";
                TempData["Error"] = msg;
                return RedirectToAction("MyOrders");
            }
        }


        // ════════════════════════════════════════════════════════════════════
        //  ADD ORDER DETAIL — Admin and Distributor
        //
        //  UnitPrice: AUTO-FETCHED from Production.Products.MRP for BOTH roles.
        //  Neither Admin nor Distributor types a price — the field is removed
        //  from the view entirely.
        //
        //  Merge logic (BOTH roles):
        //  If the same ProductId already exists in this order's detail rows,
        //  we ADD the new quantity to the existing row instead of inserting
        //  a duplicate line. This matches the same behaviour as
        //  PlaceDistributorOrder so there is never a duplicate product row
        //  on any order regardless of how the product was added.
        // ════════════════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SessionAuthorize("Admin", "Distributor")]
        public IActionResult AddOrderDetail(AddOrderDetailFormModel model)
        {
            string role = HttpContext.Session.GetString("RoleName") ?? "";

            // Step 1 — auto-fetch MRP (ignore any UnitPrice the form may have sent)
            var product = _repo.GetProductById(model.ProductId);
            if (product == null)
            {
                TempData["Error"] = "Selected product not found or is inactive.";
                return RedirectToAction("Details", new { id = model.OrderId });
            }
            model.UnitPrice = product.MRP;

            // Step 2 — ownership check for Distributor
            if (role == "Distributor")
            {
                var order = _repo.GetOrderById(model.OrderId);
                int myId = HttpContext.Session.GetInt32("DistributorId") ?? 0;
                if (order == null || order.DistributorId != myId)
                    return View("AccessDenied");
            }

            if (model.Quantity <= 0)
            {
                TempData["Error"] = "Quantity must be greater than zero.";
                return RedirectToAction("Details", new { id = model.OrderId });
            }

            // Step 3 — merge or insert via repository
            bool ok = _repo.AddOrMergeOrderDetail(model);
            TempData[ok ? "Success" : "Error"] = ok ? "Product added." : "Failed to add product.";
            return RedirectToAction("Details", new { id = model.OrderId });
        }


        // ════════════════════════════════════════════════════════════════════
        //  UPDATE ORDER STATUS — Admin only
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
        //  EDIT DISTRIBUTOR
        // ════════════════════════════════════════════════════════════════════
        [SessionAuthorize("Admin", "Distributor")]
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
        [SessionAuthorize("Admin", "Distributor")]
        public IActionResult EditDistributor(DistributorFormModel model)
        {
            if (!ModelState.IsValid) return View(model);
            bool ok = _repo.UpdateDistributor(model);
            if (!ok)
            {
                TempData["Error"] = "Update failed.";
                return View(model);
            }
            TempData["Success"] = "Profile updated successfully.";
            // Distributor goes back to MyOrders; Admin goes to DistributorDetails
            string role = HttpContext.Session.GetString("RoleName") ?? "";
            if (role == "Distributor")
                return RedirectToAction("MyOrders");
            return RedirectToAction("DistributorDetails", new { id = model.DistributorId });
        }


        // ════════════════════════════════════════════════════════════════════
        //  APPROVE / REJECT — Admin only
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
        //  SUSPEND / REINSTATE — Admin only
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
        //  MY PROFILE — Distributor self-edit
        //  GET /Sales/MyProfile
        //  Loads the distributor's own record by DistributorId from session
        //  and forwards to the shared EditDistributor view.
        // ════════════════════════════════════════════════════════════════════
        [SessionAuthorize("Distributor")]
        public IActionResult MyProfile()
        {
            int distId = HttpContext.Session.GetInt32("DistributorId") ?? 0;
            var dist = _repo.GetDistributorById(distId);
            if (dist == null)
            {
                TempData["Error"] = "Could not load your profile.";
                return RedirectToAction("MyOrders");
            }
            return View("EditDistributor", new DistributorFormModel
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

        // ════════════════════════════════════════════════════════════════════
        //  MY ORDERS — Distributor portal, own order list
        // ════════════════════════════════════════════════════════════════════
        [SessionAuthorize("Distributor")]
        public IActionResult MyOrders()
        {
            int distributorId = HttpContext.Session.GetInt32("DistributorId") ?? 0;
            var orders = _repo.GetOrders(distributorId, null, null, null);
            ViewBag.DistributorName = HttpContext.Session.GetString("DistributorName");
            // Pass full product list (not SelectList) so view can show MRP hints
            ViewBag.ProductList = _repo.GetProducts();
            LoadPlantDropdown();
            return View(orders);
        }


        // ════════════════════════════════════════════════════════════════════
        //  REPORTS — Admin only
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
        private void LoadDistributorDropdown(int? selected = null) =>
            ViewBag.Distributors = new SelectList(
                _repo.GetDistributors().Where(d => d.Status == "Approved").ToList(),
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
    //  DISTRIBUTOR REGISTRATION — public, no login required
    // ════════════════════════════════════════════════════════════════════════
    public class DistributorRegisterController : Controller
    {
        private readonly ISalesRepository _repo;
        public DistributorRegisterController(ISalesRepository repo) => _repo = repo;

        public IActionResult Index() => View("Register", new DistributorRegisterModel());

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

        public IActionResult Success() => View("RegisterSuccess");
    }
}