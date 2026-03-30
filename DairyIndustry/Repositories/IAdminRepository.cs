using DairyIndustry.Models.Admin;
using System.Data;

namespace DairyIndustry.Repositories
{
    
        public interface IAdminRepository
        {
            // ── ROLES ──────────────────────────────────────────────
            int CreateRole(string roleName);
            List<RoleModel> GetAllRoles();

            // ── USERS ──────────────────────────────────────────────
            int RegisterUser(string username, string passwordHash, int roleId, int? staffId);
            User GetUserByUsername(string username);
            List<User> GetAllUsers();
            void UpdateUserStatus(int userId, bool isActive);

            // ── AUDIT LOG ──────────────────────────────────────────
            void WriteAuditLog(int userId, string action, string entityName);
            List<AuditLogModel> GetAuditLogs(int? userId, string? entityName, DateTime? fromDate, DateTime? toDate);

        // ════════════════════════════════════════════════════════
        // LOCATION — STATE
        // ════════════════════════════════════════════════════════
        int AddState(string stateName);
        List<StateModel> GetAllStates();

        // ════════════════════════════════════════════════════════
        // LOCATION — CITY
        // ════════════════════════════════════════════════════════
        int AddCity(string cityName, int stateId);
        List<CityModel> GetAllCities();
        List<CityModel> GetCitiesByState(int stateId);

        // ════════════════════════════════════════════════════════
        // LOCATION — VILLAGE
        // ════════════════════════════════════════════════════════
        int AddVillage(string villageName, int cityId);
        List<VillageModel> GetAllVillages();
        List<VillageModel> GetVillagesByCity(int cityId);

        // ════════════════════════════════════════════════════════
        // MILK TYPES
        // ════════════════════════════════════════════════════════
        int AddMilkType(string milkTypeName);
        List<MilkTypeModel> GetAllMilkTypes();

        // ════════════════════════════════════════════════════════
        // RATE CHART
        // ════════════════════════════════════════════════════════
        int AddRateChart(int milkTypeId, decimal fatFrom, decimal fatTo, decimal clrFrom, decimal clrTo, decimal ratePerLiter, DateTime effectiveFrom);
        List<RateChartModel> GetAllRateCharts();
        RateChartModel GetActiveRate(int milkTypeId, decimal fat, decimal clr, DateTime? asOfDate);

        // ════════════════════════════════════════════════════════
        // STAFF
        // ════════════════════════════════════════════════════════
        int AddStaff(string firstName, string lastName, string phone, string email,
             string staffType, DateTime? doj,
             string bankName, string accountNumber, string ifscCode,
             string profilePhoto = null);

        List<StaffModel> GetAllStaff(string staffType = null, bool? isActive = null);

        void ToggleStaffActive(int staffId, bool isActive);

        // ════════════════════════════════════════════════════════
        // PLANT
        // ════════════════════════════════════════════════════════

        int AddPlant(string PlantName,string Location);

        List<PlantModel> GetAllPlants(bool? isActive = true);
        void TogglePlant(int id, bool isActive);
        void UpdatePlant(PlantModel plant);
        PlantModel getPlantById(int id);
    }

}
