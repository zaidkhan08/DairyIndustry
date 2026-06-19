using DairyIndustry.Models.Admin;

namespace DairyIndustry.Interfaces
{
    public interface IAuthRepository
    {
        string GenerateOtp(int userId, string purpose);
        OtpValidationResult ValidateOtp(int userId, string otpCode, string purpose);
        bool CheckTrustedDevice(int userId, string deviceToken);
        void RegisterTrustedDevice(int userId, string deviceToken, string deviceName, int daysDuration = 30);
        void RevokeTrustedDevice(int userId, string deviceToken);
        void RevokeAllTrustedDevices(int userId);
        List<TrustedDevice> GetTrustedDevices(int userId);
        void RevokeDeviceById(int userId, int deviceId);

    }
}