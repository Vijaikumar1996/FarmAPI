using FarmAPI.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static FarmAPI.DTOs.CustomerDto;

namespace FarmAPI.Controllers
{
    [Authorize] // (Roles = "Admin,Manager")
    [ApiController]
    [Route("api/[controller]")]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerService _customerService;

        public CustomersController(
            ICustomerService customerService)
        {
            _customerService = customerService;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            return Ok(await _customerService.GetAllAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(long id)
        {
            return Ok(await _customerService.GetByIdAsync(id));
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            CreateCustomerRequest request)
        {
            var result = await _customerService.CreateAsync(request);

            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            long id,
            UpdateCustomerRequest request)
        {
            await _customerService.UpdateAsync(id, request);

            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            await _customerService.DeleteAsync(id);

            return Ok();
        }

        [HttpGet("typeahead")]
        public async Task<IActionResult> Typeahead(
        [FromQuery] string? searchText)
        {
            var result =
                await _customerService
                    .GetTypeaheadAsync(searchText);

            return Ok(result);
        }
    }
}