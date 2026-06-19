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

        // ── PLANT MANAGER FILTERED LIST ────────────────────────────
        // Returns only staff assigned to the given PlantId.
        // roleId and isActive filters still apply within the plant.
        // Used by HRController.Index when role = Plant Manager.
        List<StaffModel> GetStaffByPlant(int plantId, int? roleId, bool? isActive);

        // ── USER REGISTRATION ──────────────────────────────────────
        void RegisterStaffUser(string username, string passwordHash,
                               int roleId, int staffId);

        // ── SALARY HISTORY ─────────────────────────────────────────
        void AddSalaryHistory(int staffId, decimal? oldSalary, decimal newSalary,
                              string? reason, string? changedBy);
        List<SalaryHistoryModel> GetSalaryHistory(int staffId);

        // ── PERFORMANCE NOTES ──────────────────────────────────────
        void AddStaffNote(int staffId, string noteText,
                          string noteType, string? createdBy);
        List<StaffNoteModel> GetStaffNotes(int staffId);
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