namespace DairyIndustry.Models.Admin
{
    public class Distributor
    {
        public int DistributorId { get; set; }
        public string DistributorName { get; set; } = string.Empty;
        public string? Location { get; set; }
        public string? ContactNumber { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? GSTIN { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime RegisteredOn { get; set; }
        public string Username { get; set; }
    }
}
