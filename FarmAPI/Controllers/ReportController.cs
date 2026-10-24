using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IReportsService _reportsService;

    public ReportsController(IReportsService reportsService)
    {
        _reportsService = reportsService;
    }

    [HttpGet("delivered-items")]
    public async Task<IActionResult> GetDeliveredItems(
        [FromQuery] DateOnly deliveryDate,
        [FromQuery] short? categoryId)
    {
        var result =
            await _reportsService.GetDeliveredItemsAsync(
                deliveryDate,
                categoryId);

        return Ok(result);
    }
}