using FarmAPI.DTOs;
using FarmAPI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using static FarmAPI.DTOs.CustomerRequestDto;

namespace FarmAPI.Controllers;

[ApiController]
[Route("api/customer-requests")]
public class CustomerRequestsController : ControllerBase
{
    private readonly ICustomerRequestService _customerRequestService;

    public CustomerRequestsController(
        ICustomerRequestService customerRequestService)
    {
        _customerRequestService = customerRequestService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<CustomerRequestListDto>>> GetAll(
        [FromQuery] CustomerRequestFilterDto filter)
    {
        var result = await _customerRequestService.GetAllAsync(filter);

        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<CustomerRequestDto>> GetById(long id)
    {
        var result = await _customerRequestService.GetByIdAsync(id);

        return Ok(result);
    }

    [HttpGet("customer/{customerId:long}")]
    public async Task<ActionResult<CustomerRequestLookupDto>> GetLookup(
        long customerId,[FromQuery] DateOnly deliveryDate)
    {
        var result = await _customerRequestService
            .GetCustomerRequestLookupAsync(customerId, deliveryDate);

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<long>> Create(
        [FromBody] CreateCustomerRequestDto dto)
    {
        var id = await _customerRequestService.CreateAsync(dto);

        return Ok(id);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] UpdateCustomerRequestDto dto)
    {
        await _customerRequestService.UpdateAsync(id, dto);

        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        await _customerRequestService.DeleteAsync(id);

        return NoContent();
    }
}