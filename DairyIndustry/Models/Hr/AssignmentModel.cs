namespace DairyIndustry.Models
{
    public class PlantAssignModel
    {
        public int PlantId { get; set; }
        public string? PlantName { get; set; }
        public string? Location { get; set; }
        public string DisplayText =>
            string.IsNullOrEmpty(Location) ? PlantName! : $"{PlantName} ({Location})";
    }

    public class CenterAssignModel
    {
        public int CenterId { get; set; }
        public string? CenterName { get; set; }
        public string? Location { get; set; }
        public string DisplayText =>
            string.IsNullOrEmpty(Location) ? CenterName! : $"{CenterName} ({Location})";
    }
}