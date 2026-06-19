namespace DairyIndustry.Models
{
    // Used for: Role dropdown in Create and Edit forms
    // Filled by: GetRoles() inline query on Admin.Roles
    public class RoleModel
    {
        public int RoleId { get; set; }
        public string? RoleName { get; set; }
    }
}