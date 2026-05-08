using DairyIndustry.Filters;
using DairyIndustry.Models.Admin;
using DairyIndustry.Models.Finance;
using DairyIndustry.Repositories;
using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Text;

namespace DairyIndustry.Controllers
{
    public class FinanceController : Controller
    {
        private readonly IFinanceRepository _financeRepo;
        private readonly IConfiguration _config;
        private readonly IConverter _pdfConverter;

        public FinanceController(IFinanceRepository financeRepo,
                                 IConfiguration config,
                                 IConverter pdfConverter)
        {
            _financeRepo = financeRepo;
            _config = config;
            _pdfConverter = pdfConverter;
        }

        // ════════════════════════════════════════════════════════
        // HELPER — returns PlantId from session only for Plant Manager
        // Admin gets null → sees everything
        // ════════════════════════════════════════════════════════
        private int? GetSessionPlantId()
        {
            string role = HttpContext.Session.GetString("RoleName");
            return role == "Plant Manager" ? HttpContext.Session.GetInt32("PlantId") : null;
        }

        // ════════════════════════════════════════════════════════
        // FARMER PAYMENTS
        // ════════════════════════════════════════════════════════

        public IActionResult Index()
        {
            string role = HttpContext.Session.GetString("RoleName");
            int? centerId = role == "Collection Agent"
                ? HttpContext.Session.GetInt32("CenterId")
                : null;  // Admin sees all

            var payments = _financeRepo.GetAllFarmerPayments(centerId);
            return View(payments);
        }

        [SessionAuthorize("Admin", "Collection Agent")]
        [HttpGet]
        public IActionResult Create()
        {
            string role = HttpContext.Session.GetString("RoleName");

            if (role == "Collection Agent")
            {
                ViewBag.IsAgent = true;
                ViewBag.CenterId = HttpContext.Session.GetInt32("CenterId");
                ViewBag.CenterName = HttpContext.Session.GetString("CenterName");
            }
            else
            {
                ViewBag.IsAgent = false;
                ViewBag.Centers = _financeRepo.GetAllCenters();
            }

            return View();
        }

        /// <summary>
        /// AJAX — returns farmers who have collections at the given center.
        /// Called when the user picks a center on the Create page.
        /// </summary>
        /// 
        [SessionAuthorize("Admin", "Collection Agent")]
        [HttpPost]
        public IActionResult Create(int farmerId, int centerId, DateTime fromDate, DateTime toDate, string paymentMode)
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;

            bool success = _financeRepo.CreateFarmerPayment(centerId, farmerId, fromDate, toDate, DateTime.Today, userId) > 0;

            if (!success)
            {
                TempData["Error"] = "Payment creation failed. No unpaid collections found.";
                return RedirectToAction("Create");
            }

            TempData["Success"] = "Farmer payment created successfully.";
            return RedirectToAction("Index");
        }


