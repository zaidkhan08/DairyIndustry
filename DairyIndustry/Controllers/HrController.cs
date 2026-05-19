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
                        .ToList(),

                    // FEATURE 2 — Inactive staff alert
                    // All staff with IsActive = false, ordered by name.
                    // Reuses already-loaded allStaff — zero extra DB call.
                    InactiveStaffList = allStaff
                        .Where(s => !s.IsActive)
                        .OrderBy(s => s.FullName)
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

                // Feature 3 — Salary history
                ViewBag.SalaryHistory = _repo.GetSalaryHistory(id);

                // Feature 4 — Performance notes
                ViewBag.StaffNotes = _repo.GetStaffNotes(id);

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
                    // FEATURE 3 — store current salary so Edit POST can
                    // detect whether it changed and log history accordingly
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

                // FEATURE 3 — Detect salary change BEFORE updating
                // Compare submitted Salary with CurrentSalary (hidden field)
                // Only log if salary actually changed and new salary has a value
                bool salaryChanged = model.Salary.HasValue
                    && model.Salary != model.CurrentSalary;

                // Save staff first
                _repo.UpdateStaff(model);

                // Log salary history AFTER successful save
                // Wrapped in try/catch — history failure must never
                // prevent the staff update from completing
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
                                ? null
                                : model.SalaryChangeReason.Trim(),
                            changedBy
                        );
                    }
                    catch
                    {
                        // Silently ignore — salary history logging is
                        // supplementary and must never block the main save
                    }
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

        // ─── ADD NOTE — /HR/AddNote ────────────────────────────────────
        // FEATURE 4 — Adds a performance note for a staff member.
        // Called via POST from the Details page note form.
        // NoteText is required — returns error if blank.
        // CreatedBy pulled from session Username.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddNote(int staffId, string noteText, string noteType)
        {
            if (string.IsNullOrWhiteSpace(noteText))
            {
                TempData["Error"] = "Note text cannot be empty.";
                return RedirectToAction("Details", new { id = staffId });
            }

            // Validate noteType against allowed values
            var allowedTypes = new[]
            {
                "General", "Warning", "Appreciation", "Feedback", "Observation"
            };

            if (!allowedTypes.Contains(noteType))
                noteType = "General";

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

        // ─── DELETE NOTE — /HR/DeleteNote ──────────────────────────────
        // FEATURE 4 — Deletes a note by NoteId.
        // StaffId is passed and verified in the repo to prevent
        // cross-staff deletion via URL tampering.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteNote(int noteId, int staffId)
        {
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