using DairyIndustry.Models.Admin;
using DairyIndustry.Models.Logistics;

namespace DairyIndustry.Repositories
{
    public interface ILogisticsRepository
    {
        void SaveEmailOtp(string email, string otpCode);
        bool VerifyEmailOtp(string email, string otpCode);

        Task<int> RegisterDriverAsync(string driverName, string licenseNo,
                    string phone, string email, string username, string passwordHash,
                    string drivingLicensePath);
        DriversModel GetDriverByUserId(int userId);
        List<DriversModel> GetAllDrivers();
        void UpdateDriverStatus(int driverId, string status);

        Task<int> AddVehicleAsync(int driverId, string vehicleNumber, decimal capacity, string rcPath);
        List<VehiclesModel> GetVehiclesByDriverId(int driverId);
        List<VehiclesModel> GetAllVehicles();
        void UpdateVehicleStatus(int vehicleId, string status);

        List<MilkTransferModel> GetDriverTransfers(int driverId);

        DriverContactInfo GetDriverContactInfo(int driverId);
        DriverContactInfo GetDriverContactInfoByVehicleId(int vehicleId);
        Task SendDriverStatusEmailAsync(string toEmail, string driverName,
                                                      string username, string status);
        Task SendVehicleStatusEmailAsync(string toEmail, string driverName,
                                                               string vehicleNumber, string status);
        //Task SendEmailAsync(string toEmail, string subject, string htmlBody);

    }
}
