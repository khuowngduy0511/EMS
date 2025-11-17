using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRN231_SU25_SE183841.Model;
using Repositories.Models;
using Services.Services;
using System.Text.RegularExpressions;

namespace PRN231_SU25_SE183841.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LeopardProfileController : ControllerBase
    {
        private readonly LeopardProfileService _service;

        public LeopardProfileController(LeopardProfileService service)
        {
            _service = service;
        }
        [HttpGet]
        [Authorize(Roles = "Administrator,Moderator,Developer,Member")]
        public async Task<IActionResult> GetAll()
            {
                var db = await _service.GetAll();
                var result = db.Select(h => new LeoprofileResponse
                {
                    LeopardProfileId = h.LeopardProfileId,
                    LeopardTypeId = h.LeopardProfileId,
                    LeopardName = h.LeopardName,
                    Weight = h.Weight,
                    Characteristics = h.Characteristics,
                    CareNeeds = h.CareNeeds,
                    ModifiedDate = h.ModifiedDate,
                        
                });

                return Ok(result);
            }
        [HttpGet("{id}")]
        [Authorize(Roles = "Administrator,Moderator,Developer,Member")]
        public async Task<IActionResult> GetById(int id)
        {
            var db = await _service.GetLeopardProfilebyId(id);
            var res = new LeoprofileResponse()
            {
                LeopardProfileId = db.LeopardProfileId,
                LeopardTypeId = db.LeopardProfileId,
                LeopardName = db.LeopardName,
                Weight = db.Weight,
                Characteristics = db.Characteristics,
                CareNeeds = db.CareNeeds,
                ModifiedDate = db.ModifiedDate,
            };
            return Ok(res);
        }
        [HttpPost]
        [Authorize(Roles = "Administrator,moderator")]
        public async Task<IActionResult> Create([FromBody] LeoprofileReq request)
        {
            if (string.IsNullOrWhiteSpace(request.LeopardName) ||
                !Regex.IsMatch(request.LeopardName, @"^([A-Z0-9][a-zA-Z0-9#]*\\s)*([A-Z0-9][a-zA-Z0-9#]*)$"))
                throw new ArgumentException("LeopardName is invalid");

            if (request.Weight <15)
                throw new ArgumentException("Leopard weight  must be > 15 ");

            var entity = new LeopardProfile
            {
                LeopardProfileId = request.LeopardProfileId,
                LeopardTypeId = request.LeopardProfileId,
                LeopardName = request.LeopardName,
                Weight = request.Weight,
                Characteristics = request.Characteristics,
                CareNeeds =     request.CareNeeds,
                ModifiedDate = request.ModifiedDate,
            };

            var created = await _service.AddAsync(entity);
            var full = await _service.GetLeopardProfilebyId(created.LeopardProfileId);

            return CreatedAtAction(nameof(GetById), new { id = full.LeopardProfileId }, new LeoprofileResponse
            {
                LeopardProfileId = full.LeopardProfileId,
                LeopardTypeId = full.LeopardProfileId,
                LeopardName = full.LeopardName,
                Weight = full.Weight,
                Characteristics = full.Characteristics,
                CareNeeds = full.CareNeeds,
                ModifiedDate = full.ModifiedDate,
            });
        }
        [HttpPut]
        [Authorize(Roles = "Administrator,moderator")]
        public async Task<IActionResult> Update([FromBody] LeoprofileReq request)
        {
            var db = await _service.GetLeopardProfilebyId(request.LeopardProfileId);
            db.LeopardName = request.LeopardName;
            db.LeopardProfileId = request.LeopardProfileId;
            db.LeopardTypeId = request.LeopardTypeId;
            db.Weight = request.Weight;
            db.Characteristics = request.Characteristics;
            db.CareNeeds = request.CareNeeds;
            db.ModifiedDate = request.ModifiedDate;
            _service.UpdateAsync(db);
            var res = new LeoprofileResponse()
            {
                LeopardProfileId = db.LeopardProfileId,
                LeopardTypeId = db.LeopardProfileId,
                LeopardName = db.LeopardName,
                Weight = db.Weight,
                Characteristics = db.Characteristics,
                CareNeeds = db.CareNeeds,
                ModifiedDate = db.ModifiedDate,
            };
            return Ok(res);

        }
        [HttpDelete]
        [Authorize(Roles = "Administrator,moderator")]

        public async Task<IActionResult> Delete(int id)
        {
            var res = await _service.DeleteAsync(id);
            if (!res)
                throw new KeyNotFoundException("Handbag not found");

            return Ok(new { message = "Deleted successfully" });


        }


    }
}
