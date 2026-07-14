
using Farm.API.Services.Interfaces;
using FarmAPI.Data;
using FarmAPI.DTOs;
using FarmAPI.Utils;
using FarmManagement.Entities;
using Microsoft.EntityFrameworkCore;
using static FarmAPI.Utils.Constant;

namespace Farm.API.Services
{
    public class DeliveryVerificationService : IDeliveryVerificationService
    {
        private readonly FarmDbContext _context;

        public DeliveryVerificationService(FarmDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResponse<DeliveryVerificationListDto>> SearchAsync(
            DeliveryVerificationSearchRequestDto request)
        {
            var query =
                _context.DeliveryDetails
                    .AsNoTracking()
                    .Include(x => x.Customer)
                        .ThenInclude(x => x.Area)
                    .Include(x => x.Product)
                    .Where(x => x.DeliveryDate == request.DeliveryDate)
                    .AsQueryable();

            if (request.CustomerId.HasValue)
            {
                query = query.Where(x =>
                    x.CustomerId == request.CustomerId.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                query = query.Where(x => x.Status == request.Status);
            }

            var groupedQuery = query
                .GroupBy(x => new
                {
                    x.CustomerId,
                    x.Customer.CustomerName,
                    Area = x.Customer.Area.AreaCode,
                    x.DeliveryDate
                });

            var totalRecords = await groupedQuery.CountAsync();

            var items = await groupedQuery
                .OrderBy(x => x.Key.CustomerName)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(g => new DeliveryVerificationListDto
                {
                    CustomerId = g.Key.CustomerId,
                    CustomerName = g.Key.CustomerName,
                    Area = g.Key.Area,
                    DeliveryDate = g.Key.DeliveryDate,

                    PlannedItems = string.Join(", ",
    g.Where(x => x.PlannedQty > 0 || x.DeliveredQty > 0)
     .OrderBy(x => x.Product.DisplayOrder)
     .Select(x =>
         x.PlannedQty > 0
             ? $"{x.PlannedQty:0.##} {x.Product.ProductCode}"
             : $"Extra {x.Product.ProductCode}"
     )),

                    Status = g.Where(x => x.PlannedQty > 0 || x.DeliveredQty > 0)
          .All(x => x.Status == "DELIVERED") ? "DELIVERED" :
         g.Where(x => x.PlannedQty > 0 || x.DeliveredQty > 0)
          .All(x => x.Status == "NOT_DELIVERED") ? "NOT_DELIVERED" :
         g.Where(x => x.PlannedQty > 0 || x.DeliveredQty > 0)
          .Any(x => x.Status == "PARTIAL_DELIVERED" ||
                    x.Status == "NOT_DELIVERED")
            ? "PARTIAL_DELIVERED"
            : "PENDING"
                })
                .ToListAsync();

            return new PagedResponse<DeliveryVerificationListDto>
            {
                Items = items,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalRecords = totalRecords
            };
        }

        public async Task MarkAllDeliveredAsync(
            MarkAllDeliveredRequestDto request)
        {
            var deliveryDetails = await _context.DeliveryDetails
                .Where(x =>
                    x.DeliveryDate == request.DeliveryDate &&
                    x.Status == CustomerDeliveryStatus.Pending)
                .ToListAsync();

            if (!deliveryDetails.Any())
                return;

            foreach (var item in deliveryDetails)
            {
                item.DeliveredQty = item.PlannedQty;
                item.Status = CustomerDeliveryStatus.Delivered;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<DeliveryVerificationDto> GetAsync(
    long customerId,
    DateOnly deliveryDate)
        {
            var deliveryDetails = await _context.DeliveryDetails
    .Include(x => x.Customer)
        .ThenInclude(x => x.DeliveryLocation)
    .Include(x => x.Customer)
        .ThenInclude(x => x.Area)
    .Include(x => x.Product)
    .Where(x =>
        x.CustomerId == customerId &&
        x.DeliveryDate == deliveryDate)
    .ToListAsync();

            if (!deliveryDetails.Any())
            {
                throw new Exception("Delivery details not found.");
            }

            var first = deliveryDetails.First();

            return new DeliveryVerificationDto
            {
                CustomerId = first.CustomerId,

                CustomerName = first.Customer.CustomerName,

                Address = first.Customer.DeliveryLocation.LocationName + " " + first.Customer.HouseDoorNo + " " + first.Customer.DeliveryLocation.Address,

                DeliveryDate = first.DeliveryDate,

                Remarks = first.Remarks,

                Items = deliveryDetails
    .Where(x => !(x.PlannedQty == 0 && x.DeliveredQty == 0))
    .Select(x => new DeliveryVerificationItemDto
    {
        DeliveryDetailId = x.Id,

        ProductId = x.ProductId,

        ProductCode = x.Product.ProductCode,

        ProductName = x.Product.ProductName,

        PlannedQty = x.PlannedQty,

        DeliveredQty = x.DeliveredQty,

        Status = x.Status
    })
    .ToList()
            };
        }

        public async Task SaveAsync(
     SaveDeliveryVerificationRequestDto request)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var deliveryDetails = await _context.DeliveryDetails
                    .Where(x =>
                        x.CustomerId == request.CustomerId &&
                        x.DeliveryDate == request.DeliveryDate)
                    .ToListAsync();

                if (!deliveryDetails.Any())
                {
                    throw new Exception("Delivery details not found.");
                }

                var duplicateProducts = request.Items
    .GroupBy(x => x.ProductId)
    .Where(g => g.Count() > 1)
    .Select(g => g.Key)
    .ToList();

                if (duplicateProducts.Any())
                {
                    var productNames = await _context.Products
                        .Where(x => duplicateProducts.Contains(x.Id))
                        .Select(x => x.ProductName)
                        .ToListAsync();

                    throw new Exception(
                        $"Duplicate products found: {string.Join(", ", productNames)}");
                }

                // Load only newly added products
                var productIds = request.Items
    .Where(x =>
        !x.DeliveryDetailId.HasValue &&
        x.DeliveredQty > 0)
    .Select(x => x.ProductId)
    .Distinct()
    .ToList();

                var productPrices = await _context.ProductPrices
                    .Where(x =>
                        productIds.Contains(x.ProductId) &&
                        x.EffectiveFrom <= request.DeliveryDate)
                    .GroupBy(x => x.ProductId)
                    .Select(g => g
                        .OrderByDescending(x => x.EffectiveFrom)
                        .First())
                    .ToDictionaryAsync(x => x.ProductId);

                foreach (var item in request.Items)
                {
                    // Ignore newly added product with zero quantity
                    if (!item.DeliveryDetailId.HasValue &&
                        item.DeliveredQty <= 0)
                    {
                        continue;
                    }

                    // Existing Delivery Detail
                    if (item.DeliveryDetailId.HasValue)
                    {
                        var deliveryDetail = deliveryDetails
                            .FirstOrDefault(x =>
                                x.Id == item.DeliveryDetailId.Value);

                        if (deliveryDetail == null)
                        {
                            throw new Exception(
                                $"Delivery detail {item.DeliveryDetailId} not found.");
                        }

                        deliveryDetail.DeliveredQty = item.DeliveredQty;
                        deliveryDetail.Remarks = request.Remarks;

                        deliveryDetail.Status = GetDeliveryStatus(deliveryDetail.PlannedQty,item.DeliveredQty);
                    }
                    else
                    {
                        // Check whether the product already exists for this customer & delivery date
                        var existingDeliveryDetail = deliveryDetails
                            .FirstOrDefault(x => x.ProductId == item.ProductId);

                        if (existingDeliveryDetail != null)
                        {
                            existingDeliveryDetail.DeliveredQty = item.DeliveredQty;
                            existingDeliveryDetail.Remarks = request.Remarks;

                            existingDeliveryDetail.Status = GetDeliveryStatus(existingDeliveryDetail.PlannedQty,item.DeliveredQty);

                            continue;
                        }

                        if (!productPrices.TryGetValue(
                            item.ProductId,
                            out var productPrice))
                        {
                            throw new Exception(
                                $"Price not configured for Product {item.ProductId}.");
                        }

                        _context.DeliveryDetails.Add(
                            new DeliveryDetail
                            {
                                CustomerId = request.CustomerId,

                                DeliveryDate = request.DeliveryDate,

                                ProductId = item.ProductId,

                                PlannedQty = 0,

                                DeliveredQty = item.DeliveredQty,

                                UnitPrice = productPrice.SellingPrice,

                                Remarks = request.Remarks,

                                Status = CustomerDeliveryStatus.Delivered,

                                GeneratedAt = DateTime.UtcNow
                            });
                    }
                }

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private static string GetDeliveryStatus(
    decimal plannedQty,
    decimal deliveredQty)
        {
            if (deliveredQty <= 0)
                return CustomerDeliveryStatus.NotDelivered;

            if (deliveredQty < plannedQty)
                return CustomerDeliveryStatus.PartialDelivered;

            return CustomerDeliveryStatus.Delivered;
        }
    }
}