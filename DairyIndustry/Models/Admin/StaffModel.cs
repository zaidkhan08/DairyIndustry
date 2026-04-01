namespace DairyIndustry.Models.Admin
{
    public class StaffModel
    {
        public int StaffId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName => FirstName + " " + LastName;
        public string Phone { get; set; }
        public string Email { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; }   
        public DateTime? DOJ { get; set; }
        public bool IsActive { get; set; }
        public string ProfilePhoto { get; set; }

        // Bank details for display
        public int? BankAccountId { get; set; }
        public string BankName { get; set; }
        public string AccountNumber { get; set; }
        public string IFSCCode { get; set; }
    }
}
