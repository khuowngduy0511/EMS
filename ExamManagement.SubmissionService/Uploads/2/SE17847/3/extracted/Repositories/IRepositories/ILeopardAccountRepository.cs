using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.IRepositories
{
    public interface ILeopardAccountRepository
    {
        Task<LeopardAccount?> GetUserByCredentialsAsync(string email, string password);
    }
}
