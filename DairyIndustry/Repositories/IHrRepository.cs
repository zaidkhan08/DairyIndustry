using DairyIndustry.Models;

namespace DairyIndustry.Repositories
{
    public interface IHRRepository
    {
        // ── STAFF CRUD ─────────────────────────────────────────────
        List<StaffModel> GetAllStaff(int? roleId, bool? isActive);
        StaffModel? GetStaffById(int staffId);
        int AddStaff(StaffFormModel model);
        bool UpdateStaff(StaffFormModel model);
        bool ToggleActive(int staffId, bool isActive);
        bool UpdateProfilePhoto(int staffId, string photoPath);

        // ── USER REGISTRATION ──────────────────────────────────────
        void RegisterStaffUser(string username, string passwordHash,
                               int roleId, int staffId);

        // ── SALARY HISTORY ─────────────────────────────────────────
        void AddSalaryHistory(int staffId, decimal? oldSalary, decimal newSalary,
                              string? reason, string? changedBy);
        List<SalaryHistoryModel> GetSalaryHistory(int staffId);

        // ── PERFORMANCE NOTES ──────────────────────────────────────
        // Add a note against a staff member from the Details page.
        void AddStaffNote(int staffId, string noteText,
                          string noteType, string? createdBy);

        // Get all notes for a staff member — newest first.
        List<StaffNoteModel> GetStaffNotes(int staffId);

        // Delete a note — only if it belongs to the given StaffId
        // (prevents cross-staff deletion via URL tampering).
        bool DeleteStaffNote(int noteId, int staffId);

        // ── DROPDOWNS ──────────────────────────────────────────────
        List<RoleModel> GetRoles();
        List<PlantAssignModel> GetPlants();
        List<CenterAssignModel> GetCenters();

        // ── DASHBOARD ──────────────────────────────────────────────
        HRDashboardSummaryModel GetDashboardSummary();
        List<StaffTypeCountModel> GetStaffByType();

        // ── PAYMENTS ───────────────────────────────────────────────
        List<StaffPaymentModel> GetPaymentsByStaff(int staffId);
        List<StaffPaymentModel> GetAllPayments(string? status);
    }
}