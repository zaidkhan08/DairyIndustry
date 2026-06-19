namespace DairyIndustry.Models.FarmerModel
{
    public class CenterRejectedFarmerModel
    {
        public int FarmerId { get; set; }
        public string FarmerName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Gender { get; set; }
        public string ProfilePhoto { get; set; }
        public string ApprovalStatus { get; set; }
        public string ApprovalRemark { get; set; }
        public string AadhaarNumber { get; set; }
        public string VillageName { get; set; }
        public string CityName { get; set; }
        public string StateName { get; set; }
        public string CenterName { get; set; }
        public string BankName { get; set; }
        public string AccountNumber { get; set; }
        public string IFSCCode { get; set; }
        public DateTime CreatedDate { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public string AadhaarDocumentPath { get; set; }
        public string BankPassbookPath { get; set; }
    }
}