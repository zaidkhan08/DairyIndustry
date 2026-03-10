namespace DairyIndustry.Models.Admin
{
    public class CityModel
    {
        public int CityId { get; set; }
        public string CityName { get; set; }
        public int StateId { get; set; }

        // For display
        public string StateName { get; set; }
    }
}
