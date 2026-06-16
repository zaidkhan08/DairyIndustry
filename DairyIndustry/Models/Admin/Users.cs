namespace DairyIndustry.Models.Admin
{
    public class Users
    {

        public int UserId { get; set; }
        public string UserName { get; set; }
        public string PasswordHash { get; set; }
        public int RoleId { get; set; }
        public int StaffId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CenterId { get; set; }
        public int FarmerId { get; set; }
    }
}
