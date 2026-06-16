namespace DairyIndustry.Models.FarmerModel
{

    public class FarmerProfileModel
    {
        public int FarmerId { get; set; }
        public string FarmerName { get; set; }
        public string FarmerCode { get; set; }
        public string Phone { get; set; }
        public string ProfilePhoto { get; set; }
        public bool IsActive { get; set; }
        public string VillageName { get; set; }
        public string CityName { get; set; }
        public string StateName { get; set; }
        public int CenterId { get; set; }
        public string CenterName { get; set; }
        public string BankName { get; set; }
        public string AccountNumber { get; set; }
        public string IFSCCode { get; set; }
        public DateTime? LastLogin { get; set; }
        public string AadhaarNumber { get; set; }
        public string Email { get; set; }
        public string Gender { get; set; }
        public DateTime DateOfBirth { get; set; }
    }

}
