using FarmAPI.Data;
using Microsoft.EntityFrameworkCore;
using static FarmAPI.DTOs.ReportDto;

public class ReportsService : IReportsService
{
    private readonly FarmDbContext _context;

    public ReportsService(FarmDbContext context)
    {
        _context = context;
    }

    public async Task<List<DeliveredItemsDto>> GetDeliveredItemsAsync(
        DateOnly deliveryDate,
        short? categoryId)
    {
        var query = _context.DeliveryDetails
            .AsNoTracking()
            .Where(x =>
                x.DeliveryDate == deliveryDate &&
                x.DeliveredQty > 0);

        if (categoryId.HasValue)
        {
            query = query.Where(x =>
                x.Product.CategoryId == categoryId.Value);
        }

        var result = await query
            .GroupBy(x => new
            {
                x.ProductId,
                x.Product.ProductCode,
                x.Product.ProductName,
                x.Product.LitresPerUnit,
                x.Product.DisplayOrder
            })
            .Select(g => new DeliveredItemsDto
            {
                ProductId = g.Key.ProductId,
                ProductCode = g.Key.ProductCode,
                ProductName = g.Key.ProductName,
                DisplayOrder = g.Key.DisplayOrder,

                // Actual delivered quantity
                Quantity = g.Sum(x => x.DeliveredQty),

                Litres = g.Key.LitresPerUnit == null
                    ? null
                    : g.Sum(x => x.DeliveredQty) *
                      g.Key.LitresPerUnit.Value
            })
            .ToListAsync();

        return result
            .OrderBy(x => x.DisplayOrder ?? int.MaxValue)
            .ThenBy(x => x.ProductCode)
            .ToList();
    }
}