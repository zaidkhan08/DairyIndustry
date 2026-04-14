namespace DairyIndustry.Models.Admin
{
    public class UserAssignmentViewModel
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string RoleName { get; set; }

        // Plant assignment (null if not assigned)
        public int? UserPlantId { get; set; }
        public int? PlantId { get; set; }
        public string? PlantName { get; set; }

        // Center assignment (null if not assigned)
        public int? UserCenterId { get; set; }
        public int? CenterId { get; set; }
        public string? CenterName { get; set; }
    }
}
