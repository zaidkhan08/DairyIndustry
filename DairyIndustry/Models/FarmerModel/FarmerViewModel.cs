using DairyIndustry.Models.Admin;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace DairyIndustry.Models.FarmerModel
{
    public class FarmerViewModel
    {
        public int FarmerId { get; set; }

    // =========================
    // BASIC DETAILS
    // =========================
    [Required(ErrorMessage = "Farmer name is required")]
        public string FarmerName { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Enter valid 10 digit phone")]
        public string Phone { get; set; }

        // =========================
        // LOCATION (IMPORTANT FIX)
        // =========================
        [Required(ErrorMessage = "State is required")]
        public int? StateId { get; set; }

        [Required(ErrorMessage = "City is required")]
        public int? CityId { get; set; }

        [Required(ErrorMessage = "Village is required")]
        public int? VillageId { get; set; }

        // DISPLAY ONLY (NO VALIDATION)
        [ValidateNever]
        public string VillageName { get; set; }
        [ValidateNever]
        public string CityName { get; set; }

        // =========================
        // BANK DETAILS (OPTIONAL)
        // =========================
        public string BankName { get; set; }
        public string AccountNumber { get; set; }
        public string IFSCCode { get; set; }

        // =========================
        // SYSTEM FIELDS (NO REQUIRED)
        // =========================
        [ValidateNever]
        public string FarmerCode { get; set; }
        [ValidateNever]
        public string ProfilePhoto { get; set; }

        // FILE UPLOAD
        [ValidateNever]
        public IFormFile PhotoFile { get; set; }

        public bool IsActive { get; set; }

        // =========================
        // DROPDOWNS (NO VALIDATION)
        // =========================
        public List<StateModel> States { get; set; } = new();
        public List<CityModel> Cities { get; set; } = new();
        public List<VillageModel> Villages { get; set; } = new();


        [ValidateNever]
        public string DefaultPassword { get; set; }
        public string? StateName { get; internal set; }
    }


}
