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
        public decimal Salary { get; set; }
        // Bank
        public int? BankAccountId { get; set; }
        public string BankName { get; set; }
        public string AccountNumber { get; set; }
        public string IFSCCode { get; set; }

        // Location — NEW
        public int? CenterId { get; set; }
        public string CenterName { get; set; }
        public int? PlantId { get; set; }
        public string PlantName { get; set; }
        // Derived for display — NEW
        public string AssignedToType => CenterId.HasValue ? "Collection Center"
                                      : PlantId.HasValue ? "Plant"
                                      : "Unassigned";
        public string AssignedToName => CenterName ?? PlantName ?? "—";

        public string Username { get; set; }
        public bool HasLogin { get; set; }
        public int? UserId { get; set; }
    }
}
