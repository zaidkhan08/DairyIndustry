namespace DairyIndustry.Models.Collection
{
    public class StaffDashboardViewModel
    {
        // 🔹 RESULT SET 1 (Staff + Center)
        public int StaffId { get; set; }
        public string StaffName { get; set; }
        public string StaffType { get; set; }
        public string StaffPhoto { get; set; }

        public int CenterId { get; set; }
        public string CenterName { get; set; }

        public string VillageName { get; set; }
        public string CityName { get; set; }
        public string StateName { get; set; }

        public decimal Capacity { get; set; }
        public decimal CurrentStock { get; set; }
        public decimal AvailableSpace { get; set; }



        // 🔹 RESULT SET 2 (Shift + Batch)
        public List<ShiftBatchInfo> Shifts { get; set; } = new();

        // 🔹 RESULT SET 3 (Summary)
        public decimal TotalMilkToday { get; set; }
        public decimal TotalAmountToday { get; set; }
        public int TotalEntriesToday { get; set; }
        public int ActiveFarmerCount { get; set; }
        public decimal PendingPaymentAmount { get; set; }
    }

    public class ShiftBatchInfo
    {
        public string Shift { get; set; }
        public string ShiftWindow { get; set; }

        public int? BatchId { get; set; }
        public string BatchStatus { get; set; }

        public decimal TotalQuantity { get; set; }
        public decimal? AvgFat { get; set; }
        public decimal? AvgCLR { get; set; }

        public int EntryCount { get; set; }
        public bool IsCurrentShift { get; set; }
    }
}
