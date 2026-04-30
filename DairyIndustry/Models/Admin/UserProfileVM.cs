namespace DairyIndustry.Models.Admin
{
    public class UserProfileVM
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string RoleName { get; set; }

        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTime? DOJ { get; set; }
        public decimal? Salary { get; set; }
        public string ProfilePhoto { get; set; }
    }
}
