using Microsoft.AspNetCore.Mvc.Rendering;

namespace DairyIndustry.Models.Collection
{

    public class MilkCollectionViewModel
    {
        public int FarmerId { get; set; }
        public int MilkTypeId { get; set; }
        public int BatchId { get; set; }

        public decimal Quantity { get; set; }
        public string Shift { get; set; }
        public DateTime CollectionDate { get; set; }

        public decimal AppliedFat { get; set; }
        public decimal AppliedCLR { get; set; }
        public string CenterName { get; set; }
        public List<SelectListItem> Farmers { get; set; }
        public List<SelectListItem> MilkTypes { get; set; }
        public List<SelectListItem> Batches { get; set; }
    }
}
