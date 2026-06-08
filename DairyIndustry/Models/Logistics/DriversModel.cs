namespace DairyIndustry.Models.Logistics
{
    public class DriversModel
    {
        public int DriverId { get; set; }
        public string DriverName { get; set; }
        public string LicenseNo { get; set; }
        public string? Phone { get; set; }

        public string? Email { get; set; }
        public bool IsEmailVerified { get; set; }
        public string? DrivingLicensePath { get; set; }   

        public string Status { get; set; }
        public DateTime RegisteredOn { get; set; }
        public string Username { get; set; }
        public bool IsActive { get; set; }

        public string? VehicleNumber { get; set; }
        public string? VehicleStatus { get; set; }
        public string? VehicleRCPath { get; set; }
    }
}
