using System.ComponentModel.DataAnnotations;

namespace DairyIndustry.Models
{
    // ── Display model (read from DB) ──────────────────────────────────────
    public class DistributorModel
    {
        public int DistributorId { get; set; }
        public string? DistributorName { get; set; }
        public string? Location { get; set; }
        public string? ContactNumber { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? GSTIN { get; set; }
        public string? Status { get; set; }
        public DateTime? RegisteredOn { get; set; }

        public string DisplayText =>
            string.IsNullOrEmpty(Location)
                ? DistributorName ?? ""
                : $"{DistributorName} ({Location})";

        public string StatusBadgeClass => Status switch
        {
            "Approved" => "success",
            "Rejected" => "danger",
            "Suspended" => "warning",
            "Pending" => "secondary",
            _ => "secondary"
        };
    }

    // ── Admin Add / Edit form ─────────────────────────────────────────────
    public class DistributorFormModel
    {
        public int DistributorId { get; set; }

        [Required(ErrorMessage = "Distributor name is required.")]
        [Display(Name = "Distributor Name")]
        public string? DistributorName { get; set; }

        [Display(Name = "Location")]
        public string? Location { get; set; }

        [Phone(ErrorMessage = "Enter a valid contact number.")]
        [Display(Name = "Contact Number")]
        public string? ContactNumber { get; set; }

        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Display(Name = "Address")]
        public string? Address { get; set; }

        [Display(Name = "GSTIN")]
        public string? GSTIN { get; set; }
    }

    // ── Public self-registration form ─────────────────────────────────────
    // Password is hashed in C# before being passed to usp_Sales_RegisterDistributor
    public class DistributorRegisterModel
    {
        [Required(ErrorMessage = "Business name is required.")]
        [Display(Name = "Distributor / Business Name")]
        public string? DistributorName { get; set; }

        [Display(Name = "Location")]
        public string? Location { get; set; }

        [Phone(ErrorMessage = "Enter a valid contact number.")]
        [Display(Name = "Contact Number")]
        public string? ContactNumber { get; set; }

        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Display(Name = "Address")]
        public string? Address { get; set; }

        [Display(Name = "GSTIN")]
        public string? GSTIN { get; set; }

        [Required(ErrorMessage = "Username is required.")]
        [Display(Name = "Username")]
        public string? Username { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        [Display(Name = "Password")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [Required(ErrorMessage = "Please confirm your password.")]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        [DataType(DataType.Password)]
        public string? ConfirmPassword { get; set; }
    }

    // ── Result returned by usp_Sales_DistributorLogin ─────────────────────
    // Used by the COMMON login controller to verify password and set session
    public class DistributorLoginResultModel
    {
        public int UserId { get; set; }
        public string? Username { get; set; }
        public string? PasswordHash { get; set; }   // SHA-256 hex stored in DB
        public bool IsActive { get; set; }          // false = not approved yet
        public int DistributorId { get; set; }
        public string? DistributorName { get; set; }
        public string? Location { get; set; }
        public string? ContactNumber { get; set; }
        public string? Email { get; set; }
        public string? GSTIN { get; set; }
        public string? Status { get; set; }         // Pending / Approved / Rejected / Suspended
    }
}