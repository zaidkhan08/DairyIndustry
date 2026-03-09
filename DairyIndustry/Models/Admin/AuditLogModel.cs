namespace DairyIndustry.Models.Admin
{
    public class AuditLogModel
    {
        public int LogId { get; set; }
        public string Username { get; set; }
        public string Action { get; set; }
        public string EntityName { get; set; }
        public DateTime ActionDate { get; set; }
    }
}
