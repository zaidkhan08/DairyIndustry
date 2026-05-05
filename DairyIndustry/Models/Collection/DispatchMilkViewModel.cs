using DairyIndustry.Models.Logistics;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace DairyIndustry.Models.Collection
{
    public class DispatchMilkViewModel
    {
        [Required(ErrorMessage = "Please select a batch")]
        public int BatchId { get; set; }

        // ✅ NEW
        [Required(ErrorMessage = "Please select milk type")]
        public int MilkTypeId { get; set; }


        [Required(ErrorMessage = "Please select a vehicle")]
        public int VehicleId { get; set; }

        [Required(ErrorMessage = "Please select a plant")]
        public int PlantId { get; set; }

        [Required(ErrorMessage = "Dispatch quantity is required")]
        [Range(0.01, 999999, ErrorMessage = "Quantity must be greater than 0")]
        public decimal DispatchQty { get; set; }

        [Required(ErrorMessage = "Dispatch date is required")]
        public DateTime DispatchDate { get; set; } = DateTime.Today;

        // dropdowns — populated in GET, not validated on POST
        [ValidateNever] public List<ClosedBatchDropdownItem> ClosedBatches { get; set; } = new();
        [ValidateNever] public List<VehicleDropdownItem> Vehicles { get; set; } = new();
        [ValidateNever] public List<PlantDropdownItem> Plants { get; set; } = new();


        // ✅ NEW
        [ValidateNever] public List<MilkTypes> MilkTypes { get; set; } = new();

        // ✅ NEW — available qty for selected milk type (shown in UI)
        public decimal AvailableQty { get; set; }
    }
    public class ClosedBatchDropdownItem
    {
        public int BatchId { get; set; }
        public DateTime BatchDate { get; set; }
        public string Shift { get; set; }
        public decimal? TotalQuantity { get; set; }
        public decimal TotalDispatched { get; set; }
        public decimal RemainingQty { get; set; }
        public decimal? AvgFat { get; set; }
        public decimal? AvgCLR { get; set; }

        // shown as the <option> label
        public string DisplayText =>
            $"{BatchDate:dd-MM-yyyy} | {Shift} | Remaining: {RemainingQty:F2} L of {TotalQuantity:F2} L";
    }
    public class VehicleDropdownItem
    {
        public int VehicleId { get; set; }
        public string VehicleNumber { get; set; }
        public string DriverName { get; set; }

        public string DisplayText => $"{VehicleNumber} — {DriverName}";
    }

    public class PlantDropdownItem
    {
        public int PlantId { get; set; }
        public string PlantName { get; set; }
        public string Location { get; set; }
    }

    /* ── Dispatch history — one row per TRANSFER (not per batch) ── */
    public class DispatchHistoryViewModel
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
        public decimal? BatchTotalQty { get; set; }
        public decimal BatchRemainingQty { get; set; }

        public string PlantName { get; set; }
        public string VehicleNumber { get; set; }
        public string DriverName { get; set; }

        // "In Transit" | "Received"  (set by SP)
        public string TransferStatus { get; set; }
        // ✅ NEW
        public string MilkTypeName { get; set; }
        public bool IsReceived => TransferStatus == "Received";
        public bool IsInTransit => TransferStatus == "In Transit";
    }
}

