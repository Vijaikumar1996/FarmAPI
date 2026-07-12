
using Farm.API.Entities;
using Farm.API.Enums;
using Farm.API.Interfaces;
using FarmAPI.Data;
using FarmAPI.Interface;
using Microsoft.EntityFrameworkCore;
using static FarmAPI.DTOs.InventoryDto;

namespace Farm.API.Services;

public class InventoryService : IInventoryService
{
    private readonly FarmDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public InventoryService(
        FarmDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    #region Inventory Summary

    public async Task<List<InventorySummaryDto>> GetInventoryAsync(
        InventoryFilterRequest request)
    {
        var query = _context.InventoryDailySummaries
            .AsNoTracking()
            .Include(x => x.Product)
            .Where(x => x.StockDate == request.StockDate);

        if (request.ProductId.HasValue)
        {
            query = query.Where(x => x.ProductId == request.ProductId.Value);
        }

        return await query
            .OrderBy(x => x.Product.DisplayOrder)
            .ThenBy(x => x.Product.ProductName)
            .Select(x => new InventorySummaryDto
            {
                Id = x.Id,

                StockDate = x.StockDate,

                ProductId = x.ProductId,

                ProductCode = x.Product.ProductCode,

                ProductName = x.Product.ProductName,

                OpeningStock = x.OpeningStock,

                AvailableStock = x.AvailableStock,

                Remarks = x.Remarks
            })
            .ToListAsync();
    }

    public async Task<InventorySummaryDto?> GetInventoryByProductAsync(
        long productId,
        DateOnly stockDate)
    {
        return await _context.InventoryDailySummaries
            .AsNoTracking()
            .Include(x => x.Product)
            .Where(x =>
                x.ProductId == productId &&
                x.StockDate == stockDate)
            .Select(x => new InventorySummaryDto
            {
                Id = x.Id,

                StockDate = x.StockDate,

                ProductId = x.ProductId,

                ProductCode = x.Product.ProductCode,

                ProductName = x.Product.ProductName,

                OpeningStock = x.OpeningStock,

                AvailableStock = x.AvailableStock,

                Remarks = x.Remarks
            })
            .FirstOrDefaultAsync();
    }

    #endregion

    public async Task CreateDailyStockAsync(DateOnly stockDate)
    {
        var alreadyCreated = await _context.InventoryDailySummaries
            .AnyAsync(x => x.StockDate == stockDate);

        if (alreadyCreated)
        {
            throw new Exception("Inventory already created for the selected date.");
        }

        var products = await _context.Products
            .AsNoTracking()
            .Where(x => x.IsActive && x.TrackInventory)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.ProductName)
            .ToListAsync();

        if (!products.Any())
        {
            throw new Exception("No inventory products found.");
        }

        var previousDate = stockDate.AddDays(-1);

        var previousStocks = await _context.InventoryDailySummaries
            .AsNoTracking()
            .Where(x => x.StockDate == previousDate)
            .ToDictionaryAsync(x => x.ProductId);

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var summaries = new List<InventoryDailySummary>();

            foreach (var product in products)
            {
                decimal openingStock = 0;

                if (previousStocks.TryGetValue(product.Id, out var yesterdayStock))
                {
                    openingStock = yesterdayStock.AvailableStock;
                }

                summaries.Add(new InventoryDailySummary
                {
                    StockDate = stockDate,

                    ProductId = product.Id,

                    OpeningStock = openingStock,

                    AvailableStock = openingStock,

                    CreatedAt = DateTime.UtcNow,

                    CreatedBy = _currentUser.UserId
                });
            }

            await _context.InventoryDailySummaries.AddRangeAsync(summaries);

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task AddTransactionAsync(AddInventoryTransactionRequest request)
    {
        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == request.ProductId &&
                x.IsActive);

        if (product == null)
        {
            throw new Exception("Product not found.");
        }

        var summary = await _context.InventoryDailySummaries
            .FirstOrDefaultAsync(x =>
                x.ProductId == request.ProductId &&
                x.StockDate == request.TransactionDate);

        if (summary == null)
        {
            throw new Exception("Daily inventory has not been created.");
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            var inventoryTransaction = new InventoryTransaction
            {
                TransactionDate = request.TransactionDate,

                ProductId = request.ProductId,

                TransactionType = request.TransactionType,

                Quantity = request.Quantity,

                Remarks = request.Remarks,

                ReferenceId = request.ReferenceId,

                ReferenceType = request.ReferenceType,

                CreatedAt = DateTime.UtcNow,

                CreatedBy = _currentUser.UserId
            };

            await _context.InventoryTransactions
                .AddAsync(inventoryTransaction);

            if (IsIncreaseTransaction(
        request.TransactionType,
        request.Quantity))
            {
                summary.AvailableStock += Math.Abs(request.Quantity);
            }
            else if (IsDecreaseTransaction(
                        request.TransactionType,
                        request.Quantity))
            {
                summary.AvailableStock -= Math.Abs(request.Quantity);
            }

            if (summary.AvailableStock < 0)
            {
                throw new Exception("Insufficient stock available.");
            }

            if (request.TransactionType ==
        InventoryTransactionType.Reservation &&
    summary.AvailableStock < request.Quantity)
            {
                throw new Exception(
                    $"{product.ProductName} has only {summary.AvailableStock} stock available.");
            }

            summary.UpdatedAt = DateTime.UtcNow;
            summary.UpdatedBy = _currentUser.UserId;

            _context.InventoryDailySummaries.Update(summary);

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<InventoryTransactionDto>> GetTransactionsAsync(
     long productId,
     DateOnly stockDate)
    {
        return await _context.InventoryTransactions
            .AsNoTracking()
            .Include(x => x.Product)
            .Where(x =>
                x.ProductId == productId &&
                x.TransactionDate == stockDate)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Select(x => new InventoryTransactionDto
            {
                Id = x.Id,

                TransactionDate = x.TransactionDate,

                ProductId = x.ProductId,

                ProductName = x.Product.ProductName,

                TransactionType = x.TransactionType,

                Quantity = x.Quantity,

                Remarks = x.Remarks,

                ReferenceId = x.ReferenceId,

                ReferenceType = x.ReferenceType,

                CreatedAt = x.CreatedAt
            })
            .ToListAsync();
    }

    private async Task<InventoryDailySummary?> GetInventorySummaryAsync(
    long productId,
    DateOnly stockDate)
    {
        return await _context.InventoryDailySummaries
            .FirstOrDefaultAsync(x =>
                x.ProductId == productId &&
                x.StockDate == stockDate);
    }



    private static bool IsIncreaseTransaction(
    InventoryTransactionType type,
    decimal quantity)
    {
        return type switch
        {
            InventoryTransactionType.Opening => true,

            InventoryTransactionType.StockIn => true,

            InventoryTransactionType.ReservationCancel => true,

            InventoryTransactionType.Adjustment => quantity > 0,

            _ => false
        };
    }

    private static bool IsDecreaseTransaction(
        InventoryTransactionType type,
        decimal quantity)
    {
        return type switch
        {
            InventoryTransactionType.Reservation => true,

            InventoryTransactionType.Waste => true,

            InventoryTransactionType.Adjustment => quantity < 0,

            _ => false
        };
    }
}