using DairyIndustry.Models;
using DairyIndustry.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DairyIndustry.Controllers
{
    public class HRController : Controller
    {
        private readonly IHRRepository _repo;
        private readonly IWebHostEnvironment _env;

        public HRController(IHRRepository repo, IWebHostEnvironment env)
        {
            _repo = repo;
            _env = env;
        }

        // ─── DASHBOARD ────────────────────────────────────────────────
        public IActionResult Dashboard()
        {
            try
            {
                var model = new HRDashboardViewModel
                {
                    Summary = _repo.GetDashboardSummary(),
                    StaffByType = _repo.GetStaffByType(),
                    RecentJoinings = _repo.GetAllStaff(null, null)
                                         .OrderByDescending(s => s.DOJ)
                                         .Take(5).ToList(),
                    RecentPayments = _repo.GetAllPayments(null)
                                         .Take(5).ToList()
                };
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load dashboard: " + ex.Message;
                return View(new HRDashboardViewModel());
            }
        }

        // ─── INDEX ────────────────────────────────────────────────────
        public IActionResult Index(int? roleId, bool? isActive)
        {
            try
            {
                var staff = _repo.GetAllStaff(roleId, isActive);
                LoadRolesDropdown(roleId);
                ViewBag.SelectedRoleId = roleId;
                ViewBag.SelectedActive = isActive;
                return View(staff);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load staff list: " + ex.Message;
                return View(new List<StaffModel>());
            }
        }

        // ─── DETAILS ──────────────────────────────────────────────────
        public IActionResult Details(int id)
        {
            try
            {
                var staff = _repo.GetStaffById(id);
                if (staff == null)
                {
                    TempData["Error"] = "Staff member not found.";
                    return RedirectToAction("Index");
                }
                return View(staff);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load staff details: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // ─── CREATE GET ───────────────────────────────────────────────
        public IActionResult Create()
        {
            LoadFormDropdowns(null);
            return View(new StaffFormModel());
        }

        // ─── CREATE POST ──────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(StaffFormModel model)
        {
            // Both Plant and Center selected — add error before IsValid check
            if (model.PlantId != null && model.CenterId != null)
                ModelState.AddModelError("PlantId",
                    "Staff can only be assigned to either a Plant or a Collection Center, not both.");

            // FIX: Remove PhotoFile from ModelState — IFormFile cannot be validated
            // by standard model validation and will cause IsValid = false
            ModelState.Remove("PhotoFile");

            if (!ModelState.IsValid)
            {
                LoadFormDropdowns(model.RoleId);
                return View(model);
            }

            try
            {
                // FIX: Read the uploaded file directly from Request.Form.Files
                // because IFormFile on a bound model is unreliable without [FromForm]
                var photoFile = Request.Form.Files["PhotoFile"];
                if (photoFile != null && photoFile.Length > 0)
                    model.ProfilePhoto = SavePhoto(photoFile);

                int newId = _repo.AddStaff(model);
                TempData["Success"] = "Staff member added successfully.";
                return RedirectToAction("Details", new { id = newId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to add staff: " + ex.Message;
                LoadFormDropdowns(model.RoleId);
                return View(model);
            }
        }

        // ─── EDIT GET ─────────────────────────────────────────────────
        public IActionResult Edit(int id)
        {
            try
            {
                var staff = _repo.GetStaffById(id);
                if (staff == null)
                {
                    TempData["Error"] = "Staff member not found.";
                    return RedirectToAction("Index");
                }

                var model = new StaffFormModel
                {
                    StaffId = staff.StaffId,
                    FirstName = staff.FirstName,
                    LastName = staff.LastName,
                    Phone = staff.Phone,
                    Email = staff.Email,
                    RoleId = staff.RoleId,
                    DOJ = staff.DOJ,
                    IsActive = staff.IsActive,
                    ProfilePhoto = staff.ProfilePhoto,
                    PlantId = staff.PlantId,
                    CenterId = staff.CenterId,
                    Salary = staff.Salary,
                    BankName = staff.BankName,
                    AccountNumber = staff.AccountNumber,
                    IFSCCode = staff.IFSCCode
                };

                LoadFormDropdowns(staff.RoleId);
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load staff for editing: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // ─── EDIT POST ────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(StaffFormModel model)
        {
            if (model.PlantId != null && model.CenterId != null)
                ModelState.AddModelError("PlantId",
                    "Staff can only be assigned to either a Plant or a Collection Center, not both.");

            // FIX: Remove PhotoFile from ModelState validation
            ModelState.Remove("PhotoFile");

            if (!ModelState.IsValid)
            {
                LoadFormDropdowns(model.RoleId);
                return View(model);
            }

            try
            {
                // FIX: Read photo from Request directly
                var photoFile = Request.Form.Files["PhotoFile"];
                if (photoFile != null && photoFile.Length > 0)
                    model.ProfilePhoto = SavePhoto(photoFile);

                _repo.UpdateStaff(model);
                TempData["Success"] = "Staff member updated successfully.";
                return RedirectToAction("Details", new { id = model.StaffId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to update staff: " + ex.Message;
                LoadFormDropdowns(model.RoleId);
                return View(model);
            }
        }

        // ─── TOGGLE ACTIVE ────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleActive(int id, bool currentStatus)
        {
            try
            {
                _repo.ToggleActive(id, !currentStatus);
                TempData["Success"] = !currentStatus
                    ? "Staff member activated." : "Staff member deactivated.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to update status: " + ex.Message;
            }
            return RedirectToAction("Details", new { id });
        }

        // ─── UPDATE PHOTO ─────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdatePhoto(int staffId, IFormFile photo)
        {
            try
            {
                if (photo == null || photo.Length == 0)
                {
                    TempData["Error"] = "Please select a photo to upload.";
                    return RedirectToAction("Details", new { id = staffId });
                }
                string photoPath = SavePhoto(photo);
                _repo.UpdateProfilePhoto(staffId, photoPath);
                TempData["Success"] = "Profile photo updated successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to update photo: " + ex.Message;
            }
            return RedirectToAction("Details", new { id = staffId });
        }

        // ─── PAYMENTS ─────────────────────────────────────────────────
        public IActionResult Payments(int? staffId, string? status)
        {
            try
            {
                var payments = staffId.HasValue
                    ? _repo.GetPaymentsByStaff(staffId.Value)
                    : _repo.GetAllPayments(status);

                ViewBag.StatusList = GetPaymentStatusList(status);
                ViewBag.SelectedStaffId = staffId;
                ViewBag.SelectedStatus = status;
                return View(payments);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load payments: " + ex.Message;
                return View(new List<StaffPaymentModel>());
            }
        }

        // ─── PRIVATE HELPERS ──────────────────────────────────────────

        private void LoadFormDropdowns(int? selectedRoleId)
        {
            ViewBag.Roles = _repo.GetRoles().Select(r => new SelectListItem
            {
                Value = r.RoleId.ToString(),
                Text = r.RoleName,
                Selected = r.RoleId == selectedRoleId
            }).ToList();

            ViewBag.Plants = new SelectList(_repo.GetPlants(), "PlantId", "DisplayText");
            ViewBag.Centers = new SelectList(_repo.GetCenters(), "CenterId", "DisplayText");
        }

        private void LoadRolesDropdown(int? selectedRoleId)
        {
            ViewBag.Roles = _repo.GetRoles().Select(r => new SelectListItem
            {
                Value = r.RoleId.ToString(),
                Text = r.RoleName,
                Selected = r.RoleId == selectedRoleId
            }).ToList();
        }

        private List<SelectListItem> GetPaymentStatusList(string? selected)
        {
            var statuses = new List<string> { "Pending", "Processed", "Failed", "Cancelled" };
            return statuses.Select(s => new SelectListItem
            {
                Value = s,
                Text = s,
                Selected = s == selected
            }).ToList();
        }

        // FIX: Use async-safe synchronous save; validate extension; create folder safely
        private string SavePhoto(IFormFile photo)
        {
            // Allowed image extensions only
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var ext = Path.GetExtension(photo.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
                throw new InvalidOperationException("Only image files (JPG, PNG, GIF, WEBP) are allowed.");

            string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "staff");
            Directory.CreateDirectory(uploadsFolder);  // safe if already exists

            string uniqueFileName = Guid.NewGuid().ToString() + ext;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // FIX: Use a proper using block with synchronous copy
            using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                photo.CopyTo(stream);
            }

            return "/uploads/staff/" + uniqueFileName;
        }
    }
}