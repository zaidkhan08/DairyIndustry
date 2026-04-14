namespace DairyIndustry.Models
{
    // Used for: Index page, Details page
    // Filled by: usp_HR_GetStaff, GetStaffById inline query
    public class StaffModel
    {
        public int StaffId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }

        // RoleId — NOT NULL in DB — FK to Admin.Roles
        public int RoleId { get; set; }
        public string? RoleName { get; set; }

        public DateTime? DOJ { get; set; }
        public bool IsActive { get; set; }
        public string? ProfilePhoto { get; set; }
        public int? BankAccountId { get; set; }
        public string? BankName { get; set; }
        public string? AccountNumber { get; set; }
        public string? IFSCCode { get; set; }

        // Directly on HR.Staffs — no separate assignment table
        public int? PlantId { get; set; }
        public string? PlantName { get; set; }
        public int? CenterId { get; set; }
        public string? CenterName { get; set; }

        // New column in latest DB
        public decimal? Salary { get; set; }

        // Computed — shows where staff is assigned
        public string AssignmentDisplay
        {
            get
            {
                if (PlantName != null) return $"Plant — {PlantName}";
                if (CenterName != null) return $"Collection Center — {CenterName}";
                return "Not Assigned";
            }
        }

        public string FullName => $"{FirstName} {LastName}";
        public string StatusBadgeClass => IsActive ? "badge bg-success" : "badge bg-secondary";
        public string StatusLabel => IsActive ? "Active" : "Inactive";
    }
}