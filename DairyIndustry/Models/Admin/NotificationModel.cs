namespace DairyIndustry.Models.Admin
{
    public class NotificationModel
    {
        public int NotificationId { get; set; }
        public string Category { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public int EntityId { get; set; }
        public string EntityType { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Severity { get; set; }   // danger / warning / info
        public string ActionUrl { get; set; }

        public bool IsRead { get; set; }
    }
}
