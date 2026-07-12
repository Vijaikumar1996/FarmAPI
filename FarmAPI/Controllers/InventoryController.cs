
using Farm.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static FarmAPI.DTOs.InventoryDto;

namespace Farm.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetInventory(
        [FromQuery] InventoryFilterRequest request)
    {
        var result = await _inventoryService.GetInventoryAsync(request);

        return Ok(result);
    }

    [HttpGet("{productId:long}")]
    public async Task<IActionResult> GetInventoryByProduct(
        long productId,
        [FromQuery] DateOnly stockDate)
    {
        var result = await _inventoryService.GetInventoryByProductAsync(
            productId,
            stockDate);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost("create-daily-stock")]
    public async Task<IActionResult> CreateDailyStock(
        CreateDailyStockRequest request)
    {
        await _inventoryService.CreateDailyStockAsync(request.StockDate);

        return Ok(new
        {
            message = "Daily inventory created successfully."
        });
    }

    [HttpPost("transaction")]
    public async Task<IActionResult> AddTransaction(
        AddInventoryTransactionRequest request)
    {
        await _inventoryService.AddTransactionAsync(request);

        return Ok(new
        {
            message = "Inventory transaction added successfully."
        });
    }

    [HttpGet("{productId:long}/transactions")]
    public async Task<IActionResult> GetTransactions(
        long productId,
        [FromQuery] DateOnly stockDate)
    {
        var result = await _inventoryService.GetTransactionsAsync(
            productId,
            stockDate);

        return Ok(result);
    }
}