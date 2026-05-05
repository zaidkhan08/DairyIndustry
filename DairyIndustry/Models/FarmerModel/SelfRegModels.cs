using DairyIndustry.Models.Admin;

namespace DairyIndustry.Models.FarmerModel
{
    /* ============================================================
       SelfRegisterViewModel
       Used by: Farmer/Register page (public self-registration)
       Step-by-step location selection, then final form fields.
       ============================================================ */
    public class SelfRegisterViewModel
    {
        // Location
        public int? StateId    { get; set; }
        public int? CityId     { get; set; }
        public int? VillageId  { get; set; }
        public int? CenterId   { get; set; }

        // Personal
        public string FarmerName { get; set; }
        public string Phone      { get; set; }

        // Bank (optional)
        public string BankName      { get; set; }
        public string AccountNumber { get; set; }
        public string IFSCCode      { get; set; }

        // Dropdowns — loaded per step
        public List<StateModel>         States   { get; set; } = new();
        public List<CityModel>          Cities   { get; set; } = new();
        public List<VillageModel>       Villages { get; set; } = new();
        public List<CenterDropdownModel> Centers  { get; set; } = new();
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

    /* ============================================================
       PendingApprovalViewModel
       Used by: CollectionCenter/PendingApprovals page
       One row per pending farmer at the staff's center.
       ============================================================ */
    public class PendingApprovalViewModel
    {
        public int    FarmerId      { get; set; }
        public string FarmerName    { get; set; }
        public string Phone         { get; set; }
        public string ProfilePhoto  { get; set; }
        public string VillageName   { get; set; }
        public string CityName      { get; set; }
        public string StateName     { get; set; }
        public string CenterName    { get; set; }
        public string BankName      { get; set; }
        public string AccountNumber { get; set; }
        public string IFSCCode      { get; set; }
        public string ApprovalRemark { get; set; }
    }

    /* ============================================================
       RejectFarmerViewModel
       Used by: CollectionCenter/RejectFarmer page
       Staff enters rejection reason for a specific farmer.
       ============================================================ */
    public class RejectFarmerViewModel
    {
        public int    FarmerId       { get; set; }
        public string FarmerName     { get; set; }
        public string Phone          { get; set; }
        public string ApprovalRemark { get; set; }
    }

    /* ============================================================
       ApprovalResultModel
       Returned from FarmerRepository.ApproveFarmer()
       Carries FarmerCode + DefaultPassword to show to staff
       so they can hand it to the farmer.
       ============================================================ */
    public class ApprovalResultModel
    {
        public int    FarmerId       { get; set; }
        public string FarmerCode     { get; set; }
        public string DefaultPassword { get; set; }
    }
}
