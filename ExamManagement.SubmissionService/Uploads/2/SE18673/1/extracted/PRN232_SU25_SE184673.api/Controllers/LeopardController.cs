using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using PRN232_SU25_SE184673.Repository.Models;
using PRN232_SU25_SE184673.Service;
using System.Threading.Tasks;

namespace PRN232_SU25_SE184673.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LeopardController : ControllerBase
    {
        private LeopardProfileService _service;
        public LeopardController(LeopardProfileService service) { _service = service; }

        [HttpGet]
        [Authorize(Roles = "4, 5, 6, 7")]
        public async Task<IActionResult> Get()
        {
            var items = await _service.GetAll();
            return StatusCode(200, items.Item2);
        }

        [HttpGet("id")]
        [Authorize(Roles = "4, 5, 6 ,7")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _service.GetById(id);
            return StatusCode(200, item);
        }

        [HttpPost]
        [Authorize(Roles = "5, 6")]
        public async Task<IActionResult> Add([FromBody]LeopardProfile profile)
        {
            if (profile == null) return StatusCode(400, new
            {
                errorCode = "HB40001",
                message = "need item"
            });
            if (!ModelState.IsValid) return StatusCode(400, new
            {
                errorCode = "HB40001",
                message = "Name and Weight must be check"
            });
            var item = await _service.Add(profile);
            return StatusCode(200, item.Item2);
        }

        [HttpPut("id")]
        [Authorize(Roles = "5, 6")]
        public async Task<IActionResult> Edit(int id, [FromBody] LeopardProfile profile)
        {
            if (id <=0) return StatusCode(400, new
            {
                errorCode = "HB40001",
                message = "Check input"
            });

            if(profile == null) return StatusCode(400, new
            {
                errorCode = "HB40001",
                message = "Check input"
            });
            var up = await _service.Update(id, profile);
            return StatusCode(200, up.Item2);
        }

        [HttpDelete("id")]
        [Authorize(Roles = "5, 6")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0) return StatusCode(400, new
            {
                errorCode = "HB40001",
                message = "Check input"
            });
            var item = await _service.Delete(id);
            return StatusCode(200, item.Item2);
        }

        [HttpGet("search")]
        [Authorize(Roles = "4, 5, 6, 7")]
        [EnableQuery]
        public IActionResult SearchHandbags([FromQuery] string? Name, [FromQuery] int? weight)
        {
            var query = _service.GetLeo();
            if (query.code == 404)
            {
                return StatusCode(404, new
                {
                    errorCode = "HB40401",
                    errorMessage = "Resource not found"
                });
            }
            if (!string.IsNullOrEmpty(Name))
            {
                query.Item2 = query.Item2.Where(h => h.LeopardName.ToLower().Contains(Name.ToLower()));
            }
            if (weight > 15)
            {
                query.Item2 = query.Item2.Where(h => h.Weight.Equals(weight));
            }
            return Ok(query.Item2);
        }
    }
}
