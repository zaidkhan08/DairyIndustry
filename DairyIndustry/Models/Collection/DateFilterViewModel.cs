

namespace DairyIndustry.Models.Collection
{

    public class DateFilterViewModel
    {
        public DateTime? SelectedDate { get; set; }

        public List<DateWiseMilkEntryViewModel> Entries { get; set; }
    }
}
