using Microsoft.EntityFrameworkCore;
using Repositories.IRepositories;
using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Repositories
{
    public class LeopardAccountRepository : ILeopardAccountRepository
    {
        private readonly SU25LeopardDBContext _context;
        public LeopardAccountRepository(SU25LeopardDBContext context)
        {
            _context = context;
        }
        public async Task<LeopardAccount?> GetUserByCredentialsAsync(string email, string password)
        {
            return await _context.LeopardAccounts
                .FirstOrDefaultAsync(u => u.Email == email && u.Password == password);
        }
    }
}
