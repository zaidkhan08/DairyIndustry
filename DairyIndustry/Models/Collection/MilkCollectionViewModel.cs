using Microsoft.AspNetCore.Mvc.Rendering;

namespace DairyIndustry.Models.Collection
{
    public class MilkCollectionViewModel
    {
        // Entry fields (submitted by staff)
        public int FarmerId { get; set; }
        public int MilkTypeId { get; set; }
        public decimal Quantity { get; set; }
        public decimal AppliedFat { get; set; }
        public decimal AppliedCLR { get; set; }

        // Read-only display fields only for show
        public string CenterName { get; set; }
        public string CurrentShift { get; set; }//public string CurrentShift { get; set; }   // e.g. "Morning" — detected from current time
        public string Shift { get; set; }   // ✅ used for POST 
        public DateTime CollectionDate { get; set; } // always today, set server-side

        // Dropdowns
        public List<SelectListItem> Farmers { get; set; }
        public List<SelectListItem> MilkTypes { get; set; }
    }
}
