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
    public class LeopardProfileRepo : ILeopardProfileRepo
    {
        private readonly SU25LeopardDBContext _context;

        public LeopardProfileRepo(SU25LeopardDBContext context)
        {
            _context = context;
        }

        public async Task<LeopardProfile> AddAsync(LeopardProfile dto)
        {
            _context.LeopardProfiles.Add(dto);
            await _context.SaveChangesAsync();
            return dto;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var res = await _context.LeopardProfiles.FindAsync(id);
            if (res == null) return false;
            _context.LeopardProfiles.Remove(res);
            return true;
        }

        public async Task<List<LeopardProfile>> GetAll()
        {
            return 
                await _context.LeopardProfiles.Include(h => h.LeopardType).ToListAsync();
        }

        public async Task<LeopardProfile> GetLeopardProfilebyId(int id)
        {
            return  _context.LeopardProfiles.Include(h => h.LeopardType).FirstOrDefault( x => x.LeopardProfileId == id);
        }

        public async Task<LeopardProfile> UpdateAsync(LeopardProfile dto)
        {
            _context.LeopardProfiles.Update(dto);
            await _context.SaveChangesAsync();
            return dto;
        }
    }
}
