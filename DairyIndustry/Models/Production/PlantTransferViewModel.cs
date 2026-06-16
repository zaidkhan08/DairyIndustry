using System.ComponentModel.DataAnnotations;

namespace DairyIndustry.Models.Production
{
    public class PlantTransferViewModel
    {
 
        public int TransferId { get; set; }
        public DateTime DispatchDate { get; set; }
        public decimal DispatchQty { get; set; }
        public decimal? ReceivedQty { get; set; }
        public decimal? LossQty { get; set; }
        public DateTime? ReceivedDate { get; set; }
        public int BatchId { get; set; }
        public DateTime BatchDate { get; set; }
        public string Shift { get; set; }
        public string CenterName { get; set; }
        public string PlantName { get; set; }
        public string VehicleNumber { get; set; }
        public string DriverName { get; set; }
        public string MilkTypeName { get; set; }
        public string TransferStatus { get; set; }

        public bool IsReceived => TransferStatus == "Received";
        public bool IsInTransit => TransferStatus == "In Transit";
    }

    public class ReceiveMilkViewModel
    {
        public int TransferId { get; set; }

        // display only
        public string CenterName { get; set; }
        public string MilkTypeName { get; set; }
        public string VehicleNumber { get; set; }
        public string DriverName { get; set; }
        public decimal DispatchQty { get; set; }
        public DateTime DispatchDate { get; set; }

        // staff fills this
        [Required(ErrorMessage = "Received quantity is required")]
        [Range(0.01, 999999, ErrorMessage = "Enter valid quantity")]
        public decimal ReceivedQty { get; set; }

        [Required(ErrorMessage = "Received date is required")]
        public DateTime ReceivedDate { get; set; } = DateTime.Today;
    }
}