using DairyIndustry.Models.Admin;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace DairyIndustry.Models.FarmerModel
{
    public class FarmerViewModel
    {
        public int FarmerId { get; set; }

        [Required(ErrorMessage = "Farmer name is required")]
        public string FarmerName { get; set; }

        // ONLY Village is required (important)
        [Required(ErrorMessage = "Village is required")]
        public int? VillageId { get; set; }

        public int? StateId { get; set; }
        public int? CityId { get; set; }

        //  Phone required + validation
        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Enter valid 10 digit phone")]
        public string Phone { get; set; }

        // Optional
        public string BankName { get; set; }
        public string AccountNumber { get; set; }
        public string IFSCCode { get; set; }

        // Display only
        public string FarmerCode { get; set; }

        // File upload
        public string ProfilePhoto { get; set; }
        public IFormFile PhotoFile { get; set; }

        public bool IsActive { get; set; }

        // Dropdowns
        public List<StateModel> States { get; set; }
        public List<CityModel> Cities { get; set; }
        public List<VillageModel> Villages { get; set; }
    }
}