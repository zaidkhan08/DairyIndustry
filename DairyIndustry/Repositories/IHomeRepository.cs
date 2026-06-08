using DairyIndustry.Models.ViewModels;

namespace DairyIndustry.Repositories
{
    public interface IHomeRepository
    {
        public LandingStatsModel GetLandingStats();
    }
}
