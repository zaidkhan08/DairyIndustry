namespace DairyIndustry.Models.Production
{
    public class PlantStaffModel
    {
        public int StaffId { get; set; }
        public string StaffName { get; set; }
        public string ProfilePhoto { get; set; }  // ✅ NEW
        public int PlantId { get; set; }
        public string PlantName { get; set; }
        public string PlantLocation { get; set; }
    }
}




