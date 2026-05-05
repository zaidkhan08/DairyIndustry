using System;

namespace DairyIndustry.Models.Collection
{
    public class BatchStatusViewModel
    {
        public int? BatchId { get; set; }

        public string Shift { get; set; }

        public string ShiftWindow { get; set; }

        public string BatchStatus { get; set; }

        public decimal TotalQuantity { get; set; }

        public decimal? AvgFat { get; set; }

        public decimal? AvgCLR { get; set; }

        public int EntryCount { get; set; }

        public bool IsCurrentShift { get; set; }

        public DateTime BatchDate { get; set; }

    }
}