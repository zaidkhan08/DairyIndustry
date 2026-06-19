namespace DairyIndustry.Models
{
    // Plant dropdown — filled by inline query on Production.ProcessingPlants
    public class PlantModel
    {
        public int PlantId { get; set; }
        public string? PlantName { get; set; }
        public string? Location { get; set; }

        public string DisplayText =>
            string.IsNullOrEmpty(Location)
                ? PlantName ?? ""
                : $"{PlantName} ({Location})";
    }
}