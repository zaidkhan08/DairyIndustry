using DairyIndustry.Models.Admin;

namespace DairyIndustry.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Users ValidateUser(string username, string password);
    }
}
