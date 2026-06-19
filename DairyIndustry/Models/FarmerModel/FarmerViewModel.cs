using DairyIndustry.Models.Admin;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace DairyIndustry.Models.FarmerModel
{
    public class FarmerViewModel
    {
        public int FarmerId { get; set; }

        [Required(ErrorMessage = "Farmer name is required")]
        public string FarmerName { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Enter valid 10 digit phone")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "State is required")]
        public int? StateId { get; set; }

        [Required(ErrorMessage = "City is required")]
        public int? CityId { get; set; }

        [Required(ErrorMessage = "Village is required")]
        public int? VillageId { get; set; }

        [ValidateNever]
        public string VillageName { get; set; }
        [ValidateNever]
        public string CityName { get; set; }
        public string BankName { get; set; }
        public string AccountNumber { get; set; }
        public string IFSCCode { get; set; }

        [ValidateNever]
        public string FarmerCode { get; set; }
        [ValidateNever]
        public string ProfilePhoto { get; set; }

        // FILE UPLOAD
        [ValidateNever]
        public IFormFile PhotoFile { get; set; }

        public bool IsActive { get; set; }

        public List<StateModel> States { get; set; } = new();
        public List<CityModel> Cities { get; set; } = new();
        public List<VillageModel> Villages { get; set; } = new();

        [ValidateNever]
        public string DefaultPassword { get; set; }
        public string? StateName { get; internal set; }


        [Required(ErrorMessage = "Gender is required")]
        public string Gender { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email")]


        [ValidateNever]
        public bool IsEmailVerified { get; set; }

        [ValidateNever]
        public string OTP { get; set; }

        public string? Email { get; set; }

        [Required(ErrorMessage = "Aadhaar number is required")]
        [RegularExpression(@"^[0-9]{12}$", ErrorMessage = "Enter valid 12 digit Aadhaar number")]
        public string AadhaarNumber { get; set; }


        [ValidateNever]
        public string AadhaarCardPath { get; set; }

        [Required(ErrorMessage = "Aadhaar card is required")]
        public IFormFile AadhaarFile { get; set; }

        [ValidateNever]
        public string PassbookPath { get; set; }

        public IFormFile? PassbookFile { get; set; }

        [BindNever]
        [ValidateNever]
        public string ApprovalStatus { get; set; }

        [BindNever]
        [ValidateNever]
        public bool IsFirstLogin { get; set; }

    }
}
