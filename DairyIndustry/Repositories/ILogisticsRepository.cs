using DairyIndustry.Models.Logistics;

namespace DairyIndustry.Repositories
{
    public interface ILogisticsRepository
    {
        DriversModel GetDriverByUserId(int userId);
        List<VehiclesModel> GetVehicleByDriverId(int driverId);
        int RegisterDriver(string driverName, string licenseNo, string phone, string username, string passwordHash);

        int AddVehicle(int driverId, string vehicleNumber, decimal capacity);
        List<VehiclesModel> GetVehiclesByDriverId(int driverId);

    }
}
