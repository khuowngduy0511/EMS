using Microsoft.EntityFrameworkCore;
using PRN232_SU25_SE184673.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232_SU25_SE184673.Repository.Repositories
{
    public class LeopardProfileRepository
    {
        private readonly SU25LeopardDBContext _context;

        public LeopardProfileRepository(SU25LeopardDBContext context) { _context = context; }

        public int GetLastId() {return _context.LeopardProfiles.Any() ? _context.LeopardProfiles.Max(h => h.LeopardProfileId) : 0; }

        public async Task<List<LeopardProfile>> GetAllAsync()
        {
            var items = await _context.LeopardProfiles.ToListAsync();
            return items;
        }

        public async Task<LeopardProfile?> GetById(int id)
        {
            var item = await _context.LeopardProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.LeopardProfileId == id);
            if (item == null) return null;
            return item;
        }

        public async Task<LeopardProfile?> AddNew(LeopardProfile profile)
        {
            if (profile == null) return null;
            profile.LeopardProfileId = GetLastId();
                    
            await _context.LeopardProfiles.AddAsync(profile);
            await _context.SaveChangesAsync();
            
            return _context.LeopardProfiles.Last();
        }

        public async Task<LeopardProfile?> Update(int id, LeopardProfile profile)
        {
            var item = _context.LeopardProfiles.FirstOrDefault(x => x.LeopardProfileId == id);
            if (item == null) return null;
            item.LeopardName = profile.LeopardName;
            item.CareNeeds = profile.CareNeeds;
            item.Characteristics = profile.Characteristics;
            item.Weight = profile.Weight;
            item.ModifiedDate = DateTime.UtcNow;
            item.LeopardTypeId = profile.LeopardTypeId;
            _context.Update(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task<bool> Delete(int id)
        {
            var item = _context.LeopardProfiles.FirstOrDefault(x => x.LeopardProfileId == id);
            if (item == null) return false;
            _context.Remove(item);
            await _context.SaveChangesAsync();
            return true;
        }

        public IQueryable<LeopardProfile> GetForOData()
        {
            return _context.LeopardProfiles.AsNoTracking();
        }

    }
}
