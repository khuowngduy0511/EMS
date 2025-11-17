using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Interface
{
   public interface ILeopardAccountRepo
    {
        Task<LeopardAccount?> GetUserByCredentialsAsync(string email, string password);

    }
}
