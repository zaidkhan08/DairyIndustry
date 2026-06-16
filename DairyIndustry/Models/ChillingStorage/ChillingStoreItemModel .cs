using System.ComponentModel.DataAnnotations;

namespace DairyIndustry.Models.ChillingStorage
{
    // Used for: Add new entry form (Create page) and Edit page
    // Filled by: user input in the form
    // Passed to: StoreItem SP (add) and UpdateEntry query (edit)
    public class ChillingStoreItemModel
    {
        public int StorageId { get; set; }  // 0 for new, filled for edit

        // Fix #10 — [Required] on int never catches 0 (model binding default)
        // [Range] correctly rejects 0 and negative values
        [Range(1, int.MaxValue, ErrorMessage = "Plant assignment is missing. Contact Admin.")]
        [Display(Name = "Processing Plant")]
        public int PlantId { get; set; }

        // Nullable — user may select a product OR leave as Raw Milk
        [Display(Name = "Product (leave empty for Raw Milk)")]
        public int? ProductId { get; set; }

        [Required(ErrorMessage = "Quantity is required.")]
        [Range(0.01, 99999.99, ErrorMessage = "Quantity must be greater than 0.")]
        [Display(Name = "Quantity (Litres)")]
        public decimal MilkQuantity { get; set; }

        [Range(-10.00, 50.00, ErrorMessage = "Temperature must be between -10 and 50°C.")]
        [Display(Name = "Temperature (°C)")]
        public decimal? Temperature { get; set; }

        [Display(Name = "Storage Date")]
        [DataType(DataType.Date)]
        public DateTime StoredDate { get; set; } = DateTime.Today;

        // Shift — optional, one of Morning / Afternoon / Night
        [Display(Name = "Shift")]
        [RegularExpression("^(Morning|Afternoon|Night)$",
            ErrorMessage = "Shift must be Morning, Afternoon, or Night.")]
        public string? Shift { get; set; }
    }
}