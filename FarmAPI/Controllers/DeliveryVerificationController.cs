
using Farm.API.Services;
using Farm.API.Services.Interfaces;
using FarmAPI.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Farm.API.Controllers
{
    [ApiController]
    [Route("api/delivery-verifications")]
    public class DeliveryVerificationController : ControllerBase
    {
        private readonly IDeliveryVerificationService _service;

        public DeliveryVerificationController(
            IDeliveryVerificationService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> Search(
            [FromQuery] DeliveryVerificationSearchRequestDto request)
        {
            var result = await _service.SearchAsync(request);

            return Ok(result);
        }

        [HttpPut("mark-all-delivered")]
        public async Task<IActionResult> MarkAllDelivered(
    MarkAllDeliveredRequestDto request)
        {
            await _service.MarkAllDeliveredAsync(request);

            return Ok();
        }

        [HttpGet("details")]
        public async Task<IActionResult> Get(
    long customerId,
    DateOnly deliveryDate)
        {
            var result =
                await _service.GetAsync(
                    customerId,
                    deliveryDate);

            return Ok(result);
        }

        [HttpPut]
        public async Task<IActionResult> Save(
    SaveDeliveryVerificationRequestDto request)
        {
            await _service
                .SaveAsync(request);

            return Ok();
        }
    }
}