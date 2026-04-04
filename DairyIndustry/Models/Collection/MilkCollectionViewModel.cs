using DairyIndustry.Models.Admin;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.ComponentModel.DataAnnotations;

namespace DairyIndustry.Models.Collection
{
    public class MilkCollectionViewModel
    {
        public int CollectionId { get; set; }

        [Required(ErrorMessage = "Farmer is required")]
        public int FarmerId { get; set; }

        public int CenterId { get; set; }

        [Required(ErrorMessage = "Milk type is required")]
        public int MilkTypeId { get; set; }

        public int BatchId { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(0.1, 1000, ErrorMessage = "Invalid quantity")]
        public decimal Quantity { get; set; }

   
        [Required]
        [RegularExpression("Morning|Evening", ErrorMessage = "Invalid shift")]
        public string Shift { get; set; }


        public DateTime CollectionDate { get; set; }

        public decimal? AppliedFat { get; set; }

        public decimal? AppliedCLR { get; set; }

        public decimal? RatePerLiter { get; set; }

        public decimal? Amount { get; set; }

        public string? FarmerName { get; set; }
        public string? FarmerCode { get; set; }
        public string? MilkTypeName { get; set; }
        public string? ShiftWindow { get; set; }
        public string? ReceiptNumber { get; set; }

        [ValidateNever]
        public List<RateChartModel> RateCharts { get; set; }
    }
}
