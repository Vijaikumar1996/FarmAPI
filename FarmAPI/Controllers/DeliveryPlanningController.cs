using FarmManagement.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static FarmAPI.DTOs.DeliveryPlanningDto;

namespace FarmManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DeliveryPlanningController : ControllerBase
{
    private readonly IDeliveryPlanningService _service;

    public DeliveryPlanningController(
        IDeliveryPlanningService service)
    {
        _service = service;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate(
        GenerateDeliveryRequest request)
    {       
        var result = await _service.GenerateDeliveryAsync(request);

        return Ok(result);
    }

    [HttpGet("farm-summary")]
    public async Task<IActionResult> GetFarmSummary(
    [FromQuery] DateOnly deliveryDate,
    [FromQuery] short? categoryId)
    {
        var result = await _service
            .GetFarmSummaryAsync(
                deliveryDate,
                categoryId);

        return Ok(result);
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(
    [FromQuery] DateOnly deliveryDate)
    {
        return Ok(await _service
            .GetGenerationStatusAsync(deliveryDate));
    }

    [HttpGet("driver-loading")]
    public async Task<IActionResult> GetDriverLoading(
     [FromQuery] DateOnly deliveryDate)
    {
        var result = await _service
            .GetDriverLoadingAsync(deliveryDate);

        return Ok(result);
    }

    [HttpGet("delivery-boy-sheet")]
    public async Task<IActionResult> GetDeliveryBoySheet(
    [FromQuery] DateOnly deliveryDate,
    [FromQuery] long? areaId)
    {
        var result = await _service
            .GetDeliveryBoySheetAsync(
                deliveryDate,
                areaId);

        return Ok(result);
    }

    [HttpGet("delivery-boy-sheet/export")]
    public async Task<IActionResult> ExportDeliveryBoySheet(
    [FromQuery] DateOnly deliveryDate,
    [FromQuery] long? areaId)
    {
        var file = await _service
            .ExportDeliveryBoySheetAsync(
                deliveryDate,
                areaId);

        var fileName = areaId.HasValue
            ? $"DeliveryBoy_{deliveryDate:ddMMyyyy}.xlsx"
            : $"DeliveryBoy_All_{deliveryDate:ddMMyyyy}.xlsx";

        return File(
            file,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }
}