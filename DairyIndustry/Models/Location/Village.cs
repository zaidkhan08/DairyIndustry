namespace DairyIndustry.Models.Location
{
    public class Village
    {


        public int VillageId { get; set; }
        public string VillageName { get; set; }
        public int CityId { get; set; }
        public City City { get; set; }
    }
}