        [HttpPost]
        public IActionResult CancelFarmerPayment(int paymentId)
        {
            try
            {
                _financeRepo.CancelFarmerPayment(paymentId);
                TempData["Success"] = $"Payment FP-{paymentId} has been cancelled.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction("Index");
        }



        [HttpGet]
        public IActionResult GetFarmersByCenter(int centerId)
        {
            var farmers = _financeRepo.GetFarmersByCenter(centerId);
            return Json(farmers.Select(f => new
            {
                farmerId = f.FarmerId,
                farmerName = f.FarmerName,
                farmerCode = f.FarmerCode
            }));
        }

        [HttpPost]
        public IActionResult ReactivateFarmerPayment(int paymentId)
        {
            try
            {
                _financeRepo.ReactivateFarmerPayment(paymentId);
                TempData["Success"] = $"Payment FP-{paymentId} reactivated to Pending.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction("Index");
        }

        /// <summary>
        /// AJAX — looks up a farmer by FarmerCode and validates they belong to the chosen center.
        /// </summary>
        [HttpGet]
        public IActionResult GetFarmerByCode(string farmerCode, int centerId)
        {
            var farmer = _financeRepo.GetFarmerByCode(farmerCode);
            if (farmer == null)
                return Json(new { success = false, message = "No farmer found with this code." });

            // Confirm this farmer actually has collections at the selected center
            var farmers = _financeRepo.GetFarmersByCenter(centerId);
            bool belongsToCenter = farmers.Any(f => f.FarmerId == farmer.FarmerId);
            if (!belongsToCenter)
                return Json(new { success = false, message = "This farmer has no collections at the selected center." });

            return Json(new
            {
                success = true,
                farmerId = farmer.FarmerId,
                farmerName = farmer.FarmerName,
                farmerCode = farmer.FarmerCode
            });
        }

        [SessionAuthorize("Admin", "Collection Agent")]
        [HttpPost]
        public IActionResult Preview(int farmerId, int centerId, DateTime fromDate, DateTime toDate)
        {
            bool hasBankAccount = _financeRepo.FarmerHasBankAccount(farmerId);
            if (!hasBankAccount)
                return Json(new { success = false, message = "This farmer has no bank account linked. Please add bank details before processing payment." });

            var preview = _financeRepo.PreviewFarmerPayment(farmerId, centerId, fromDate, toDate);
            if (preview == null || preview.TotalAmount == 0)
                return Json(new { success = false, message = "No unpaid collections found for this farmer in the selected date range." });

            string paidByName = HttpContext.Session.GetString("FullName")
                             ?? HttpContext.Session.GetString("UserName")
                             ?? "Unknown";

            return Json(new
            {
                success = true,
                farmerName = preview.FarmerName,
                centerName = preview.CenterName,
                totalQty = preview.TotalQty,
                totalAmount = preview.TotalAmount,
                unpaidCollections = preview.UnpaidCollections,
                paidBy = paidByName
            });
        }


        [HttpGet]
        [Route("Finance/Detail/{id}")]
        public IActionResult Detail(int id)
        {
            var payment = _financeRepo.GetFarmerPaymentById(id);
            if (payment == null) return NotFound();
            ViewBag.PublishableKey = _config["Stripe:PublishableKey"];
            return View(payment);
        }

        [HttpPost]
        public IActionResult CreateCheckoutSession(int paymentId, decimal totalAmount, string farmerName)
        {
            StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];
            var domain = $"{Request.Scheme}://{Request.Host}";

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency   = "inr",
                            UnitAmount = (long)(totalAmount * 100),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name        = $"Farmer Payment - {farmerName}",
                                Description = $"Payment ID: {paymentId}"
                            }
                        },
                        Quantity = 1
                    }
                },
                Mode = "payment",
                SuccessUrl = $"{domain}/Finance/PaymentSuccess?paymentId={paymentId}&session_id={{CHECKOUT_SESSION_ID}}",
                CancelUrl = $"{domain}/Finance/PaymentCancelled?paymentId={paymentId}",
                Metadata = new Dictionary<string, string>
                {
                    { "paymentId",   paymentId.ToString() },
                    { "paymentType", "Farmer" }
                }
            };

            var service = new SessionService();
            Session session = service.Create(options);
            return Redirect(session.Url);
        }

        [HttpGet]
        public IActionResult PaymentSuccess(int paymentId, string session_id)
        {
            StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];
            var service = new SessionService();
            Session session = service.Get(session_id);
            string bankStatus = session.PaymentStatus == "paid" ? "Success" : "Failed";
            _financeRepo.RecordPaymentTransaction("Farmer", paymentId, bankStatus, session_id);
            TempData["Success"] = bankStatus == "Success"
                ? $"Payment processed successfully! Transaction: {session_id}"
                : $"Payment failed. Transaction: {session_id}";
            return RedirectToAction("Detail", new { id = paymentId });
        }

        [HttpGet]
        public IActionResult PaymentCancelled(int paymentId)
        {
            TempData["Error"] = "Payment was cancelled. You can try again from the payment detail page.";
            return RedirectToAction("Detail", new { id = paymentId });
        }

        [HttpGet]
        [Route("Finance/ViewReceipt/{id}")]
        public IActionResult ViewReceipt(int id)
        {
            var payment = _financeRepo.GetFarmerPaymentById(id);
            if (payment == null) return NotFound();
            string html = BuildReceiptHtml(payment);
            return Content(html, "text/html");
        }

        [HttpGet]
        [Route("Finance/DownloadReceipt/{id}")]
        public IActionResult DownloadReceipt(int id)
        {
            var payment = _financeRepo.GetFarmerPaymentById(id);
            if (payment == null) return NotFound();
            string html = BuildReceiptHtml(payment);
            byte[] pdfBytes = GeneratePdfFromHtml(html);
            string fileName = $"FarmerPayment_FP{payment.PaymentId}_{payment.FarmerName.Replace(" ", "_")}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        [HttpGet]
        public IActionResult GetFarmerCenter(int farmerId)
        {
            var center = _financeRepo.GetFarmerDefaultCenter(farmerId);
            if (center == null)
                return Json(new { success = false });

            return Json(new
            {
                success = true,
                centerId = center.CenterId,
                centerName = center.CenterName
            });
        }

        // ════════════════════════════════════════════════════════
        // CENTER PAYMENTS
        // ════════════════════════════════════════════════════════

        // INDEX — Admin sees all, Plant Manager sees their plant only
        public IActionResult CenterPayments()
        {
            var payments = _financeRepo.GetAllCenterPayments(GetSessionPlantId());
            return View(payments);
        }

        // CREATE GET — dropdown filtered by plant for Plant Manager
        [SessionAuthorize("Plant Manager", "Admin")]
        [HttpGet]
        public IActionResult CreateCenterPayment()
        {
            var transfers = _financeRepo.GetEligibleTransfers(GetSessionPlantId());
            ViewBag.Transfers = transfers;
            return View();
        }

        // AUTO-FILL AJAX — scoped to session plant so no cross-plant spoofing
        [HttpGet]
        public IActionResult GetTransferDetails(int transferId)
        {
            var transfers = _financeRepo.GetEligibleTransfers(GetSessionPlantId());
            var t = transfers.FirstOrDefault(x => x.TransferId == transferId);
            if (t == null)
                return Json(new { success = false, message = "Transfer not found." });

            decimal baseRate = 0m;
            if (t.MilkTypeId.HasValue && t.TestedFat.HasValue && t.TestedCLR.HasValue)
                baseRate = _financeRepo.GetActiveRate(t.MilkTypeId.Value, t.TestedFat.Value, t.TestedCLR.Value);

            // +₹3/L center bonus applied on top of the base rate chart price
            decimal suggestedRate = baseRate > 0 ? baseRate + 3m : 0m;
            decimal total = t.ReceivedQty * suggestedRate;

            return Json(new
            {
                success = true,
                transferId = t.TransferId,
                centerId = t.CenterId,
                plantId = t.PlantId,
                receivedQty = t.ReceivedQty,
                testedFat = t.TestedFat,
                testedCLR = t.TestedCLR,
                baseRate = baseRate,
                ratePerLiter = suggestedRate,
                totalAmount = total,
                hasRate = suggestedRate > 0,
                hasCancelledPayment = t.HasCancelledPayment,
                cancelledPaymentId = t.CancelledPaymentId
            });
        }

        // CREATE POST
        [HttpPost]
        public IActionResult CreateCenterPayment(int transferId, decimal ratePerLiter, bool hasCancelledPayment, int? cancelledPaymentId)
        {
            try
            {
                int cpId;
                if (hasCancelledPayment && cancelledPaymentId.HasValue)
                {
                    // Cancelled row already exists — UPDATE it back to Pending (no duplicate insert)
                    cpId = _financeRepo.ReactivateCenterPayment(cancelledPaymentId.Value, ratePerLiter, DateTime.Today);
                    TempData["Success"] = $"Payment CP-{cpId:D4} reactivated. Proceed to pay via Stripe.";
                }
                else
                {
                    // Brand new — INSERT
                    cpId = _financeRepo.CreateCenterPayment(transferId, ratePerLiter, DateTime.Today);
                    TempData["Success"] = "Center payment record created. Proceed to pay via Stripe.";
                }
                return RedirectToAction("CenterPaymentDetail", new { id = cpId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("CreateCenterPayment");
            }
        }

        // DETAIL
        [HttpGet]
        [Route("Finance/CenterPaymentDetail/{id}")]
        public IActionResult CenterPaymentDetail(int id)
        {
            var payment = _financeRepo.GetCenterPaymentById(id);
            if (payment == null) return NotFound();
            ViewBag.PublishableKey = _config["Stripe:PublishableKey"];
            return View(payment);
        }

        // STRIPE CHECKOUT for Center Payment
        [HttpPost]
        public IActionResult CreateCenterCheckoutSession(int centerPaymentId, decimal totalAmount, string centerName)
        {
            StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];
            var domain = $"{Request.Scheme}://{Request.Host}";

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency   = "inr",
                            UnitAmount = (long)(totalAmount * 100),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name        = $"Center Payment - {centerName}",
                                Description = $"Center Payment ID: {centerPaymentId}"
                            }
                        },
                        Quantity = 1
                    }
                },
                Mode = "payment",
                SuccessUrl = $"{domain}/Finance/CenterPaymentSuccess?centerPaymentId={centerPaymentId}&session_id={{CHECKOUT_SESSION_ID}}",
                CancelUrl = $"{domain}/Finance/CenterPaymentCancelled?centerPaymentId={centerPaymentId}",
                Metadata = new Dictionary<string, string>
                {
                    { "paymentId",   centerPaymentId.ToString() },
                    { "paymentType", "Center" }
                }
            };

            var service = new SessionService();
            Session session = service.Create(options);
            return Redirect(session.Url);
        }

        // STRIPE SUCCESS for Center Payment
        [HttpGet]
        public IActionResult CenterPaymentSuccess(int centerPaymentId, string session_id)
        {
            StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];
            var service = new SessionService();
            Session session = service.Get(session_id);
            string bankStatus = session.PaymentStatus == "paid" ? "Success" : "Failed";
            _financeRepo.RecordPaymentTransaction("Center", centerPaymentId, bankStatus, session_id);
            TempData["Success"] = bankStatus == "Success"
                ? $"Center payment processed successfully! Transaction: {session_id}"
                : $"Payment failed. Transaction: {session_id}";
            return RedirectToAction("CenterPaymentDetail", new { id = centerPaymentId });
        }

        // STRIPE CANCELLED for Center Payment
        [HttpGet]
        public IActionResult CenterPaymentCancelled(int centerPaymentId)
        {
            TempData["Error"] = "Payment was cancelled. You can try again from the payment detail page.";
            return RedirectToAction("CenterPaymentDetail", new { id = centerPaymentId });
        }

        // ════════════════════════════════════════════════════════
        // RECEIPT HELPERS
        // ════════════════════════════════════════════════════════

        private string BuildReceiptHtml(FarmerPaymentModel p)
        {
            string statusColor = p.PaymentStatus == "Processed" ? "#dcfce7" : p.PaymentStatus == "Pending" ? "#fef9c3" : "#fee2e2";
            string statusText = p.PaymentStatus == "Processed" ? "#166534" : p.PaymentStatus == "Pending" ? "#854d0e" : "#991b1b";
            string statusBorder = p.PaymentStatus == "Processed" ? "#86efac" : p.PaymentStatus == "Pending" ? "#fde047" : "#fca5a5";
            string bankDetails = p.BankName != null ? $"{p.BankName}" : "Not linked";
            string accountNo = p.AccountNumber ?? "N/A";
            string ifsc = p.IFSCCode ?? "N/A";
            string transRef = p.TransactionReference ?? "N/A";
            string bankSt = p.BankStatus ?? "N/A";
            string bankStColor = p.BankStatus == "Success" ? "#166534" : "#991b1b";
            string pid = p.PaymentId.ToString("D6");
            string generated = DateTime.Now.ToString("dd MMM yyyy, hh:mm tt");
            string today = DateTime.Now.ToString("dd MMM yyyy");
            string fromDate = p.FromDate.ToString("dd MMM yyyy");
            string toDate = p.ToDate.ToString("dd MMM yyyy");
            string payDate = p.PaymentDate.ToString("dd MMM yyyy");
            string qty = p.TotalQty.ToString("N2");
            string amt = p.TotalAmount.ToString("N2");

            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html><html><head><meta charset='utf-8'/><style>");
            sb.Append("*{margin:0;padding:0;box-sizing:border-box;}");
            sb.Append("body{font-family:'Segoe UI',Arial,sans-serif;background:#f0f0f0;padding:24px;color:#111;}");
            sb.Append(".page{background:#fff;max-width:720px;margin:0 auto;padding:28px 32px;font-size:12px;}");
            sb.Append(".top-header{display:flex;justify-content:space-between;align-items:flex-start;border-bottom:2px solid #1e3a5f;padding-bottom:10px;margin-bottom:10px;}");
            sb.Append(".co-name{font-size:18px;font-weight:700;color:#1e3a5f;letter-spacing:0.5px;}");
            sb.Append(".co-sub{font-size:10px;color:#555;margin-top:3px;}");
            sb.Append(".rt{text-align:right;}");
            sb.Append(".rt h2{font-size:15px;font-weight:700;color:#1e3a5f;border:1px solid #1e3a5f;padding:4px 10px;display:inline-block;}");
            sb.Append(".rt p{font-size:10px;color:#555;margin-top:4px;}");
            sb.Append($".status-bar{{text-align:center;padding:5px;font-size:11px;font-weight:700;letter-spacing:1px;margin-bottom:10px;border-radius:3px;background:{statusColor};color:{statusText};border:0.5px solid {statusBorder};}}");
            sb.Append(".meta-row{display:flex;justify-content:space-between;margin-bottom:10px;gap:12px;}");
            sb.Append(".meta-box{flex:1;border:0.5px solid #ccc;border-radius:4px;padding:8px 10px;}");
            sb.Append(".meta-box .lbl{font-size:9px;text-transform:uppercase;color:#777;letter-spacing:0.5px;font-weight:600;margin-bottom:4px;}");
            sb.Append(".meta-box .val{font-size:11px;color:#111;line-height:1.6;}");
            sb.Append("table{width:100%;border-collapse:collapse;margin-bottom:10px;}");
            sb.Append("table th{background:#1e3a5f;color:#fff;font-size:10px;padding:6px 8px;text-align:left;font-weight:600;}");
            sb.Append("table th.r,table td.r{text-align:right;}");
            sb.Append("table td{font-size:11px;padding:5px 8px;border-bottom:0.5px solid #e5e7eb;color:#111;}");
            sb.Append("table tr:nth-child(even) td{background:#f9fafb;}");
            sb.Append(".totals-section{display:flex;justify-content:flex-end;margin-bottom:10px;}");
            sb.Append(".totals-table{width:260px;font-size:11px;}");
            sb.Append(".totals-table td{padding:4px 8px;border-bottom:0.5px solid #e5e7eb;}");
            sb.Append(".grand td{background:#1e3a5f !important;color:#fff !important;font-weight:700;font-size:13px;-webkit-print-color-adjust:exact;}");
            sb.Append(".bottom-row{display:flex;gap:12px;margin-top:10px;}");
            sb.Append(".bank-box{flex:1;border:0.5px solid #ccc;border-radius:4px;padding:8px 10px;}");
            sb.Append(".bank-box .lbl{font-size:9px;text-transform:uppercase;color:#777;letter-spacing:0.5px;font-weight:600;margin-bottom:5px;}");
            sb.Append(".bank-box table{margin:0;} .bank-box table td{border:none;padding:2px 0;font-size:11px;} .bank-box table td:first-child{color:#777;width:110px;}");
            sb.Append(".sig-box{width:160px;border:0.5px solid #ccc;border-radius:4px;padding:8px 10px;text-align:center;display:flex;flex-direction:column;justify-content:space-between;}");
            sb.Append(".sig-line{border-top:1px solid #aaa;margin-top:40px;padding-top:4px;font-size:10px;color:#555;}");
            sb.Append(".footer{border-top:1px solid #ccc;margin-top:12px;padding-top:8px;text-align:center;font-size:9px;color:#888;}");
            sb.Append("</style></head><body><div class='page'>");

            sb.Append("<div class='top-header'>");
            sb.Append("<div><div class='co-name'>Dairy Management System</div>");
            sb.Append("<div class='co-sub'>Farmer Milk Payment Receipt</div>");
            sb.Append("<div class='co-sub'>Payment Type: Direct Bank Transfer via Stripe</div></div>");
            sb.Append($"<div class='rt'><h2>PAYMENT RECEIPT</h2><p>Receipt No: FP-{pid}</p><p>Date: {today}</p></div>");
            sb.Append("</div>");

            sb.Append($"<div class='status-bar'>PAYMENT STATUS: {p.PaymentStatus.ToUpper()}</div>");

            sb.Append("<div class='meta-row'>");
            sb.Append($"<div class='meta-box'><div class='lbl'>Farmer Details</div><div class='val'><strong>{p.FarmerName}</strong><br/>Bank: {bankDetails}<br/>A/C: {accountNo} | IFSC: {ifsc}</div></div>");
            sb.Append($"<div class='meta-box'><div class='lbl'>Collection Center</div><div class='val'><strong>{p.CenterName}</strong><br/>Collection Period:<br/>{fromDate} → {toDate}</div></div>");
            sb.Append($"<div class='meta-box'><div class='lbl'>Payment Info</div><div class='val'>Payment Date: <strong>{payDate}</strong><br/>Method: Stripe<br/>Currency: INR<br/>Bank Status: <strong style='color:{bankStColor}'>{bankSt}</strong></div></div>");
            sb.Append("</div>");

            sb.Append("<table><thead><tr>");
            sb.Append("<th style='width:36px'>S.No</th><th>Description</th><th>Collection Period</th>");
            sb.Append("<th class='r'>Total Qty (L)</th><th class='r'>Amount (₹)</th>");
            sb.Append("</tr></thead><tbody>");
            sb.Append($"<tr><td>1</td><td>Milk Collection Payment</td><td>{fromDate} – {toDate}</td><td class='r'>{qty}</td><td class='r'>{amt}</td></tr>");
            sb.Append("</tbody></table>");

            sb.Append("<div class='totals-section'><table class='totals-table'>");
            sb.Append($"<tr><td style='color:#777'>Total Quantity</td><td class='r'>{qty} L</td></tr>");
            sb.Append($"<tr><td style='color:#777'>Subtotal</td><td class='r'>₹ {amt}</td></tr>");
            sb.Append($"<tr><td style='color:#777'>Deductions</td><td class='r'>₹ 0.00</td></tr>");
            sb.Append($"<tr class='grand'><td>Total Amount Paid</td><td class='r'>₹ {amt}</td></tr>");
            sb.Append("</table></div>");

            sb.Append("<div class='bottom-row'>");
            sb.Append("<div class='bank-box'><div class='lbl'>Bank &amp; Transaction Details</div><table>");
            sb.Append($"<tr><td>Bank Name</td><td>{bankDetails}</td></tr>");
            sb.Append($"<tr><td>Account No</td><td>{accountNo}</td></tr>");
            sb.Append($"<tr><td>IFSC Code</td><td>{ifsc}</td></tr>");
            sb.Append($"<tr><td>Transaction Ref</td><td style='color:#1e3a5f;font-weight:600'>{transRef}</td></tr>");
            sb.Append("<tr><td>Payment Via</td><td>Stripe</td></tr>");
            sb.Append("</table></div>");
            sb.Append("<div class='sig-box'>");
            sb.Append("<div style='font-size:10px;color:#777;text-align:left'>For Dairy Management System</div>");
            sb.Append("<div><div class='sig-line'>Authorised Signatory</div>");
            sb.Append("<div style='font-size:9px;color:#777;margin-top:3px;'>Dairy Management System</div></div>");
            sb.Append("</div></div>");

            sb.Append($"<div class='footer'>This is a computer-generated receipt and does not require a physical signature. | Receipt No: FP-{pid} | Generated: {generated} | Dairy Management System</div>");
            sb.Append("</div></body></html>");
            return sb.ToString();
        }

        private byte[] GeneratePdfFromHtml(string html)
        {
            var doc = new HtmlToPdfDocument
            {
                GlobalSettings = new GlobalSettings
                {
                    ColorMode = ColorMode.Color,
                    Orientation = Orientation.Portrait,
                    PaperSize = PaperKind.A4,
                    Margins = new MarginSettings { Top = 0, Bottom = 0, Left = 0, Right = 0 }
                },
                Objects =
                {
                    new ObjectSettings
                    {
                        PagesCount  = true,
                        HtmlContent = html,
                        WebSettings = { DefaultEncoding = "utf-8" }
                    }
                }
            };

            return _pdfConverter.Convert(doc);
        }


        // REACTIVATE — Re-pay a cancelled payment (updates existing row, no duplicate)
        [HttpPost]
        public IActionResult ReactivateCenterPayment(int centerPaymentId, decimal ratePerLiter)
        {
            try
            {
                int cpId = _financeRepo.ReactivateCenterPayment(centerPaymentId, ratePerLiter, DateTime.Today);
                TempData["Success"] = $"Payment CP-{cpId:D4} reactivated. Proceed to pay via Stripe.";
                return RedirectToAction("CenterPaymentDetail", new { id = cpId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("CenterPayments");
            }
        }

        [HttpPost]
        public IActionResult CancelCenterPayment(int centerPaymentId)
        {
            try
            {
                _financeRepo.CancelCenterPayment(centerPaymentId);
                TempData["Success"] = $"Payment CP-{centerPaymentId:D4} has been cancelled.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction("CenterPayments");
        }


        // ════════════════════════════════════════════════════════
        // CENTER WALLET — full page
        // ════════════════════════════════════════════════════════

        [SessionAuthorize("Admin", "Collection Agent")]
        public IActionResult CenterWallet()
        {
            // Collection Agent → scoped to their own center from session
            // Admin            → null → sees all centers aggregated
            int? centerId = null;
            string role = HttpContext.Session.GetString("RoleName");
            if (role == "Collection Agent")
                centerId = HttpContext.Session.GetInt32("CenterId");

            var vm = _financeRepo.GetCenterWallet(centerId);
            return View(vm);
        }

        // ════════════════════════════════════════════════════════
        // CENTER WALLET PARTIAL — loaded via AJAX from Index page
        // ════════════════════════════════════════════════════════

        [SessionAuthorize("Admin", "Collection Agent")]
        [HttpGet]
        public IActionResult CenterWalletPartial()
        {
            int? centerId = null;
            string role = HttpContext.Session.GetString("RoleName");
            if (role == "Collection Agent")
                centerId = HttpContext.Session.GetInt32("CenterId");

            var vm = _financeRepo.GetCenterWallet(centerId);
            return PartialView("_CenterWalletPartial", vm);
        }

        
    }
}