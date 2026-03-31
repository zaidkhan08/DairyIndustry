namespace DairyIndustry.Models.Logistics
{
    public class DriversModel
    {
        public int DriverId { get; set; }
        public int UserId { get; set; }
        public string DriverName { get; set; }
        public string LicenseNo { get; set; }
        public string Phone { get; set; }
        public string Status { get; set; }
        public DateTime RegisteredOn { get; set; }

        public string Username { get; set; }
        public bool IsActive { get; set; }

        public string VehicleNumber { get; set; }
        public string VehicleStatus { get; set; }
    }
}
