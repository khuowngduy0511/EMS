using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Interface
{
  public  interface ILeopardProfileRepo
    {
        Task<List<LeopardProfile>> GetAll();
        Task<LeopardProfile> GetLeopardProfilebyId(int id);
        Task<LeopardProfile> AddAsync(LeopardProfile dto);
        Task<LeopardProfile> UpdateAsync(LeopardProfile dto);
        Task<bool> DeleteAsync(int id);
    }
}
