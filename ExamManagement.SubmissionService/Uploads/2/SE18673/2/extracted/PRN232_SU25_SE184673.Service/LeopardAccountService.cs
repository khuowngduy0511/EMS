using PRN232_SU25_SE184673.Repository.Models;
using PRN232_SU25_SE184673.Repository.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232_SU25_SE184673.Service
{
    public class LeopardAccountService
    {
        private readonly LeopardAccountRepository _repository;

        public LeopardAccountService (LeopardAccountRepository service) {_repository = service; }

        public async Task<LeopardAccount> Authentication(string username, string password)
        {
            return await _repository.Authentication(username, password);
        }
    }
}
