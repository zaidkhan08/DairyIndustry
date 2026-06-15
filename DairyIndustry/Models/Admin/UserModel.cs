namespace DairyIndustry.Models.Admin
{
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public bool IsActive { get; set; }
        public int? StaffId { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public int? CenterId { get; set; }
        public int? PlantId { get; set; }
        public string CenterName { get; set; }
        public string PlantName { get; set; }
        public DateTime CreatedDate { get; set; }

        public int? DriverId { get; set; }
        public string DriverName { get; set; }
        public string DriverStatus { get; set; }
    }
}
