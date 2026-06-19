
namespace DairyIndustry.Models.FarmerModel
{
    /* ============================================================
       ApprovalResultModel
       Returned from FarmerRepository.ApproveFarmer()

       CHANGE: added Email so the controller can send the
       approval email without an extra DB round-trip.
       Email is null when the farmer did not supply one —
       controller handles that case gracefully.
       ============================================================ */
    public class ApprovalResultModel
    {
        public int FarmerId { get; set; }
        public string FarmerCode { get; set; }
        public string DefaultPassword { get; set; }

        public string? Email { get; set; }
    }
}