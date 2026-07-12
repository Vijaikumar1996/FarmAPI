using FarmAPI.DTOs;
using FarmAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace FarmAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/customer-subscriptions")]
public class CustomerSubscriptionController : ControllerBase
{
    private readonly ICustomerSubscriptionService _service;

    public CustomerSubscriptionController(
        ICustomerSubscriptionService service)
    {
        _service = service;
    }

    #region Get All

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] CustomerSubscriptionFilterDto filter)
    {
        var result = await _service.GetAllAsync(filter);

        return Ok(result);
    }

    #endregion

    #region Get By Id

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _service.GetByIdAsync(id);

        return Ok(result);
    }

    #endregion

    #region Create

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateCustomerSubscriptionDto dto)
    {
        var id = await _service.CreateAsync(dto);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new { Id = id });
    }

    #endregion

    #region Update

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] UpdateCustomerSubscriptionDto dto)
    {
        await _service.UpdateAsync(id, dto);

        return NoContent();
    }

    #endregion

    #region Delete

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        await _service.DeleteAsync(id);

        return NoContent();
    }

    #endregion
}