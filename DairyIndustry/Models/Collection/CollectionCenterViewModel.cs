using System.ComponentModel.DataAnnotations;

namespace DairyIndustry.Models.Collection
{
    public class CollectionCenterViewModel
    {
        public int CenterId { get; set; }

        [Required(ErrorMessage = "Center name is required")]
        [StringLength(100)]
        public string CenterName { get; set; }

        [Required(ErrorMessage = "Village is required")]
        public int VillageId { get; set; }

        [Range(0, 999999)]
        public decimal? Capacity { get; set; }

        [StringLength(100)]
        public string? Location { get; set; }
    }
}