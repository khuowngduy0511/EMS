using Microsoft.EntityFrameworkCore;
using Repositories.Interface;
using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Repositories
{
    public class LeopardAccountRepo : ILeopardAccountRepo
    {
        private readonly SU25LeopardDBContext _context;

        public LeopardAccountRepo(SU25LeopardDBContext context)
        {
            _context = context;
        }

        public async Task<LeopardAccount?> GetUserByCredentialsAsync(string email, string password)
        {
            return await _context.LeopardAccounts.FirstOrDefaultAsync(x => x.Email == email && x.Password == password);

              
        }
    }
}
