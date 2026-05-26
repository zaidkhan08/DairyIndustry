using DairyIndustry.Models.Admin;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace DairyIndustry.Models.FarmerModel
{
    public class FarmerEditModel
    {
        // =========================
        // BASIC INFO
        // =========================
        public int FarmerId { get; set; }

        public string? FarmerName { get; set; }
        public string? FarmerCode { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }

        // =========================
        // LOCATION
        // =========================
        public int StateId { get; set; }
        public int CityId { get; set; }
        public int VillageId { get; set; }

        [ValidateNever]
        public string? StateName { get; set; }

        [ValidateNever]
        public string? CityName { get; set; }

        [ValidateNever]
        public string? VillageName { get; set; }

        // =========================
        // BANK DETAILS
        // =========================
        public string? BankName { get; set; }
        public string? AccountNumber { get; set; }
        public string? IFSCCode { get; set; }

        // =========================
        // IDENTITY (READ-ONLY IN EDIT)
        // =========================
        [ValidateNever]
        public string? AadhaarNumber { get; set; }

        // =========================
        // FILE PATHS (FROM DB)
        // =========================
        [ValidateNever]
        public string? ProfilePhoto { get; set; }

        [ValidateNever]
        public string? AadhaarCardPath { get; set; }

        [ValidateNever]
        public string? PassbookPath { get; set; }

        // =========================
        // FILE UPLOADS
        // =========================
        [ValidateNever]
        public IFormFile? PhotoFile { get; set; }

        [ValidateNever]
        public IFormFile? AadhaarFile { get; set; }

        [ValidateNever]
        public IFormFile? PassbookFile { get; set; }

        // =========================
        // STATUS (READ-ONLY)
        // =========================
        [ValidateNever]
        public bool IsActive { get; set; }

        [ValidateNever]
        public string? ApprovalStatus { get; set; }

        // =========================
        // DROPDOWNS
        // =========================
        [ValidateNever]
        public List<StateModel> States { get; set; } = new();

        [ValidateNever]
        public List<CityModel> Cities { get; set; } = new();

        [ValidateNever]
        public List<VillageModel> Villages { get; set; } = new();
    }
}