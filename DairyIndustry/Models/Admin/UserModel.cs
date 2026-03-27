namespace DairyIndustry.Models.Admin
{
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public int RoleId { get; set; }
        public int? StaffId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }

        // For display — comes from JOIN in LoginUser SP
        public string RoleName { get; set; }
    }
}
