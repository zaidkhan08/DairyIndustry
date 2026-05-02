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

            if (role == "Distributor")
            {
                int dId = HttpContext.Session.GetInt32("DistributorId") ?? 0;
                if (dId > 0) SetNotifBadge(dId, _repo.GetOrders(dId, null, null, null));
            }

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
        //  CANCEL ORDER — Distributor self-cancel (Pending orders only)
        //  POST /Sales/CancelOrder
        // ════════════════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SessionAuthorize("Distributor")]
        public IActionResult CancelOrder(int orderId)
        {
            int myId = HttpContext.Session.GetInt32("DistributorId") ?? 0;
            var order = _repo.GetOrderById(orderId);

            if (order == null || order.DistributorId != myId)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction("MyOrders");
            }
            if (order.OrderStatus != "Pending")
            {
                TempData["Error"] = "Only Pending orders can be cancelled.";
                return RedirectToAction("Details", new { id = orderId });
            }

            bool ok = _repo.UpdateOrderStatus(orderId, "Cancelled");
            TempData[ok ? "Success" : "Error"] = ok
                ? "Order cancelled successfully."
                : "Could not cancel the order.";
            return RedirectToAction("MyOrders");
        }


        // ════════════════════════════════════════════════════════════════════
        //  REORDER — Distributor re-places all items from a past Delivered order.
        //  Loops through each line item and calls PlaceDistributorOrder for each,
        //  which applies the smart merge / today's order logic automatically.
        //  GET /Sales/Reorder/5
        // ════════════════════════════════════════════════════════════════════
        [SessionAuthorize("Distributor")]
        public IActionResult Reorder(int id)
        {
            int myId = HttpContext.Session.GetInt32("DistributorId") ?? 0;
            var order = _repo.GetOrderById(id);

            if (order == null || order.DistributorId != myId)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction("MyOrders");
            }

            var details = _repo.GetOrderDetails(id);
            if (!details.Any())
            {
                TempData["Error"] = "This order has no items to reorder.";
                return RedirectToAction("Details", new { id });
            }

            int lastOrderId = 0;
            int failCount = 0;
            foreach (var item in details)
            {
                try
                {
                    lastOrderId = _repo.PlaceDistributorOrder(
                        myId, order.PlantId, item.ProductId, item.Quantity);
                }
                catch { failCount++; }
            }

            if (failCount == details.Count)
            {
                TempData["Error"] = "Reorder failed — some products may no longer be available.";
                return RedirectToAction("MyOrders");
            }

            TempData["Success"] = failCount == 0
                ? $"Reorder placed successfully from Order #{id}."
                : $"Reorder placed with {failCount} item(s) skipped (product unavailable).";
            return RedirectToAction("Details", new { id = lastOrderId });
        }


        // ════════════════════════════════════════════════════════════════════
        //  CONFIRM RECEIPT — Distributor confirms they received a Delivered order.
        //  Changes status from Delivered → Received so admin knows it's acknowledged.
        //  POST /Sales/ConfirmReceipt
        // ════════════════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SessionAuthorize("Distributor")]
        public IActionResult ConfirmReceipt(int orderId)
        {
            int myId = HttpContext.Session.GetInt32("DistributorId") ?? 0;
            var order = _repo.GetOrderById(orderId);

            if (order == null || order.DistributorId != myId)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction("MyOrders");
            }
            if (order.OrderStatus != "Delivered")
            {
                TempData["Error"] = "Only Delivered orders can be confirmed as received.";
                return RedirectToAction("Details", new { id = orderId });
            }

            bool ok = _repo.UpdateOrderStatus(orderId, "Received");
            TempData[ok ? "Success" : "Error"] = ok
                ? "Receipt confirmed! Thank you for acknowledging delivery."
                : "Could not confirm receipt. Please try again.";
            return RedirectToAction("Details", new { id = orderId });
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
        //  PRODUCT CATALOGUE — Distributor read-only view of available products
        //  GET /Sales/ProductCatalogue
        // ════════════════════════════════════════════════════════════════════
        [SessionAuthorize("Distributor")]
        public IActionResult ProductCatalogue()
        {
            int distId = HttpContext.Session.GetInt32("DistributorId") ?? 0;
            var products = _repo.GetProducts();
            SetNotifBadge(distId, _repo.GetOrders(distId, null, null, null));
            return View(products);
        }


        // ════════════════════════════════════════════════════════════════════
        //  MY ANALYTICS — Distributor personal order analytics
        //  GET /Sales/MyAnalytics
        // ════════════════════════════════════════════════════════════════════
        [SessionAuthorize("Distributor")]
        public IActionResult MyAnalytics()
        {
            int distId = HttpContext.Session.GetInt32("DistributorId") ?? 0;
            var analytics = _repo.GetDistributorAnalytics(distId);
            SetNotifBadge(distId, _repo.GetOrders(distId, null, null, null));
            return View(analytics);
        }


        // ════════════════════════════════════════════════════════════════════
        //  EXPORT ORDERS — Download distributor order history as CSV
        //  GET /Sales/ExportOrders
        // ════════════════════════════════════════════════════════════════════
        [SessionAuthorize("Distributor")]
        public IActionResult ExportOrders(string? status, string? fromDate, string? toDate)
        {
            int distId = HttpContext.Session.GetInt32("DistributorId") ?? 0;
            string distName = HttpContext.Session.GetString("DistributorName") ?? "Distributor";

            DateTime? from = DateTime.TryParse(fromDate, out var fd) ? fd : null;
            DateTime? to = DateTime.TryParse(toDate, out var td) ? td : null;

            var orders = _repo.GetOrders(distId, string.IsNullOrEmpty(status) ? null : status,
                                         from, to);

            // Build CSV
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Order #,Date,Status,Total Amount (₹)");
            foreach (var o in orders)
            {
                sb.AppendLine(
                    $"{o.OrderId}," +
                    $"{o.OrderDate:dd-MMM-yyyy}," +
                    $"{o.OrderStatus}," +
                    $"{o.TotalAmount:N2}");
            }

            string fileName = $"Orders_{distName.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.csv";
            byte[] bytes = System.Text.Encoding.UTF8.GetPreamble()
                           .Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString()))
                           .ToArray();
            return File(bytes, "text/csv", fileName);
        }


        // ════════════════════════════════════════════════════════════════════
        //  MY DETAILS — Distributor view-only profile (registration details)
        //  GET /Sales/MyDetails
        // ════════════════════════════════════════════════════════════════════
        [SessionAuthorize("Distributor")]
        public IActionResult MyDetails()
        {
            int distId = HttpContext.Session.GetInt32("DistributorId") ?? 0;
            var dist = _repo.GetDistributorById(distId);
            if (dist == null)
            {
                TempData["Error"] = "Could not load your details.";
                return RedirectToAction("MyOrders");
            }
            SetNotifBadge(distId, _repo.GetOrders(distId, null, null, null));
            return View(dist);
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
            SetNotifBadge(distId, _repo.GetOrders(distId, null, null, null));

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
            ViewBag.ProductList = _repo.GetProducts();
            LoadPlantDropdown();
            // SetNotifBadge writes current unseen orders to session for sidebar
            // Badge only clears when distributor explicitly clicks Mark as seen (DismissNotif)
            SetNotifBadge(distributorId, orders);
            return View(orders);
        }


        // ════════════════════════════════════════════════════════════════════
        //  DISMISS NOTIF — form POST when distributor clicks "Mark as seen".
        //  Marks all current notif orders as seen in session → badge clears
        //  on next page load. Redirects back to the same page.
        //  POST /Sales/DismissNotif
        // ════════════════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SessionAuthorize("Distributor")]
        public IActionResult DismissNotif()
        {
            int distId = HttpContext.Session.GetInt32("DistributorId") ?? 0;
            if (distId > 0)
            {
                var orders = _repo.GetOrders(distId, null, null, null);
                MarkNotifSeen(distId, orders);
            }
            // Redirect back to whatever page the distributor was on
            string referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer))
                return Redirect(referer);
            return RedirectToAction("MyOrders");
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
        // ════════════════════════════════════════════════════════════════════
        //  NOTIFICATION BADGE — auto-set on every page for Distributor
        //  Counts orders with status Confirmed, Dispatched, or Delivered
        //  (i.e. admin has acted on them). Badge shows on the My Orders
        //  sidebar link so distributor always knows something needs attention.
        //  Resets to 0 when distributor opens MyOrders (all orders are seen).
        // ════════════════════════════════════════════════════════════════════
        public override void OnActionExecuting(
            Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
        {
            base.OnActionExecuting(context);
            // Session is not yet available here — SetNotifBadge is called
            // explicitly in each distributor GET action instead.
        }

        // Store notif orders as JSON in session so sidebar can read them directly.
        // Sidebar reads "NotifOrders_<distId>" from session — no ViewBag needed.
        private void SetNotifBadge(int distributorId, List<SalesOrderModel> orders)
        {
            // Read seen (OrderId+Status) composite keys from DB
            // e.g. "46_Confirmed" — so status change makes it new again
            var seenKeys = _repo.GetSeenOrderKeys(distributorId);

            var notifOrders = orders
                .Where(o => (o.OrderStatus == "Confirmed" ||
                             o.OrderStatus == "Dispatched" ||
                             o.OrderStatus == "Delivered")
                            && !seenKeys.Contains($"{o.OrderId}_{o.OrderStatus}"))
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new {
                    OrderId = o.OrderId,
                    Status = o.OrderStatus,
                    Date = o.OrderDate.ToString("dd MMM yyyy"),
                    Amount = o.TotalAmount
                })
                .ToList();

            string json = System.Text.Json.JsonSerializer.Serialize(notifOrders);
            HttpContext.Session.SetString($"NotifOrders_{distributorId}", json);
        }

        // Mark actioned orders as seen in DB with their current status.
        // Uses (OrderId, Status) so re-seeing after status change works.
        // Called from DismissNotif when distributor clicks Mark as seen.
        private void MarkNotifSeen(int distributorId, List<SalesOrderModel> orders)
        {
            var pairs = orders
                .Where(o => o.OrderStatus == "Confirmed" ||
                            o.OrderStatus == "Dispatched" ||
                            o.OrderStatus == "Delivered")
                .Select(o => (o.OrderId, o.OrderStatus));
            _repo.MarkOrdersSeen(distributorId, pairs);
            // Clear session cache so sidebar refreshes on next page load
            HttpContext.Session.Remove($"NotifOrders_{distributorId}");
        }

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