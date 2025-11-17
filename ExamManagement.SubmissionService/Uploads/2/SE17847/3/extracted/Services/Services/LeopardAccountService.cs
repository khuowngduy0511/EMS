using Repositories.IRepositories;
using Repositories.Models;
using Services.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Services
{
    public class LeopardAccountService : ILeopardAccountService
    {
        private readonly ILeopardAccountRepository _leopardAccountRepository;
        public LeopardAccountService(ILeopardAccountRepository systemAccountRepository)
        {
            _leopardAccountRepository = systemAccountRepository;
        }
        public async Task<LeopardAccount?> GetUserByCredentialsAsync(string email, string password)
        {
            return await _leopardAccountRepository.GetUserByCredentialsAsync(email, password);
        }
    }
}
