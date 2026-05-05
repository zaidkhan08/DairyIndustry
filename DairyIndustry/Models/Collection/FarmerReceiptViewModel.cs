
using System;
using System.ComponentModel.DataAnnotations;

namespace DairyIndustry.Models.Collection
{
    public class FarmerReceiptViewModel
    {
        // ── Receipt core ──────────────────────────────────────────────
        public int ReceiptId { get; set; }
        public int CollectionId { get; set; }
        public string? ReceiptNumber { get; set; }
        public DateTime ReceiptDate { get; set; }

        // ── Farmer ───────────────────────────────────────────────────
        public int FarmerId { get; set; }
        public string? FarmerName { get; set; }
        public string? FarmerCode { get; set; }

        // ── Collection detail ─────────────────────────────────────────
        public string? CenterName { get; set; }
        public string? MilkTypeName { get; set; }
        public string? Shift { get; set; }
        public DateTime CollectionDate { get; set; }
        public decimal Quantity { get; set; }
        public decimal? AppliedFat { get; set; }
        public decimal? AppliedCLR { get; set; }
        public decimal? RatePerLiter { get; set; }
        public decimal? Amount { get; set; }
    }
}

//Install-Package itext7 for pdf receipt