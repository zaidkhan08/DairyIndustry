using System;
using System.ComponentModel.DataAnnotations;

namespace DairyIndustry.Models.Collection
{
    public class CollectionBatchViewModel
    {
        public int BatchId { get; set; }

        public int CenterId { get; set; }  

     
        [Required]
        [RegularExpression("Morning|Evening", ErrorMessage = "Invalid shift")]
        public string Shift { get; set; }

        [Required]
        public DateTime BatchDate { get; set; }

        public decimal? TotalQuantity { get; set; }

        public decimal? AvgFat { get; set; }

        public decimal? AvgCLR { get; set; }

    
        [RegularExpression("Open|Dispatched|Closed|Cancelled", ErrorMessage = "Invalid status")]
        public string Status { get; set; } = "Open";
    }
}
