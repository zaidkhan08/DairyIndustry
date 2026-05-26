using DairyIndustry.Models.Admin;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace DairyIndustry.Models.FarmerModel
{
    /* ============================================================
       SelfRegisterViewModel
       Used by: Farmer/Register page (public self-registration)
       Step-by-step location selection, then final form fields.
       ============================================================ */
    using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
    using System.ComponentModel.DataAnnotations;

    public class SelfRegisterViewModel
    {
        // ================= LOCATION =================

        [Required(ErrorMessage = "Please select state")]
        public int? StateId { get; set; }

        [Required(ErrorMessage = "Please select city")]
        public int? CityId { get; set; }

        [Required(ErrorMessage = "Please select village")]
        public int? VillageId { get; set; }

        [Required(ErrorMessage = "Please select center")]
        public int? CenterId { get; set; }

        // ================= PERSONAL =================

        [Required(ErrorMessage = "Farmer name is required")]
        [StringLength(100)]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Only alphabets allowed")]
        public string FarmerName { get; set; }

        [Required(ErrorMessage = "Phone number required")]
        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter valid 10 digit mobile number")]
        public string Phone { get; set; }

        [ValidateNever]
        public bool IsEmailVerified { get; set; }

        // ================= EMAIL =================

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [RegularExpression(
            @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$",
            ErrorMessage = "Invalid email format")]
        public string Email { get; set; }

        public string EmailOTP { get; set; }

        // ================= OTHER DETAILS =================

        [RegularExpression(@"^(Male|Female|Other)?$", ErrorMessage = "Invalid gender")]
        public string Gender { get; set; }

        [Required(ErrorMessage = "DateOfBirth is required")]
        public DateTime? DateOfBirth { get; set; }

        [RegularExpression(@"^\d{12}$", ErrorMessage = "Aadhaar must be 12 digits")]
        public string AadhaarNumber { get; set; }

        // ================= FILES =================
        [Required(ErrorMessage = "Profile Photo is required")]
        public IFormFile ProfilePhotoFile { get; set; }
        [Required(ErrorMessage = "Aadhar document is required")]
        public IFormFile AadhaarFile { get; set; }

        [Required(ErrorMessage = "Passbook document is required")]
        public IFormFile PassbookFile { get; set; }

        // ================= PATHS =================

        public string ProfilePhotoPath { get; set; }

        public string AadhaarDocPath { get; set; }

        public string PassbookDocPath { get; set; }

        // ================= BANK (OPTIONAL) =================

        [Required(ErrorMessage = "Bank name is required")]
        [RegularExpression(@"^[a-zA-Z\s]*$", ErrorMessage = "Invalid bank name")]
        public string BankName { get; set; }

        [Required(ErrorMessage = "Account number is required")]
        [RegularExpression(@"^\d{9,18}$", ErrorMessage = "Invalid account number")]
        public string AccountNumber { get; set; }

        [Required(ErrorMessage = "IFSC code is required")]
        [RegularExpression(@"^[A-Z]{4}0[A-Z0-9]{6}$", ErrorMessage = "Invalid IFSC code")]
        public string IFSCCode { get; set; }

        // ================= DROPDOWNS =================

        public List<StateModel> States { get; set; } = new();

        public List<CityModel> Cities { get; set; } = new();

        public List<VillageModel> Villages { get; set; } = new();

        public List<CenterDropdownModel> Centers { get; set; } = new();
    }

    /* ============================================================
       CenterDropdownModel
       Used by: usp_GetCentersByVillage result set
       ============================================================ */
    public class CenterDropdownModel
    {
        public int    CenterId   { get; set; }
        public string CenterName { get; set; }
        public string Location   { get; set; }
    }

    /* ============================================================
       FarmerStatusViewModel
       Used by: Farmer/CheckStatus page
       Farmer enters phone → sees their approval status.
       ============================================================ */
    public class FarmerStatusViewModel
    {
        public string Phone          { get; set; }
        public int?   FarmerId       { get; set; }
        public string FarmerName     { get; set; }
        public string FarmerCode     { get; set; }
        public string ApprovalStatus { get; set; }
        public string ApprovalRemark { get; set; }
        public string CenterName     { get; set; }
        public bool   Searched       { get; set; }
    }

    public class RejectFarmerViewModel
    {
        public int    FarmerId       { get; set; }
        public string FarmerName     { get; set; }
        public string Phone          { get; set; }
        public string ApprovalRemark { get; set; }
    }

}
