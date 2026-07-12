using FarmAPI.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static FarmAPI.DTOs.DeliveryLocationDto;

namespace FarmAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DeliveryLocationsController : ControllerBase
    {
        private readonly IDeliveryLocationService _deliveryLocationService;

        public DeliveryLocationsController(
            IDeliveryLocationService deliveryLocationService)
        {
            _deliveryLocationService = deliveryLocationService;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            return Ok(await _deliveryLocationService.GetAllAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(long id)
        {
            return Ok(await _deliveryLocationService.GetByIdAsync(id));
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            CreateDeliveryLocationRequest request)
        {
            var result = await _deliveryLocationService.CreateAsync(request);

            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            long id,
            UpdateDeliveryLocationRequest request)
        {
            await _deliveryLocationService.UpdateAsync(id, request);

            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            await _deliveryLocationService.DeleteAsync(id);

            return Ok();
        }
    }
}