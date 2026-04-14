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

        // RoleId — NOT NULL in DB — must be selected
        [Required(ErrorMessage = "Role is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid role.")]
        [Display(Name = "Role")]
        public int RoleId { get; set; }

        [Display(Name = "Date of Joining")]
        [DataType(DataType.Date)]
        public DateTime? DOJ { get; set; }

        [Display(Name = "Active Staff Member")]
        public bool IsActive { get; set; } = true;

        // Assignment — directly on HR.Staffs, fill only one
        [Display(Name = "Plant")]
        public int? PlantId { get; set; }

        [Display(Name = "Collection Center")]
        public int? CenterId { get; set; }

        // New column
        [Range(0, 9999999.99, ErrorMessage = "Enter a valid salary.")]
        [Display(Name = "Salary")]
        public decimal? Salary { get; set; }

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
    }
}