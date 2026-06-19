namespace DairyIndustry.Models
{
    // Used for: Details page — Salary History card
    // Filled by: HrRepository.GetSalaryHistory()
    public class SalaryHistoryModel
    {
        public int HistoryId { get; set; }
        public int StaffId { get; set; }
        public decimal? OldSalary { get; set; }
        public decimal NewSalary { get; set; }
        public DateTime ChangedDate { get; set; }
        public string? Reason { get; set; }
        public string? ChangedBy { get; set; }

        // Computed — show change direction for display
        public string ChangeDirection
        {
            get
            {
                if (OldSalary == null) return "initial";
                if (NewSalary > OldSalary.Value) return "increase";
                if (NewSalary < OldSalary.Value) return "decrease";
                return "same";
            }
        }

        public string ChangeBadgeClass => ChangeDirection switch
        {
            "increase" => "bg-success",
            "decrease" => "bg-danger",
            "initial" => "bg-primary",
            _ => "bg-secondary"
        };

        public string ChangeIcon => ChangeDirection switch
        {
            "increase" => "bi-arrow-up-circle-fill",
            "decrease" => "bi-arrow-down-circle-fill",
            "initial" => "bi-stars",
            _ => "bi-dash-circle"
        };
    }
}