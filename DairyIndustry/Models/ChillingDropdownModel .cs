namespace DairyIndustry.Models
{
    // Used for: populating dropdowns in Add and Edit forms

    // ── Processing Plant dropdown item ──
    public class PlantDropdownModel
    {
        public int PlantId { get; set; }
        public string PlantName { get; set; }
        public string Location { get; set; }

        // Display text shown in the dropdown
        public string DisplayText =>
            string.IsNullOrEmpty(Location)
                ? PlantName
                : $"{PlantName} ({Location})";
    }

    // ── Product dropdown item ──
    public class ProductDropdownModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductType { get; set; }
        public string Unit { get; set; }

        // Display text shown in the dropdown
        public string DisplayText => $"{ProductName} ({ProductType} — {Unit})";
    }

}
