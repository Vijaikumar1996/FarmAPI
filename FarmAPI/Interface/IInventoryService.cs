
using static FarmAPI.DTOs.InventoryDto;

namespace Farm.API.Interfaces;

public interface IInventoryService
{
    Task<List<InventorySummaryDto>> GetInventoryAsync(InventoryFilterRequest request);

    Task<InventorySummaryDto?> GetInventoryByProductAsync(
        long productId,
        DateOnly stockDate);

    Task CreateDailyStockAsync(DateOnly stockDate);

    Task AddTransactionAsync(AddInventoryTransactionRequest request);

    Task<List<InventoryTransactionDto>> GetTransactionsAsync(
        long productId,
        DateOnly stockDate);
}