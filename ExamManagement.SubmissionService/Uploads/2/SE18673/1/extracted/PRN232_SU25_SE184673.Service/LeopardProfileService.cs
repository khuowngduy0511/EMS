using PRN232_SU25_SE184673.Repository.Models;
using PRN232_SU25_SE184673.Repository.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PRN232_SU25_SE184673.Service
{
    public class LeopardProfileService
    {
        private readonly LeopardProfileRepository repository;

        public LeopardProfileService(LeopardProfileRepository repository) { this.repository = repository; }

        public async Task<(int code, List<LeopardProfile>)> GetAll()
        {
            var items =  await repository.GetAllAsync();
            return (200, items);
        }

        public async Task<(int code, LeopardProfile?)> GetById(int id)
        {
            if (id <= 0) return (404, null);
            var item = await repository.GetById(id);
            if (item == null) return (404, null);
            return (200, item);
        }

        public async Task<(int code, LeopardProfile?)> Add(LeopardProfile item)
        {
            if (item == null) return (400, null);
            var newLeo = await repository.AddNew(item);
            if (newLeo == null) return (400, null);
            return (200, newLeo);
        }

        public async Task<(int code, LeopardProfile?)> Update(int id, LeopardProfile leopardProfile)
        {
            if (id <= 0) return (404, null) ;
            if (leopardProfile == null) return (400, null);

            var up = await repository.Update(id, leopardProfile);
            return (200, up);
        }

        public async Task<(int code, bool)> Delete(int id)
        {
            if (id <= 0) return (404, false);
            var del = await repository.Delete(id);
            return (200, del);
        }

        public (int code, IQueryable<LeopardProfile>? query) GetLeo()
        {
            var leos = repository.GetForOData();
            if (leos!=null) return(200, leos);
            return (404, null);
        }
    }
}
