namespace DairyIndustry.Models.Collection
{


    public class OpenBatchRequest
    {
        public int CenterId { get; set; }
        public string CenterName { get; set; }
        public string Shift { get; set; }
        public DateTime BatchDate { get; set; }


    }
}
