using Microsoft.EntityFrameworkCore;
using PRN232_SU25_SE184673.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232_SU25_SE184673.Repository.Repositories
{
    public class LeopardAccountRepository
    {
        private readonly SU25LeopardDBContext _dbContext;

        public LeopardAccountRepository(SU25LeopardDBContext dbContext) { _dbContext = dbContext; }

        public async Task<LeopardAccount?> Authentication(string email, string password)
        {
            var account = await _dbContext.LeopardAccounts
                .FirstOrDefaultAsync(a => a.Email == email && a.Password == password);
            if (account == null) return null;
            return account;
        }
    }
}
