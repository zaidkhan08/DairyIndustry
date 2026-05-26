namespace DairyIndustry.Models.Finance
{
    // ── One row in the Plant Staff List ─────────────────────────
    public class PlantStaffModel
    {
        public int StaffId { get; set; }
        public string FullName { get; set; }        // FirstName + LastName
        public string RoleName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public decimal? Salary { get; set; }
        public bool IsActive { get; set; }
        public DateTime? JoiningDate { get; set; }

        // Center this staff member belongs to
        public int CenterId { get; set; }
        public string CenterName { get; set; }

        // Plant derived from center (used for scoping)
        public int PlantId { get; set; }
        public string PlantName { get; set; }

        // Latest payment info
        public DateTime? LastPaymentDate { get; set; }
        public string LastPaymentStatus { get; set; }

        // ── Badge helper ────────────────────────────────────────
        public string StatusPillClass => IsActive ? "sl-pill-active" : "sl-pill-inactive";
        public string PaymentPillClass => LastPaymentStatus switch
        {
            "Processed" => "sl-pill-processed",
            "Pending" => "sl-pill-pending",
            "Failed" => "sl-pill-failed",
            _ => "sl-pill-none"
        };
    }

    // ── Summary card at top of the Staff List page ──────────────
    public class PlantStaffSummary
    {
        public int PlantId { get; set; }
        public string PlantName { get; set; }
        public int TotalStaff { get; set; }
        public int ActiveStaff { get; set; }
        public int InactiveStaff { get; set; }
        public decimal TotalMonthlySalary { get; set; }   // sum of Salary for active staff
        public int TotalCenters { get; set; }              // distinct centers in this plant
    }

    // ── Wrapper passed to the view ───────────────────────────────
    public class PlantStaffViewModel
    {
        public PlantStaffSummary Summary { get; set; }
        public List<PlantStaffModel> Staff { get; set; } = new();
    }
}