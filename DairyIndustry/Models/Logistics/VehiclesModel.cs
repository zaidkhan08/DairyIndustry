using Microsoft.AspNetCore.Components.Forms;

namespace DairyIndustry.Models.Logistics
{
    public class VehiclesModel
    {
        public int VehicleId { get; set; }
        public string VehicleNumber { get; set; }
        public double Capacity { get; set; }
        public int DriverId { get; set; }
        public string DriverName { get; set; }
        public string DriverStatus { get; set; }
    }
}
