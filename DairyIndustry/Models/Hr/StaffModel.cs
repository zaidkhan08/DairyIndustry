namespace DairyIndustry.Models
{
    // Used for: Index page, Details page
    public class StaffModel
    {
        public int StaffId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }

        public int RoleId { get; set; }
        public string? RoleName { get; set; }

        public DateTime? DOJ { get; set; }
        public bool IsActive { get; set; }
        public string? ProfilePhoto { get; set; }
        public int? BankAccountId { get; set; }
        public string? BankName { get; set; }
        public string? AccountNumber { get; set; }
        public string? IFSCCode { get; set; }

        public int? PlantId { get; set; }
        public string? PlantName { get; set; }
        public int? CenterId { get; set; }
        public string? CenterName { get; set; }

        public decimal? Salary { get; set; }

        // ── FEATURE 1 — Login Account Status ───────────────────────
        // Populated only on GetStaffById (Details page).
        // Left null on GetAllStaff (Index page) for performance —
        // no join on Admin.Users for list queries.
        public bool HasLoginAccount { get; set; } = false;
        public string? LoginUsername { get; set; }
        public bool IsLoginActive { get; set; } = false;
        public DateTime? LoginCreatedDate { get; set; }

        // Computed helpers
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