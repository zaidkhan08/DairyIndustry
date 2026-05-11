using DairyIndustry.Filters;
using DairyIndustry.Models;
using DairyIndustry.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DairyIndustry.Controllers
{
    [SessionAuthorize("HR Manager")]
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
                var allStaff = _repo.GetAllStaff(null, null);
                int thisMonth = DateTime.Today.Month;
                int thisYear = DateTime.Today.Year;

                var model = new HRDashboardViewModel
                {
                    Summary = _repo.GetDashboardSummary(),
                    StaffByType = _repo.GetStaffByType(),

                    RecentJoinings = allStaff
                        .OrderByDescending(s => s.DOJ)
                        .Take(5)
                        .ToList(),

                    RecentPayments = _repo.GetAllPayments(null)
                        .Take(5)
                        .ToList(),

                    // Feature 4A — Work anniversaries this month
                    AnniversariesThisMonth = allStaff
                        .Where(s => s.DOJ.HasValue
                                 && s.DOJ.Value.Month == thisMonth
                                 && s.DOJ.Value.Year != thisYear)
                        .OrderBy(s => s.DOJ!.Value.Day)
                        .ToList(),

                    // Feature 4B — New joinings this month
                    NewJoiningsThisMonth = allStaff
                        .Where(s => s.DOJ.HasValue
                                 && s.DOJ.Value.Month == thisMonth
                                 && s.DOJ.Value.Year == thisYear)
                        .OrderBy(s => s.DOJ!.Value.Day)
                        .ToList()
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
            // Cannot assign to both Plant and Center
            if (model.PlantId != null && model.CenterId != null)
                ModelState.AddModelError("PlantId",
                    "Staff can only be assigned to either a Plant or a Collection Center, not both.");

            // FIX — Username and Password must both be filled or both be empty
            // Prevents partial credential entry
            bool hasUsername = !string.IsNullOrWhiteSpace(model.Username);
            bool hasPassword = !string.IsNullOrWhiteSpace(model.Password);

            if (hasUsername && !hasPassword)
                ModelState.AddModelError("Password",
                    "Password is required when a username is provided.");

            if (hasPassword && !hasUsername)
                ModelState.AddModelError("Username",
                    "Username is required when a password is provided.");

            if (hasPassword && model.Password!.Length < 6)
                ModelState.AddModelError("Password",
                    "Password must be at least 6 characters.");

            // IFormFile cannot be validated by standard model validation
            ModelState.Remove("PhotoFile");

            // Remove credential validation from ModelState —
            // they are optional fields handled manually above
            ModelState.Remove("Username");
            ModelState.Remove("Password");

            if (!ModelState.IsValid)
            {
                LoadFormDropdowns(model.RoleId);
                return View(model);
            }

            try
            {
                // Read photo directly from Request (reliable across all scenarios)
                var photoFile = Request.Form.Files["PhotoFile"];
                if (photoFile != null && photoFile.Length > 0)
                    model.ProfilePhoto = SavePhoto(photoFile);

                // FIX — AddStaff now uses usp_HR_AddStaff SP which handles
                // bank account creation and StaffId return in one transaction
                int newId = _repo.AddStaff(model);

                // ── CREATE LOGIN ACCOUNT (if credentials provided) ──
                // Uses BCrypt to hash the password before storing.
                // usp_Admin_RegisterUser already checks for duplicate username.
                if (hasUsername && hasPassword)
                {
                    string passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
                    _repo.RegisterStaffUser(model.Username!.Trim(), passwordHash, model.RoleId, newId);
                }

                TempData["Success"] = hasUsername
                    ? $"Staff member added successfully and login account created for '{model.Username}'."
                    : "Staff member added successfully.";

                return RedirectToAction("Details", new { id = newId });
            }
            catch (InvalidOperationException ex)
            {
                // Duplicate phone, email, or username surfaces here cleanly
                TempData["Error"] = ex.Message;
                LoadFormDropdowns(model.RoleId);
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to add staff: " + ex.Message;
                LoadFormDropdowns(model.RoleId);
                return View(model);
            }
        }

        // ─── EDIT GET ─────────────────────────────────────────────────
        // NOTE: Username/Password are intentionally NOT loaded here.
        // Login credentials are managed via the Admin panel — not here.
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
                    // Username and Password deliberately NOT set here
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

            ModelState.Remove("PhotoFile");
            ModelState.Remove("Username");
            ModelState.Remove("Password");

            if (!ModelState.IsValid)
            {
                LoadFormDropdowns(model.RoleId);
                return View(model);
            }

            try
            {
                var photoFile = Request.Form.Files["PhotoFile"];
                if (photoFile != null && photoFile.Length > 0)
                    model.ProfilePhoto = SavePhoto(photoFile);

                _repo.UpdateStaff(model);
                TempData["Success"] = "Staff member updated successfully.";
                return RedirectToAction("Details", new { id = model.StaffId });
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                LoadFormDropdowns(model.RoleId);
                return View(model);
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
                    ? "Staff member activated."
                    : "Staff member deactivated.";
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

        private string SavePhoto(IFormFile photo)
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var ext = Path.GetExtension(photo.FileName).ToLowerInvariant();

            if (!allowed.Contains(ext))
                throw new InvalidOperationException(
                    "Only image files (JPG, PNG, GIF, WEBP) are allowed.");

            string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "staff");
            Directory.CreateDirectory(uploadsFolder);

            string uniqueFileName = Guid.NewGuid().ToString() + ext;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                photo.CopyTo(stream);
            }

            return "/uploads/staff/" + uniqueFileName;
        }
    }
}