using DairyIndustry.Models.Admin;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace DairyIndustry.Models.FarmerModel
{
    public class RegByCenterModel
    {
        // =====================================================
        // LOCATION
        // =====================================================

        [Required(ErrorMessage = "Please select state")]
        public int? StateId { get; set; }

        [Required(ErrorMessage = "Please select city")]
        public int? CityId { get; set; }

        [Required(ErrorMessage = "Please select village")]
        public int? VillageId { get; set; }

        // =====================================================
        // PERSONAL DETAILS
        // =====================================================

        [Required(ErrorMessage = "Farmer name is required")]
        [StringLength(100, ErrorMessage = "Maximum 100 characters allowed")]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Only alphabets allowed")]
        public string FarmerName { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        [RegularExpression(@"^(Male|Female|Other)$", ErrorMessage = "Invalid gender")]
        public string Gender { get; set; }

        [Required(ErrorMessage = "Date of birth is required")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter valid 10 digit mobile number")]
        public string Phone { get; set; }

        // =====================================================
        // EMAIL
        // =====================================================

        [EmailAddress(ErrorMessage = "Invalid email address")]
        [RegularExpression(
            @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$",
            ErrorMessage = "Invalid email format")]
        public string Email { get; set; }

        [ValidateNever]
        public bool IsEmailVerified { get; set; }

        // =====================================================
        // AADHAAR
        // =====================================================

        [Required(ErrorMessage = "Aadhaar number is required")]
        [RegularExpression(@"^\d{12}$", ErrorMessage = "Aadhaar must be 12 digits")]
        public string AadhaarNumber { get; set; }

        // =====================================================
        // BANK DETAILS
        // =====================================================

        [Required(ErrorMessage = "Bank name is required")]
        [StringLength(100)]
        [RegularExpression(@"^[a-zA-Z\s&.-]+$", ErrorMessage = "Invalid bank name")]
        public string BankName { get; set; }

        [Required(ErrorMessage = "Account number is required")]
        [RegularExpression(@"^\d{9,18}$", ErrorMessage = "Account number must be 9-18 digits")]
        public string AccountNumber { get; set; }

        [Required(ErrorMessage = "IFSC code is required")]
        [RegularExpression(@"^[A-Z]{4}0[A-Z0-9]{6}$", ErrorMessage = "Invalid IFSC code")]
        public string IFSCCode { get; set; }

        // =====================================================
        // FILES
        // =====================================================

        [Required(ErrorMessage = "Profile photo is required")]
        public IFormFile PhotoFile { get; set; }

        [Required(ErrorMessage = "Aadhaar document is required")]
        public IFormFile AadhaarFile { get; set; }

        [Required(ErrorMessage = "Passbook document is required")]
        public IFormFile PassbookFile { get; set; }

        // =====================================================
        // FILE PATHS
        // =====================================================

        [ValidateNever]
        public string ProfilePhoto { get; set; }

        [ValidateNever]
        public string AadhaarCardPath { get; set; }

        [ValidateNever]
        public string PassbookPath { get; set; }

        // =====================================================
        // DROPDOWNS
        // =====================================================

        [ValidateNever]
        public List<StateModel> States { get; set; } = new();

        [ValidateNever]
        public List<CityModel> Cities { get; set; } = new();

        [ValidateNever]
        public List<VillageModel> Villages { get; set; } = new();

        // =====================================================
        // GENERATED VALUES
        // =====================================================

        [ValidateNever]
        public int FarmerId { get; set; }

        [ValidateNever]
        public string FarmerCode { get; set; }

        [ValidateNever]
        public string DefaultPassword { get; set; }
    }
}