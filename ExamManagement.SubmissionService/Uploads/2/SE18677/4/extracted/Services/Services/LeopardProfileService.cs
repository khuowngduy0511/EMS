using Repositories.Interface;
using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Services
{
    public class LeopardProfileService 
    {
        private readonly ILeopardProfileRepo _repo;

        public LeopardProfileService(ILeopardProfileRepo repo)
        {
            _repo = repo;
        }

        public Task<LeopardProfile> AddAsync(LeopardProfile dto)
        {
            return _repo.AddAsync(dto);
        }

        public Task<bool> DeleteAsync(int id)
        {
            return _repo.DeleteAsync(id);
        }

        public Task<List<LeopardProfile>> GetAll()
        {
            return _repo.GetAll();
        }

        public Task<LeopardProfile> GetLeopardProfilebyId(int id)
        {
            return _repo.GetLeopardProfilebyId(id);

        }

        public Task<LeopardProfile> UpdateAsync(LeopardProfile dto)
        {
            return _repo.UpdateAsync(dto);

        }
    }
}
