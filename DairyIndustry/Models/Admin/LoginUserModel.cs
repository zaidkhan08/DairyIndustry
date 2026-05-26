namespace DairyIndustry.Models.Admin
{
    public class LoginUserModel
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public bool IsActive { get; set; }
        public int? StaffId { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; }

        // From HR.Staffs
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public int? CenterId { get; set; }
        public int? PlantId { get; set; }

        // From joins
        public string CenterName { get; set; }
        public string PlantName { get; set; }
    }
}
