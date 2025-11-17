using Repositories.Models;

namespace Services.IServices
{
    public interface ILeopardAccountService
    {
        Task<LeopardAccount?> GetUserByCredentialsAsync(string email, string password);
    }
}
