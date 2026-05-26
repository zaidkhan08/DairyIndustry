namespace DairyIndustry.Models.Admin
{
    public class TrustedDevice
    {
        public int DeviceId { get; set; }
        public string DeviceToken { get; set; }    
        public string DeviceName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUsedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsActive { get; set; }
    }
}
