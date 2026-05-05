namespace DairyIndustry.Models.Admin
{
    public class CollectionCenterModel
    {
        public int CenterId { get; set; }
        public string CenterName { get; set; }
        public int VillageId { get; set; }
        public string? VillageName { get; set; }
        public int Capacity { get; set; }
        public string Location { get; set; }
        public bool IsActive { get; set; }
    }
}
