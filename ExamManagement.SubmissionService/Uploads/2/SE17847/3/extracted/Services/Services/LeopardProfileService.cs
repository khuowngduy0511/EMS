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
    public class LeopardProfileService : ILeopardProfileService
    {
        private readonly ILeopardProfileRepository _repository;

        public LeopardProfileService(ILeopardProfileRepository repository)
        {
            _repository = repository;
        }

        public Task<LeopardProfile?> GetByIdAsync(int id) => _repository.GetByIdAsync(id);
        public Task<LeopardProfile> AddAsync(LeopardProfile profile) => _repository.AddAsync(profile);
        public Task<LeopardProfile> UpdateAsync(LeopardProfile profile) => _repository.UpdateAsync(profile);
        public Task<bool> DeleteAsync(int id) => _repository.DeleteAsync(id);

        IQueryable<LeopardProfile> ILeopardProfileService.GetAllWithTypeQueryable()
        {
            return _repository.GetAllWithTypeQueryable();
        }

        public Task<List<LeopardProfile>> GetAllWithTypeAsync()
        {
            return _repository.GetAllWithTypeAsync();
        }
    }
}
