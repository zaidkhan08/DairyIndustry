
using System;
using System.ComponentModel.DataAnnotations;

namespace DairyIndustry.Models.Collection
{
    public class CenterInventoryViewModel
    {
        public int InventoryId { get; set; }

        [Required]
        public int CenterId { get; set; }
        public string? CenterName { get; set; }

        [Required]
        public int MilkTypeId { get; set; }

        public string MilkTypeName { get; set; }
        [Required]
        public decimal AvailableQuantity { get; set; } = 0;

        public DateTime LastUpdated { get; set; }

        public decimal TotalCollected { get; set; }   // total milk collected (from batches)
        public decimal TotalDispatched { get; set; }   // total milk dispatched (from transfer table)
    }
}