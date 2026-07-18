
using Farm.API.Services.Interfaces;
using FarmAPI.Data;
using FarmAPI.DTOs;
using FarmAPI.Entities;
using FarmAPI.Interface;
using FarmAPI.Utils;
using FarmManagement.Entities;
using Microsoft.EntityFrameworkCore;
using static FarmAPI.Utils.Constant;

namespace Farm.API.Services
{
    public class DeliveryVerificationService : IDeliveryVerificationService
    {
        private readonly FarmDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public DeliveryVerificationService(FarmDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
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
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var billingMonth = GetBillingMonth(request.DeliveryDate);

                var deliveryDetails = await _context.DeliveryDetails
                    .Where(x =>
                        x.DeliveryDate == request.DeliveryDate &&
                        x.Status == CustomerDeliveryStatus.Pending)
                    .ToListAsync();

                if (!deliveryDetails.Any())
                    return;

                var customerIds = deliveryDetails
                    .Select(x => x.CustomerId)
                    .Distinct()
                    .ToList();

                var ledgerDictionary = await _context.CustomerMonthlyLedgers
                    .Where(x =>
                        x.BillingMonth == billingMonth &&
                        customerIds.Contains(x.CustomerId))
                    .ToDictionaryAsync(x => x.CustomerId);

                var outstandingDictionary = await _context.CustomerOutstanding
                    .Where(x => customerIds.Contains(x.CustomerId))
                    .ToDictionaryAsync(x => x.CustomerId);

                var currentDeliveryCharge =
                    await GetDeliveryChargeAsync();

                var subscriptionCustomers = (
                    await _context.CustomerSubscriptions
                        .Where(x =>
                            customerIds.Contains(x.CustomerId) &&
                            x.IsActive)
                        .Select(x => x.CustomerId)
                        .Distinct()
                        .ToListAsync())
                    .ToHashSet();

                foreach (var item in deliveryDetails)
                {
                    item.DeliveredQty = item.PlannedQty;

                    item.Status = CustomerDeliveryStatus.Delivered;

                    var amount = item.DeliveredQty * item.UnitPrice;

                    UpdateMonthlyLedger(
                        ledgerDictionary,
                        item,
                        amount);

                    UpdateCustomerOutstanding(
                        outstandingDictionary,
                        item,
                        amount);
                }

                foreach (var customerId in customerIds)
                {
                    if (subscriptionCustomers.Contains(customerId))
                        continue;

                    if (!ledgerDictionary.TryGetValue(customerId, out var ledger))
                        continue;

                    if (!outstandingDictionary.TryGetValue(customerId, out var outstanding))
                        continue;

                    UpdateDeliveryCharge(
                        ledger,
                        outstanding,
                        false,
                        true,
                        currentDeliveryCharge);
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
                var billingMonth = GetBillingMonth(request.DeliveryDate);

                var deliveryDetails = await _context.DeliveryDetails
                    .Where(x =>
                        x.CustomerId == request.CustomerId &&
                        x.DeliveryDate == request.DeliveryDate)
                    .ToListAsync();

                if (!deliveryDetails.Any())
                {
                    throw new Exception("Delivery details not found.");
                }

                // Duplicate Product Validation
                var duplicateProducts = request.Items
                    .GroupBy(x => x.ProductId)
                    .Where(x => x.Count() > 1)
                    .Select(x => x.Key)
                    .ToList();

                if (duplicateProducts.Any())
                {
                    var productNames = await _context.Products
                        .Where(x => duplicateProducts.Contains(x.Id))
                        .Select(x => x.ProductName)
                        .ToListAsync();

                    throw new Exception(
                        $"Duplicate products found : {string.Join(", ", productNames)}");
                }

                // Load prices only for newly added products
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
                    .Select(x => x
                        .OrderByDescending(y => y.EffectiveFrom)
                        .First())
                    .ToDictionaryAsync(x => x.ProductId);

                // Billing dictionaries
                var ledger = await GetOrCreateMonthlyLedgerAsync(
                    request.CustomerId,
                    billingMonth);

                var outstanding = await GetOrCreateCustomerOutstandingAsync(
                    request.CustomerId);

                var currentDeliveryCharge = await GetDeliveryChargeAsync();

                var isSubscriptionCustomer =
                    await IsSubscriptionCustomerAsync(
                        request.CustomerId);

                var oldDeliveryChargeApplicable =
    !isSubscriptionCustomer &&
    deliveryDetails.Any(x =>
        x.Status == CustomerDeliveryStatus.Delivered ||
        x.Status == CustomerDeliveryStatus.PartialDelivered);

                foreach (var item in request.Items)
                {
                    // Ignore newly added product with zero qty
                    if (!item.DeliveryDetailId.HasValue &&
                        item.DeliveredQty <= 0)
                    {
                        continue;
                    }

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

                        // Billing Update (uses OLD values)
                        UpdateBilling(
     ledger,
     outstanding,
     deliveryDetail,
     item.DeliveredQty);

                        // Update Delivery Detail
                        deliveryDetail.DeliveredQty = item.DeliveredQty;

                        deliveryDetail.Remarks = request.Remarks;

                        deliveryDetail.Status = GetDeliveryStatus(
                            deliveryDetail.PlannedQty,
                            item.DeliveredQty);
                    }
                    else
                    {
                        if (!productPrices.TryGetValue(
                            item.ProductId,
                            out var productPrice))
                        {
                            throw new Exception(
                                $"Price not configured for Product {item.ProductId}.");
                        }

                        var newDelivery = new DeliveryDetail
                        {
                            CustomerId = request.CustomerId,

                            DeliveryDate = request.DeliveryDate,

                            BillingMonth = GetBillingMonth(request.DeliveryDate),

                            ProductId = item.ProductId,

                            PlannedQty = 0,

                            DeliveredQty = 0,

                            UnitPrice = productPrice.SellingPrice,

                            Remarks = request.Remarks,

                            Status = CustomerDeliveryStatus.Pending,

                            GeneratedAt = DateTime.UtcNow,

                            GeneratedBy = _currentUser.UserId
                        };

                        // Billing Update
                        UpdateBilling(
    ledger,
    outstanding,
    newDelivery,
    item.DeliveredQty);

                        newDelivery.DeliveredQty = item.DeliveredQty;
                        newDelivery.Status = CustomerDeliveryStatus.Delivered;

                        _context.DeliveryDetails.Add(newDelivery);
                    }
                }

                var newDeliveryChargeApplicable =
    !isSubscriptionCustomer &&
    (
        deliveryDetails.Any(x => x.DeliveredQty > 0) ||
        request.Items.Any(x =>
            !x.DeliveryDetailId.HasValue &&
            x.DeliveredQty > 0)
    );

                UpdateDeliveryCharge(
    ledger,
    outstanding,
    oldDeliveryChargeApplicable,
    newDeliveryChargeApplicable,
    currentDeliveryCharge);

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

        private void UpdateMonthlyLedger(
     Dictionary<long, CustomerMonthlyLedger> ledgerDictionary,
     DeliveryDetail detail,
     decimal amount)
        {
            if (!ledgerDictionary.TryGetValue(detail.CustomerId, out var ledger))
            {
                ledger = new CustomerMonthlyLedger
                {
                    CustomerId = detail.CustomerId,

                    BillingMonth = detail.BillingMonth,

                    ProductAmount = 0,

                    DeliveryCharge = 0,

                    AdjustmentAmount = 0,

                    PaidAmount = 0,

                    BalanceAmount = 0,

                    CreatedAt = DateTime.UtcNow,

                    CreatedBy = _currentUser.UserId
                };

                ledgerDictionary.Add(detail.CustomerId, ledger);

                _context.CustomerMonthlyLedgers.Add(ledger);
            }

            ledger.ProductAmount += amount;

            ledger.BalanceAmount += amount;

            ledger.UpdatedAt = DateTime.UtcNow;

            ledger.UpdatedBy = _currentUser.UserId;
        }

        private void UpdateCustomerOutstanding(
     Dictionary<long, CustomerOutstanding> outstandingDictionary,
     DeliveryDetail detail,
     decimal amount)
        {
            if (!outstandingDictionary.TryGetValue(detail.CustomerId, out var outstanding))
            {
                outstanding = new CustomerOutstanding
                {
                    CustomerId = detail.CustomerId,

                    OutstandingAmount = 0,

                    CreatedAt = DateTime.UtcNow,

                    CreatedBy = _currentUser.UserId
                };

                outstandingDictionary.Add(detail.CustomerId, outstanding);

                _context.CustomerOutstanding.Add(outstanding);
            }

            outstanding.OutstandingAmount += amount;

            outstanding.UpdatedAt = DateTime.UtcNow;

            outstanding.UpdatedBy = _currentUser.UserId;
        }

        private async Task<decimal> GetDeliveryChargeAsync()
        {
            return await _context.DeliveryChargeMasters

                .Where(x => x.IsActive)

                .OrderByDescending(x => x.EffectiveFrom)

                .Select(x => x.DeliveryCharge)

                .FirstOrDefaultAsync();
        }


        private void UpdateBilling(
    CustomerMonthlyLedger ledger,
    CustomerOutstanding outstanding,
    DeliveryDetail deliveryDetail,
    decimal newDeliveredQty)
        {
            var differenceAmount = GetBillingDifference(
                deliveryDetail,
                newDeliveredQty);

            if (differenceAmount != 0)
            {
                ledger.ProductAmount += differenceAmount;

                ledger.BalanceAmount += differenceAmount;

                outstanding.OutstandingAmount += differenceAmount;
            }

            ledger.UpdatedAt = DateTime.UtcNow;
            ledger.UpdatedBy = _currentUser.UserId;

            outstanding.UpdatedAt = DateTime.UtcNow;
            outstanding.UpdatedBy = _currentUser.UserId;
        }

        private decimal GetBillingDifference(
    DeliveryDetail deliveryDetail,
    decimal newDeliveredQty)
        {
            var newAmount = CalculateAmount(
    newDeliveredQty,
    deliveryDetail.UnitPrice);

            if (deliveryDetail.Status == CustomerDeliveryStatus.Pending ||
                deliveryDetail.Status == CustomerDeliveryStatus.NotDelivered)
            {
                return newAmount;
            }

            var oldAmount =
                deliveryDetail.DeliveredQty *
                deliveryDetail.UnitPrice;

            return newAmount - oldAmount;
        }

        private decimal GetDeliveryChargeAmount(
    string status,
    decimal deliveryCharge)
        {
            if (status == CustomerDeliveryStatus.Delivered ||
                status == CustomerDeliveryStatus.PartialDelivered)
            {
                return deliveryCharge;
            }

            return 0;
        }

        private async Task<CustomerMonthlyLedger> GetOrCreateMonthlyLedgerAsync(
    long customerId,
    DateOnly billingMonth)
        {
            var ledger = await _context.CustomerMonthlyLedgers
                .FirstOrDefaultAsync(x =>
                    x.CustomerId == customerId &&
                    x.BillingMonth == billingMonth);

            if (ledger != null)
                return ledger;

            ledger = new CustomerMonthlyLedger
            {
                CustomerId = customerId,

                BillingMonth = billingMonth,

                ProductAmount = 0,

                DeliveryCharge = 0,

                AdjustmentAmount = 0,

                PaidAmount = 0,

                BalanceAmount = 0,

                CreatedAt = DateTime.UtcNow,

                CreatedBy = _currentUser.UserId
            };

            _context.CustomerMonthlyLedgers.Add(ledger);

            return ledger;
        }

        private async Task<CustomerOutstanding> GetOrCreateCustomerOutstandingAsync(
    long customerId)
        {
            var outstanding = await _context.CustomerOutstanding
                .FirstOrDefaultAsync(x =>
                    x.CustomerId == customerId);

            if (outstanding != null)
                return outstanding;

            outstanding = new CustomerOutstanding
            {
                CustomerId = customerId,

                OutstandingAmount = 0,

                CreatedAt = DateTime.UtcNow,

                CreatedBy = _currentUser.UserId
            };

            _context.CustomerOutstanding.Add(outstanding);

            return outstanding;
        }

        private static DateOnly GetBillingMonth(
    DateOnly deliveryDate)
        {
            return new DateOnly(
                deliveryDate.Year,
                deliveryDate.Month,
                1);
        }

        private static decimal CalculateAmount(
    decimal qty,
    decimal unitPrice)
        {
            return qty * unitPrice;
        }

        private async Task<bool> IsSubscriptionCustomerAsync(
    long customerId)
        {
            return await _context.CustomerSubscriptions
                .AnyAsync(x =>
                    x.CustomerId == customerId &&
                    x.IsActive);
        }

        private void UpdateDeliveryCharge(
    CustomerMonthlyLedger ledger,
    CustomerOutstanding outstanding,
    bool oldApplicable,
    bool newApplicable,
    decimal deliveryCharge)
        {
            decimal oldCharge =
                oldApplicable ? deliveryCharge : 0;

            decimal newCharge =
                newApplicable ? deliveryCharge : 0;

            var difference =
                newCharge - oldCharge;

            if (difference == 0)
                return;

            ledger.DeliveryCharge += difference;

            ledger.BalanceAmount += difference;

            outstanding.OutstandingAmount += difference;

            ledger.UpdatedAt = DateTime.UtcNow;
            ledger.UpdatedBy = _currentUser.UserId;

            outstanding.UpdatedAt = DateTime.UtcNow;
            outstanding.UpdatedBy = _currentUser.UserId;
        }

    }
}