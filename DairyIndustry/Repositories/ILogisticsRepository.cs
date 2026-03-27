using DairyIndustry.Models.Logistics;

namespace DairyIndustry.Repositories
{
    public interface ILogisticsRepository
    {
        int AddDriver(string driverName, string licenseNo, string phone);
        void UpdateDriverStatus(int driverId, string status);
        List<DriversModel> GetAllDrivers();
        DriversModel GetDriverById(int driverId);
        int? GetDriverIdByUserId(int userId);
    }
}
