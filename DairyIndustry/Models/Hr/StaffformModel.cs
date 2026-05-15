using System.ComponentModel.DataAnnotations;

namespace DairyIndustry.Models
{
    // Used for: Create page and Edit page
    public class StaffFormModel
    {
        public int StaffId { get; set; }

        [Required(ErrorMessage = "First name is required.")]
        [Display(Name = "First Name")]
        public string? FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required.")]
        [Display(Name = "Last Name")]
        public string? LastName { get; set; }

        [Phone(ErrorMessage = "Enter a valid phone number.")]
        [Display(Name = "Phone")]
        public string? Phone { get; set; }

        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Role is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid role.")]
        [Display(Name = "Role")]
        public int RoleId { get; set; }

        [Display(Name = "Date of Joining")]
        [DataType(DataType.Date)]
        public DateTime? DOJ { get; set; }

        [Display(Name = "Active Staff Member")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Plant")]
        public int? PlantId { get; set; }

        [Display(Name = "Collection Center")]
        public int? CenterId { get; set; }

        [Range(0, 9999999.99, ErrorMessage = "Enter a valid salary.")]
        [Display(Name = "Salary")]
        public decimal? Salary { get; set; }

        // ── FEATURE 3 — Salary History ──────────────────────────────
        // Holds the salary value BEFORE editing — sent as a hidden field
        // from Edit.cshtml. Controller compares this with the submitted
        // Salary to detect a change and log it to HR.SalaryHistory.
        // Never shown to the user — purely a comparison mechanism.
        public decimal? CurrentSalary { get; set; }

        // Optional reason HR provides when changing salary.
        // Only shown on Edit form when salary field is changed.
        [Display(Name = "Reason for Salary Change")]
        [StringLength(255, ErrorMessage = "Reason cannot exceed 255 characters.")]
        public string? SalaryChangeReason { get; set; }
        // ── END FEATURE 3 ───────────────────────────────────────────

        // Bank details
        [Display(Name = "Bank Name")]
        public string? BankName { get; set; }

        [Display(Name = "Account Number")]
        public string? AccountNumber { get; set; }

        [Display(Name = "IFSC Code")]
        public string? IFSCCode { get; set; }

        // Profile Photo
        [Display(Name = "Profile Photo")]
        public string? ProfilePhoto { get; set; }

        [Display(Name = "Upload New Photo")]
        public IFormFile? PhotoFile { get; set; }

        // ── Login Credentials (Create only — optional) ───────────────
        [Display(Name = "Username")]
        [StringLength(50, ErrorMessage = "Username cannot exceed 50 characters.")]
        public string? Username { get; set; }

        [Display(Name = "Password")]
        [StringLength(100, MinimumLength = 6,
            ErrorMessage = "Password must be at least 6 characters.")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }
    }
}