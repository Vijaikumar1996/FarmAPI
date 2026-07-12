using FarmAPI.Services;
using FarmAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/subscription-frequencies")]
public class SubscriptionFrequencyController : ControllerBase
{
    private readonly ISubscriptionFrequencyService _service;

    public SubscriptionFrequencyController(
        ISubscriptionFrequencyService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();

        return Ok(result);
    }

    [HttpGet("dropdown")]
    public async Task<IActionResult> GetDropdown()
    {
        var result = await _service.GetDropdownAsync();

        return Ok(result);
    }
}