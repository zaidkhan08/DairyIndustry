namespace DairyIndustry.Models.Admin
{
    public class VillageModel
    {
        public int VillageId { get; set; }
        public string VillageName { get; set; }
        public int CityId { get; set; }

        // For display
        public string CityName { get; set; }
        public string StateName { get; set; }
    }
}
