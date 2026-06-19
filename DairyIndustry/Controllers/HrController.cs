using DairyIndustry.Filters;
using DairyIndustry.Models;
using DairyIndustry.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DairyIndustry.Controllers
{
    // ═══════════════════════════════════════════════════════════════
    //  SESSION GUARD — Both HR Manager and Plant Manager can access
    //  this controller. Every action internally checks the role and
    //  applies the appropriate restrictions.
    //  Collection Agent will be added here once their session is fixed.
    // ═══════════════════════════════════════════════════════════════
    [SessionAuthorize("HR Manager", "Plant Manager")]
    public class HRController : Controller
    {
        private readonly IHRRepository _repo;
        private readonly IWebHostEnvironment _env;

        // ── Pagination — 5 entries per page ────────────────────────
        private const int PageSize = 5;

        public HRController(IHRRepository repo, IWebHostEnvironment env)
        {
            _repo = repo;
            _env = env;
        }

        // ═══════════════════════════════════════════════════════════
        //  PRIVATE HELPERS
        // ═══════════════════════════════════════════════════════════

        // Returns the session role name
        private string SessionRole =>
            HttpContext.Session.GetString("RoleName") ?? string.Empty;

        // Returns true if the logged-in user is an HR Manager
        private bool IsHRManager =>
            string.Equals(SessionRole, "HR Manager", StringComparison.OrdinalIgnoreCase);

        // Returns true if the logged-in user is a Plant Manager
        private bool IsPlantManager =>
            string.Equals(SessionRole, "Plant Manager", StringComparison.OrdinalIgnoreCase);

        // Returns the PlantId from session for Plant Manager.
        // Returns null if not a Plant Manager or not assigned.
        private int? SessionPlantId =>
            IsPlantManager ? HttpContext.Session.GetInt32("PlantId") : null;

        // ── Guard: blocks Plant Manager from HR-only actions ──────────
        // Returns a redirect result if the current user is NOT an HR Manager.
        // Usage: var guard = HROnly(); if (guard != null) return guard;
        private IActionResult? HROnly()
        {
            if (!IsHRManager)
            {
                TempData["Error"] = "Access denied. This action is only available to HR Managers.";
                return RedirectToAction("Index");
            }
            return null;
        }

        // ── Guard: ensures Plant Manager can only access their own plant's staff ──
        // Returns a redirect if the staff member does not belong to the
        // Plant Manager's plant. HR Manager always passes through.
        private IActionResult? PlantOwnershipGuard(StaffModel staff)
        {
            if (IsHRManager) return null;  // HR sees everything

            int? plantId = SessionPlantId;
            if (plantId == null)
            {
                TempData["Error"] = "Your account is not assigned to any plant. Contact HR.";
                return RedirectToAction("Index");
            }

            if (staff.PlantId != plantId)
            {
                TempData["Error"] = "Access denied. This staff member does not belong to your plant.";
                return RedirectToAction("Index");
            }

            return null;
        }

        // ─── DASHBOARD — HR Manager only ──────────────────────────────
        // Plant Manager is redirected to Index (their staff list)
        public IActionResult Dashboard()
        {
            // Plant Manager has no dashboard — redirect to their staff list
            if (IsPlantManager)
                return RedirectToAction("Index");

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
                        .Take(5).ToList(),

                    RecentPayments = _repo.GetAllPayments(null)
                        .Take(5).ToList(),

                    AnniversariesThisMonth = allStaff
                        .Where(s => s.DOJ.HasValue
                                 && s.DOJ.Value.Month == thisMonth
                                 && s.DOJ.Value.Year != thisYear)
                        .OrderBy(s => s.DOJ!.Value.Day).ToList(),

                    NewJoiningsThisMonth = allStaff
                        .Where(s => s.DOJ.HasValue
                                 && s.DOJ.Value.Month == thisMonth
                                 && s.DOJ.Value.Year == thisYear)
                        .OrderBy(s => s.DOJ!.Value.Day).ToList(),

                    InactiveStaffList = allStaff
                        .Where(s => !s.IsActive)
                        .OrderBy(s => s.FullName).ToList()
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
        // HR Manager  → sees ALL staff, can filter by role and status
        // Plant Manager → sees ONLY their plant's staff
        //                 Role/status filter still works within their plant
        //                 PlantId filter is forced from session — cannot be
        //                 overridden via URL params
        public IActionResult Index(int? roleId, bool? isActive, int page = 1)
        {
            try
            {
                List<StaffModel> allStaff;

                if (IsPlantManager)
                {
                    int? plantId = SessionPlantId;

                    if (plantId == null)
                    {
                        TempData["Error"] = "Your account is not assigned to any plant. Contact HR.";
                        return View(new List<StaffModel>());
                    }

                    // Get only this plant's staff — PlantId is forced from session
                    allStaff = _repo.GetStaffByPlant(plantId.Value, roleId, isActive);
                    ViewBag.IsPlantManager = true;
                    ViewBag.PlantName = HttpContext.Session.GetString("PlantName") ?? "Your Plant";
                }
                else
                {
                    // HR Manager — full access
                    allStaff = _repo.GetAllStaff(roleId, isActive);
                    ViewBag.IsPlantManager = false;
                }

                // ── Pagination ────────────────────────────────────────
                int totalRecords = allStaff.Count;
                int totalPages = (int)Math.Ceiling(totalRecords / (double)PageSize);
                if (page < 1) page = 1;
                if (page > totalPages && totalPages > 0) page = totalPages;

                var pagedStaff = allStaff
                    .Skip((page - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalRecords = totalRecords;
                ViewBag.PageSize = PageSize;
                ViewBag.RoleId = roleId;
                ViewBag.IsActive = isActive;
                // ── End Pagination ────────────────────────────────────

                LoadRolesDropdown(roleId);
                ViewBag.SelectedRoleId = roleId;
                ViewBag.SelectedActive = isActive;

                return View(pagedStaff);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load staff list: " + ex.Message;
                return View(new List<StaffModel>());
            }
        }

        // ─── DETAILS ──────────────────────────────────────────────────
        // HR Manager  → can view any staff member
        // Plant Manager → can only view staff from their plant
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

                // Ownership guard — Plant Manager cannot view other plants' staff
                var guard = PlantOwnershipGuard(staff);
                if (guard != null) return guard;

                ViewBag.SalaryHistory = _repo.GetSalaryHistory(id);
                ViewBag.StaffNotes = _repo.GetStaffNotes(id);
                ViewBag.IsPlantManager = IsPlantManager;

                return View(staff);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load staff details: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // ─── CREATE GET — HR Manager only ─────────────────────────────
        public IActionResult Create()
        {
            var guard = HROnly(); if (guard != null) return guard;
            LoadFormDropdowns(null);
            return View(new StaffFormModel());
        }

        // ─── CREATE POST — HR Manager only ────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(StaffFormModel model)
        {
            var guard = HROnly(); if (guard != null) return guard;

            if (model.PlantId != null && model.CenterId != null)
                ModelState.AddModelError("PlantId",
                    "Staff can only be assigned to either a Plant or a Collection Center, not both.");

            bool hasUsername = !string.IsNullOrWhiteSpace(model.Username);
            bool hasPassword = !string.IsNullOrWhiteSpace(model.Password);

            if (hasUsername && !hasPassword)
                ModelState.AddModelError("Password", "Password is required when a username is provided.");
            if (hasPassword && !hasUsername)
                ModelState.AddModelError("Username", "Username is required when a password is provided.");
            if (hasPassword && model.Password!.Length < 6)
                ModelState.AddModelError("Password", "Password must be at least 6 characters.");

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

                int newId = _repo.AddStaff(model);

                if (hasUsername && hasPassword)
                {
                    string passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
                    _repo.RegisterStaffUser(model.Username!.Trim(), passwordHash, model.RoleId, newId);
                }

                TempData["Success"] = hasUsername
                    ? $"Staff member added and login account created for '{model.Username}'."
                    : "Staff member added successfully.";

                return RedirectToAction("Details", new { id = newId });
            }
            catch (InvalidOperationException ex)
            {
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
        // HR Manager  → can edit any staff
        // Plant Manager → can only edit staff from their plant
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

                // Ownership guard — Plant Manager cannot edit other plants' staff
                var guard = PlantOwnershipGuard(staff);
                if (guard != null) return guard;

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
                    CurrentSalary = staff.Salary,
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
        // Plant Manager ownership is re-verified on POST to prevent
        // URL tampering — cannot edit staff outside their plant
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(StaffFormModel model)
        {
            // Re-verify ownership on POST
            if (IsPlantManager)
            {
                var existingStaff = _repo.GetStaffById(model.StaffId);
                if (existingStaff == null)
                {
                    TempData["Error"] = "Staff member not found.";
                    return RedirectToAction("Index");
                }
                var ownerGuard = PlantOwnershipGuard(existingStaff);
                if (ownerGuard != null) return ownerGuard;
            }

            if (model.PlantId != null && model.CenterId != null)
                ModelState.AddModelError("PlantId",
                    "Staff can only be assigned to either a Plant or a Collection Center, not both.");

            ModelState.Remove("PhotoFile");
            ModelState.Remove("Username");
            ModelState.Remove("Password");
            ModelState.Remove("SalaryChangeReason");

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

                bool salaryChanged = model.Salary.HasValue
                    && model.Salary != model.CurrentSalary;

                _repo.UpdateStaff(model);

                if (salaryChanged)
                {
                    try
                    {
                        string? changedBy = HttpContext.Session.GetString("Username");
                        _repo.AddSalaryHistory(
                            model.StaffId,
                            model.CurrentSalary,
                            model.Salary!.Value,
                            string.IsNullOrWhiteSpace(model.SalaryChangeReason)
                                ? null : model.SalaryChangeReason.Trim(),
                            changedBy);
                    }
                    catch { /* history failure never blocks the main save */ }
                }

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
        // Plant Manager can toggle their own plant's staff active status
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleActive(int id, bool currentStatus)
        {
            try
            {
                // Ownership check for Plant Manager
                if (IsPlantManager)
                {
                    var staff = _repo.GetStaffById(id);
                    if (staff != null)
                    {
                        var ownerGuard = PlantOwnershipGuard(staff);
                        if (ownerGuard != null) return ownerGuard;
                    }
                }

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
                // Ownership check for Plant Manager
                if (IsPlantManager)
                {
                    var staff = _repo.GetStaffById(staffId);
                    if (staff != null)
                    {
                        var ownerGuard = PlantOwnershipGuard(staff);
                        if (ownerGuard != null) return ownerGuard;
                    }
                }

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

        // ─── ADD NOTE — HR Manager only ───────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddNote(int staffId, string noteText, string noteType)
        {
            var guard = HROnly(); if (guard != null) return guard;

            if (string.IsNullOrWhiteSpace(noteText))
            {
                TempData["Error"] = "Note text cannot be empty.";
                return RedirectToAction("Details", new { id = staffId });
            }

            var allowedTypes = new[] { "General", "Warning", "Appreciation", "Feedback", "Observation" };
            if (!allowedTypes.Contains(noteType)) noteType = "General";

            try
            {
                string? createdBy = HttpContext.Session.GetString("Username");
                _repo.AddStaffNote(staffId, noteText, noteType, createdBy);
                TempData["Success"] = "Note added successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to add note: " + ex.Message;
            }
            return RedirectToAction("Details", new { id = staffId });
        }

        // ─── DELETE NOTE — HR Manager only ────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteNote(int noteId, int staffId)
        {
            var guard = HROnly(); if (guard != null) return guard;

            try
            {
                bool deleted = _repo.DeleteStaffNote(noteId, staffId);
                TempData[deleted ? "Success" : "Error"] = deleted
                    ? "Note deleted successfully."
                    : "Note not found or already deleted.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to delete note: " + ex.Message;
            }
            return RedirectToAction("Details", new { id = staffId });
        }

        // ─── PAYMENTS — HR Manager only ───────────────────────────────
        public IActionResult Payments(int? staffId, string? status)
        {
            var guard = HROnly(); if (guard != null) return guard;

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