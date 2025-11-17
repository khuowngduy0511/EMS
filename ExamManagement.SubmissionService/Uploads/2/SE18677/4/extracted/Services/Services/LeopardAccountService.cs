using Repositories.Interface;
using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Services
{
    public class LeopardAccountService
    {
        private readonly ILeopardAccountRepo _repo;

        public LeopardAccountService(ILeopardAccountRepo repo)
        {
            _repo = repo;
        }
        public async Task<LeopardAccount> GetUserByCredentialsAsync(string email, string password)
        {
            return await _repo.GetUserByCredentialsAsync(email, password);
        }
    }
}
