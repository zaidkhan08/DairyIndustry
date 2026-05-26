namespace DairyIndustry.Models.FarmerModel
{    
     //Farmer.usp_Center_GetPendingApprovals SP 
    public class PendingApprovalModel
    {
        // ── Identity ─────────────────────────────────────────────
        public int FarmerId { get; set; }
        public string FarmerName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }         
        public string Gender { get; set; }       
        public DateTime? DateOfBirth { get; set; }       
        public string AadhaarNumber { get; set; }       
        public string ProfilePhoto { get; set; }
        public DateTime? CreatedDate { get; set; }        

        // ── Approval ─────────────────────────────────────────────
        public string ApprovalStatus { get; set; }
        public string ApprovalRemark { get; set; }

        // ── Location ─────────────────────────────────────────────
        public string VillageName { get; set; }
        public string CityName { get; set; }
        public string StateName { get; set; }
        public string CenterName { get; set; }

        // ── Bank ─────────────────────────────────────────────────
        public string BankName { get; set; }
        public string AccountNumber { get; set; }
        public string IFSCCode { get; set; }

        // ── Documents ────────────────────────────────────────────
        public string AadhaarCardPath { get; set; }       
        public string PassbookPath { get; set; }     

        // ── Computed helpers used in the view ────────────────────

        /// <summary>Masked Aadhaar: shows only last 4 digits — XXXX-XXXX-1234</summary>
        public string AadhaarMasked =>
            string.IsNullOrEmpty(AadhaarNumber) || AadhaarNumber.Length < 4
                ? "Not provided"
                : "XXXX-XXXX-" + AadhaarNumber[^4..];

        /// <summary>Age calculated from DateOfBirth, null-safe.</summary>
        public string AgeDisplay =>
            DateOfBirth.HasValue
                ? $"{DateTime.Today.Year - DateOfBirth.Value.Year} yrs"
                : "Not provided";

        /// <summary>Days since application was submitted.</summary>
        public int DaysPending =>
            CreatedDate.HasValue
                ? (DateTime.Today - CreatedDate.Value.Date).Days
                : 0;

        /// <summary>True when bank details were provided.</summary>
        public bool HasBankDetails =>
            !string.IsNullOrEmpty(BankName) &&
            !string.IsNullOrEmpty(AccountNumber);

        /// <summary>True when Aadhaar doc was uploaded.</summary>
        public bool HasAadhaarDoc => !string.IsNullOrEmpty(AadhaarCardPath);

        /// <summary>True when passbook doc was uploaded.</summary>
        public bool HasPassbookDoc => !string.IsNullOrEmpty(PassbookPath);
    }
}