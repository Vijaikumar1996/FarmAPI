using FarmAPI.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static FarmAPI.DTOs.AreaDto;

namespace FarmAPI.Controllers
{
    [Authorize] //(Roles = "Admin,Manager")
    [ApiController]
    [Route("api/[controller]")]
    public class AreasController : ControllerBase
    {
        private readonly IAreaService _areaService;

        public AreasController(
            IAreaService areaService)
        {
            _areaService = areaService;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            return Ok(await _areaService.GetAllAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(long id)
        {
            return Ok(await _areaService.GetByIdAsync(id));
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            CreateAreaRequest request)
        {
            var result = await _areaService.CreateAsync(request);

            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            long id,
            UpdateAreaRequest request)
        {
            await _areaService.UpdateAsync(id, request);

            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            await _areaService.DeleteAsync(id);

            return Ok();
        }
    }
}
