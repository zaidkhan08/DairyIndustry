using DairyIndustry.Models.FarmerModel;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace DairyIndustry.Models.Collection
{
    public class MilkRejectionViewModel
    {
        public int RejectionId { get; set; }

        // WHO
        [Required(ErrorMessage = "Farmer is required")]
        public int FarmerId { get; set; }
        public string? FarmerName { get; set; }
        public string? FarmerCode { get; set; }

        // MILK DETAILS
        [Required(ErrorMessage = "Milk type is required")]
        public int MilkTypeId { get; set; }
        public string? MilkTypeName { get; set; }

        public decimal? AppliedFat { get; set; }
        public decimal? AppliedCLR { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(0.1, 1000, ErrorMessage = "Enter valid quantity")]
        public decimal Quantity { get; set; }

        // WHY REJECTED — simple string, matches SP directly
        [Required(ErrorMessage = "Rejection reason is required")]
        public string RejectionReason { get; set; }

        public string? Remarks { get; set; }

        // DISPLAY ONLY
        public string? Shift { get; set; }
        public DateTime RejectionDate { get; set; }
        public string? CenterName { get; set; }

        // FIXED LIST — matches SP CHECK constraint exactly
        [ValidateNever]
        public List<string> RejectionReasons { get; set; } = new()
        {
            "Adulterated",
            "Low Quality",
            "Wrong Temperature",
            "Smell / Spoiled",
            "Farmer Refused Rate",
            "Other"
        };

        // DROPDOWNS
        [ValidateNever]
        public List<FarmerViewModel> Farmers { get; set; } = new();

        [ValidateNever]
        public List<MilkTypes> MilkTypes { get; set; } = new();
        public int BatchId { get; internal set; }
    }
}