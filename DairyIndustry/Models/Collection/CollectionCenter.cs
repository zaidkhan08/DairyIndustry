namespace DairyIndustry.Models.Collection
{
    public class CollectionCenter
    {

        public int CenterId { get; set; }
        public string CenterName { get; set; }
        
        public int VillageId { get; set; }
        public string VillageName { get; set; }

        public decimal Capacity {  get; set; }
        public string Location { get; set; }
    }
}
