using DairyIndustry.Models;

namespace DairyIndustry.Repositories
{
    public interface IHRRepository
    {
        // ── STAFF CRUD ─────────────────────────────────────────
        List<StaffModel> GetAllStaff(int? roleId, bool? isActive);
        StaffModel? GetStaffById(int staffId);
        int AddStaff(StaffFormModel model);
        bool UpdateStaff(StaffFormModel model);
        bool ToggleActive(int staffId, bool isActive);
        bool UpdateProfilePhoto(int staffId, string photoPath);

        // ── DROPDOWNS ──────────────────────────────────────────
        List<RoleModel> GetRoles();
        List<PlantAssignModel> GetPlants();
        List<CenterAssignModel> GetCenters();

        // ── DASHBOARD ──────────────────────────────────────────
        HRDashboardSummaryModel GetDashboardSummary();
        List<StaffTypeCountModel> GetStaffByType();

        // ── PAYMENTS ───────────────────────────────────────────
        List<StaffPaymentModel> GetPaymentsByStaff(int staffId);
        List<StaffPaymentModel> GetAllPayments(string? status);
    }
}