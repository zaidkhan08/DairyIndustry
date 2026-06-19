namespace DairyIndustry.Models.Location
{
    public class City
    {
        public int CityId { get; set; }
        public string CityName { get; set; }
        public int StateId { get; set; }  // foreign key
        public State State { get; set; }   // navigation property

      
    }
}
