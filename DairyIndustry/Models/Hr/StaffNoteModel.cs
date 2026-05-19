namespace DairyIndustry.Models
{
    // Used for: Details page — Performance Notes section
    // Filled by: HrRepository.GetStaffNotes()
    public class StaffNoteModel
    {
        public int NoteId { get; set; }
        public int StaffId { get; set; }
        public string NoteText { get; set; } = string.Empty;
        public string NoteType { get; set; } = "General";
        public DateTime CreatedDate { get; set; }
        public string? CreatedBy { get; set; }

        // ── Computed display helpers ────────────────────────────────
        public string BadgeClass => NoteType switch
        {
            "Warning" => "bg-danger",
            "Appreciation" => "bg-success",
            "Feedback" => "bg-info text-dark",
            "Observation" => "bg-warning text-dark",
            _ => "bg-secondary"   // General
        };

        public string IconClass => NoteType switch
        {
            "Warning" => "bi-exclamation-triangle-fill text-danger",
            "Appreciation" => "bi-star-fill text-success",
            "Feedback" => "bi-chat-left-text-fill text-info",
            "Observation" => "bi-eye-fill text-warning",
            _ => "bi-journal-text text-secondary"  // General
        };

        public string CardBorderColor => NoteType switch
        {
            "Warning" => "#ef4444",
            "Appreciation" => "#22c55e",
            "Feedback" => "#06b6d4",
            "Observation" => "#f59e0b",
            _ => "#94a3b8"  // General
        };

        public string CardBgColor => NoteType switch
        {
            "Warning" => "#fef2f2",
            "Appreciation" => "#f0fdf4",
            "Feedback" => "#ecfeff",
            "Observation" => "#fffbeb",
            _ => "#f8fafc"  // General
        };
    }
}