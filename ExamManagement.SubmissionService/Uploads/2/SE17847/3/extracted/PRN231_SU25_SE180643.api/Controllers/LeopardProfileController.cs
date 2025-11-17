using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using PE_2.API.Model.Handbag;
using Repositories.Models;
using Services.IServices;
using System.Text.RegularExpressions;

namespace PRN231_SU25_SE180643.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LeopardProfilesController : ControllerBase
    {
        private readonly ILeopardProfileService _leopardProfileService;

        public LeopardProfilesController(ILeopardProfileService handbagService)
        {
            _leopardProfileService = handbagService;
        }

        [HttpGet]
        [Authorize(Roles = "administrator,moderator,developer,member")]
        public async Task<IActionResult> GetAll()
        {
            var leopardProfiles = await _leopardProfileService.GetAllWithTypeAsync();
            var result = leopardProfiles.Select(h => new LeopardProfileResponse
            {
                LeopardProfileId = h.LeopardProfileId,
                LeopardTypeId = h.LeopardTypeId,
                LeopardProfileName = h.LeopardName,
                Weight = h.Weight,
                Characteristics = h.Characteristics,
                CareNeeds = h.CareNeeds,
                ModifiedDate = h.ModifiedDate


            });

            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Administrator,moderator,developer,member")]
        public async Task<IActionResult> GetById(int id)
        {
            var h = await _leopardProfileService.GetByIdAsync(id);
            if (h == null)
                throw new KeyNotFoundException("LeopardProfile not found");

            var result = new LeopardProfileResponse
            {
                LeopardProfileId = h.LeopardProfileId,
                LeopardTypeId = h.LeopardTypeId,
                LeopardProfileName = h.LeopardName,
                Weight = h.Weight,
                Characteristics = h.Characteristics,
                CareNeeds = h.CareNeeds,
                ModifiedDate = h.ModifiedDate
            };

            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,moderator")]
        public async Task<IActionResult> Create([FromBody] LeopardProfileRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.LeopardProfileName) ||
                !Regex.IsMatch(request.LeopardProfileName, @"^([A-Z0-9][a-zA-Z0-9#]*\s)*([A-Z0-9][a-zA-Z0-9#]*)$"))
                throw new ArgumentException("profile name is invalid");

            if (request.Weight < 15)
                throw new ArgumentException("Weight must be >= 15");

            var entity = new LeopardProfile
            {
                LeopardTypeId = request.LeopardTypeId,
                LeopardName = request.LeopardProfileName,
                Weight = request.Weight,
                Characteristics = request.Characteristics,
                CareNeeds = request.CareNeeds,
                ModifiedDate = request.ModifiedDate
            };

            var created = await _leopardProfileService.AddAsync(entity);
            var full = await _leopardProfileService.GetByIdAsync(created.LeopardProfileId);

            return CreatedAtAction(nameof(GetById), new { id = full.LeopardProfileId }, new LeopardProfileResponse
            {
                LeopardProfileId = full.LeopardProfileId,
                LeopardTypeId = full.LeopardTypeId,
                LeopardProfileName = full.LeopardName,
                Weight = full.Weight,
                Characteristics = full.Characteristics,
                CareNeeds = full.CareNeeds,
                ModifiedDate = full.ModifiedDate
            });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "administrator,moderator")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateLeopardProfileRequest request)
        {

            var exists = await _leopardProfileService.GetByIdAsync(id);
            if (exists == null)
                throw new KeyNotFoundException("LeopardProfile not found");

            exists.LeopardTypeId = request.LeopardTypeId;
            exists.LeopardName = request.LeopardProfileName;
            exists.Weight = request.Weight;
            exists.CareNeeds = request.CareNeeds;
            exists.Characteristics = request.Characteristics;
            exists.ModifiedDate = request.ModifiedDate;

            var updated = await _leopardProfileService.UpdateAsync(exists);

            return Ok(new LeopardProfileResponse
            {
                LeopardProfileId = updated.LeopardProfileId,
                LeopardTypeId = updated.LeopardTypeId,
                LeopardProfileName = updated.LeopardName,
                Weight = updated.Weight,
                CareNeeds = updated.CareNeeds,
                Characteristics = updated.Characteristics,
                ModifiedDate = updated.ModifiedDate,
            });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "administrator,moderator")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _leopardProfileService.DeleteAsync(id);
            if (!success)
                throw new KeyNotFoundException("LeopardProfile not found");

            return Ok(new { message = "Deleted successfully" });
        }

        [HttpGet("search")]
        [Authorize(Roles = "administrator,moderator,developer,member")]
        public IActionResult Search(ODataQueryOptions<LeopardProfile> options)
        {
            var query = _leopardProfileService.GetAllWithTypeQueryable(); // IQueryable<LeopardProfile>

            // Áp dụng OData filter, sort, paging,...
            var filteredQuery = (IQueryable<LeopardProfile>)options.ApplyTo(query);

            // Lấy dữ liệu từ DB (có thể dùng ToListAsync nếu async)
            var result = filteredQuery
                .ToList()
                .GroupBy(h => h.LeopardType.LeopardTypeName)
                .Select(g => new
                {
                    Type = g.Key,
                    LeopardProfiles = g.Select(h => new LeopardProfileResponse
                    {
                        LeopardProfileId = h.LeopardProfileId,
                        LeopardTypeId = h.LeopardTypeId,
                        LeopardProfileName = h.LeopardName,
                        Weight = h.Weight,
                        Characteristics = h.Characteristics,
                        CareNeeds = h.CareNeeds,
                        ModifiedDate = h.ModifiedDate
                    })
                });

            return Ok(result);
        }

    }
}
