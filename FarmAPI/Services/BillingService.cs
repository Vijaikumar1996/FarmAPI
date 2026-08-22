using FarmAPI.Data;
using FarmAPI.Entities;
using FarmAPI.Interface;
using Microsoft.EntityFrameworkCore;
using static FarmAPI.DTOs.BillingDto;
using static FarmAPI.Utils.Constant;

namespace FarmAPI.Services
{
    public partial class BillingService : IBillingService
    {
        private readonly FarmDbContext _context;
        private readonly ICurrentUserService _currentUser;

        private readonly ICustomerHelperService _customerHelper;

        public BillingService(
            FarmDbContext context,
            ICurrentUserService currentUser,
            ICustomerHelperService customerHelper)
        {
            _context = context;
            _currentUser = currentUser;
            _customerHelper = customerHelper;
        }

        public async Task<BillingSearchResponse> GetMonthlyBillingAsync(
      BillingFilterRequest request)
        {
            var billingMonth = new DateOnly(
                request.BillingMonth.Year,
                request.BillingMonth.Month,
                1);

            var subscriptionCustomerIds =
                await _customerHelper.GetSubscriptionCustomerIdsAsync();

            var query = _context.CustomerMonthlyLedgers
                .Where(x => x.BillingMonth == billingMonth);

            if (request.CustomerId.HasValue)
            {
                query = query.Where(x =>
                    x.CustomerId == request.CustomerId.Value);
            }

            // Customer Type Filter
            if (!string.IsNullOrWhiteSpace(request.CustomerType))
            {
                switch (request.CustomerType.ToUpper())
                {
                    case "SUBSCRIPTION":

                        query = query.Where(x =>
                            subscriptionCustomerIds.Contains(x.CustomerId));

                        break;

                    case "NON_SUBSCRIPTION":

                        query = query.Where(x =>
                            !subscriptionCustomerIds.Contains(x.CustomerId));

                        break;
                }
            }

            // Payment Status Filter
            if (!string.IsNullOrWhiteSpace(request.PaymentStatus))
            {
                switch (request.PaymentStatus.ToUpper())
                {
                    case "PAID":

                        query = query.Where(x =>
                            x.BalanceAmount <= 0);

                        break;

                    case "PENDING":

                        query = query.Where(x =>
                            x.BalanceAmount > 0);

                        break;
                }
            }

            var billingList = await query
                .OrderBy(x => x.Customer.CustomerName)
                .Select(x => new
                {
                    BillingId = x.Id,

                    CustomerId = x.CustomerId,

                    CustomerName = x.Customer.CustomerName,

                    AreaCode = x.Customer.Area.AreaCode,

                    DeliveryLocationName =
                        x.Customer.DeliveryLocation != null
                            ? x.Customer.DeliveryLocation.LocationName
                            : null,

                    HouseDoorNo = x.Customer.HouseDoorNo,

                    LocationAddress =
                        x.Customer.DeliveryLocation != null
                            ? x.Customer.DeliveryLocation.Address
                            : null,

                    DoorNoAtEnd =
                        x.Customer.DeliveryLocation != null
                            && x.Customer.DeliveryLocation.DoorNoAtEnd,

                    BillingMonth = x.BillingMonth,

                    ProductAmount = x.ProductAmount,

                    DeliveryCharge = x.DeliveryCharge,

                    AdjustmentAmount = x.AdjustmentAmount,

                    PaidAmount = x.PaidAmount,

                    CurrentMonthBalance = x.BalanceAmount
                })
                .ToListAsync();

            // Build the final response after EF has finished executing
            var billingItems = billingList
                .Select(x =>
                {
                    var addressParts = x.DoorNoAtEnd
                        ? new[]
                        {
                    $"{x.DeliveryLocationName} {x.HouseDoorNo}",
                    x.LocationAddress
                        }
                        : new[]
                        {
                    x.HouseDoorNo,
                    x.DeliveryLocationName,
                    x.LocationAddress
                        };

                    var address = string.Join(
                        ", ",
                        addressParts.Where(x =>
                            !string.IsNullOrWhiteSpace(x)));

                    return new BillingListResponse
                    {
                        BillingId = x.BillingId,

                        CustomerId = x.CustomerId,

                        CustomerName = x.CustomerName,                     

                        Address = address,

                        DeliveryLocationName = x.DeliveryLocationName,

                        BillingMonth = x.BillingMonth,

                        ProductAmount = x.ProductAmount,

                        DeliveryCharge = x.DeliveryCharge,

                        AdjustmentAmount = x.AdjustmentAmount,

                        PaidAmount = x.PaidAmount,

                        CurrentMonthBalance = x.CurrentMonthBalance
                    };
                })
                .ToList();

            var summary = new BillingSummaryResponse
            {
                CustomerCount = billingItems.Count,

                TotalBill = billingItems.Sum(x =>
                    x.ProductAmount +
                    x.DeliveryCharge +
                    x.AdjustmentAmount),

                TotalCollected = billingItems.Sum(x =>
                    x.PaidAmount),

                TotalOutstanding = billingItems.Sum(x =>
                    x.CurrentMonthBalance)
            };

            return new BillingSearchResponse
            {
                Summary = summary,

                Items = billingItems
            };
        }


        public async Task ReceivePaymentAsync(
    ReceivePaymentRequest request)
        {
            var billingMonth = new DateOnly(
                request.BillingMonth.Year,
                request.BillingMonth.Month,
                1);

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var ledger = await _context.CustomerMonthlyLedgers
                    .FirstOrDefaultAsync(x =>
                        x.CustomerId == request.CustomerId &&
                        x.BillingMonth == billingMonth);

                if (ledger == null)
                    throw new Exception("Monthly bill not found.");

                if (request.Amount <= 0)
                    throw new Exception("Payment amount should be greater than zero.");

                if (request.Amount > ledger.BalanceAmount)
                    throw new Exception("Payment amount cannot exceed current month balance.");

                var payment = new Payment
                {
                    CustomerId = request.CustomerId,

                    BillingMonth = billingMonth,

                    PaymentDate = request.PaymentDate,

                    Amount = request.Amount,

                    PaymentMode = request.PaymentMode.Trim(),

                    Remarks = request.Remarks?.Trim(),

                    CreatedAt = DateTime.UtcNow,

                    CreatedBy = _currentUser.UserId,

                    UpdatedAt = DateTime.UtcNow,

                    UpdatedBy = _currentUser.UserId,

                };

                _context.Payments.Add(payment);

                ledger.PaidAmount += request.Amount;

                ledger.BalanceAmount -= request.Amount;

                ledger.UpdatedAt = DateTime.UtcNow;

                ledger.UpdatedBy = _currentUser.UserId;

                var outstanding = await _context.CustomerOutstanding
                    .FirstOrDefaultAsync(x =>
                        x.CustomerId == request.CustomerId);

                if (outstanding == null)
                {
                    outstanding = new CustomerOutstanding
                    {
                        CustomerId = request.CustomerId,

                        OutstandingAmount = 0,

                        CreatedAt = DateTime.UtcNow,

                        CreatedBy = _currentUser.UserId
                    };

                    _context.CustomerOutstanding.Add(outstanding);
                }

                outstanding.OutstandingAmount -= request.Amount;

                outstanding.UpdatedAt = DateTime.UtcNow;

                outstanding.UpdatedBy = _currentUser.UserId;

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task AddAdjustmentAsync(
    BillingAdjustmentRequest request)
        {
            var billingMonth = new DateOnly(
                request.BillingMonth.Year,
                request.BillingMonth.Month,
                1);

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var ledger = await _context.CustomerMonthlyLedgers
                    .FirstOrDefaultAsync(x =>
                        x.CustomerId == request.CustomerId &&
                        x.BillingMonth == billingMonth);

                if (ledger == null)
                    throw new Exception("Monthly bill not found.");

                if (request.Amount == 0)
                    throw new Exception("Adjustment amount cannot be zero.");

                var adjustment = new BillingAdjustment
                {
                    CustomerId = request.CustomerId,

                    BillingMonth = billingMonth,

                    AdjustmentDate = request.AdjustmentDate,

                    Amount = request.Amount,

                    Reason = request.Reason.Trim(),

                    Remarks = request.Remarks?.Trim(),

                    CreatedAt = DateTime.UtcNow,

                    CreatedBy = _currentUser.UserId,

                    UpdatedAt = DateTime.UtcNow,

                    UpdatedBy = _currentUser.UserId
                };

                _context.BillingAdjustments.Add(adjustment);

                ledger.AdjustmentAmount += request.Amount;

                ledger.BalanceAmount += request.Amount;

                ledger.UpdatedAt = DateTime.UtcNow;

                ledger.UpdatedBy = _currentUser.UserId;

                var outstanding = await _context.CustomerOutstanding
                    .FirstOrDefaultAsync(x =>
                        x.CustomerId == request.CustomerId);

                if (outstanding == null)
                {
                    outstanding = new CustomerOutstanding
                    {
                        CustomerId = request.CustomerId,

                        OutstandingAmount = 0,

                        CreatedAt = DateTime.UtcNow,

                        CreatedBy = _currentUser.UserId
                    };

                    _context.CustomerOutstanding.Add(outstanding);
                }

                outstanding.OutstandingAmount += request.Amount;

                outstanding.UpdatedAt = DateTime.UtcNow;

                outstanding.UpdatedBy = _currentUser.UserId;

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<BillingDetailsResponse> GetBillingDetailsAsync(
     long customerId,
     DateOnly billingMonth)
        {
            billingMonth = new DateOnly(
                billingMonth.Year,
                billingMonth.Month,
                1);

            // Get monthly ledger details
            var ledger = await _context.CustomerMonthlyLedgers
                .Where(x =>
                    x.CustomerId == customerId &&
                    x.BillingMonth == billingMonth)
                .Select(x => new
                {
                    x,

                    CustomerName = x.Customer.CustomerName,

                    HouseDoorNo = x.Customer.HouseDoorNo,

                    LocationName = x.Customer.DeliveryLocation != null
                        ? x.Customer.DeliveryLocation.LocationName
                        : null,

                    LocationAddress = x.Customer.DeliveryLocation != null
                        ? x.Customer.DeliveryLocation.Address
                        : null,

                    DoorNoAtEnd = x.Customer.DeliveryLocation != null
                        && x.Customer.DeliveryLocation.DoorNoAtEnd,

                    AreaName = x.Customer.Area.AreaName,

                    DeliveryLocation =
                        x.Customer.DeliveryLocation != null
                            ? x.Customer.DeliveryLocation.LocationName
                            : string.Empty
                })
                .FirstOrDefaultAsync();

            if (ledger == null)
                throw new Exception("Monthly bill not found.");

            // Build address in C# instead of inside EF query
            var addressParts = ledger.DoorNoAtEnd
                ? new[]
                {
            $"{ledger.LocationName} {ledger.HouseDoorNo}",
            ledger.LocationAddress
                }
                : new[]
                {
            ledger.HouseDoorNo,
            ledger.LocationName,
            ledger.LocationAddress
                };

            var address = string.Join(
                ", ",
                addressParts.Where(x => !string.IsNullOrWhiteSpace(x)));

            // Get previous outstanding
            var previousOutstanding = await _context.CustomerMonthlyLedgers
                .Where(x =>
                    x.CustomerId == customerId &&
                    x.BillingMonth < billingMonth)
                .SumAsync(x => x.BalanceAmount);

            // Get deliveries
            var deliveries = await _context.DeliveryDetails
                .Where(x =>
                    x.CustomerId == customerId &&
                    x.BillingMonth == billingMonth &&
                    x.Status == CustomerDeliveryStatus.Delivered)
                .OrderBy(x => x.DeliveryDate)
                .Select(x => new DeliveryDto
                {
                    DeliveryDate = x.DeliveryDate,

                    ProductName = x.Product.ProductName,

                    Quantity = x.DeliveredQty,

                    Amount = x.DeliveredQty * x.UnitPrice
                })
                .ToListAsync();

            // Get payments
            var payments = await _context.Payments
                .Where(x =>
                    x.CustomerId == customerId &&
                    x.BillingMonth == billingMonth)
                .OrderBy(x => x.PaymentDate)
                .Select(x => new PaymentDto
                {
                    PaymentDate = x.PaymentDate,

                    Amount = x.Amount,

                    PaymentMode = x.PaymentMode,

                    Remarks = x.Remarks
                })
                .ToListAsync();

            // Get billing adjustments
            var adjustments = await _context.BillingAdjustments
                .Where(x =>
                    x.CustomerId == customerId &&
                    x.BillingMonth == billingMonth)
                .OrderBy(x => x.AdjustmentDate)
                .Select(x => new AdjustmentDto
                {
                    AdjustmentDate = x.AdjustmentDate,

                    Amount = x.Amount,

                    Reason = x.Reason,

                    Remarks = x.Remarks
                })
                .ToListAsync();

            // Calculate current charges
            var currentCharges =
                ledger.x.ProductAmount +
                ledger.x.DeliveryCharge +
                ledger.x.AdjustmentAmount;

            // Build response
            return new BillingDetailsResponse
            {
                Summary = new BillingSummaryDto
                {
                    CustomerName = ledger.CustomerName,

                    AreaName = address,

                    DeliveryLocation = ledger.DeliveryLocation,

                    BillingMonth = ledger.x.BillingMonth,

                    PreviousOutstanding = previousOutstanding,

                    ProductAmount = ledger.x.ProductAmount,

                    DeliveryCharge = ledger.x.DeliveryCharge,

                    AdjustmentAmount = ledger.x.AdjustmentAmount,

                    CurrentCharges = currentCharges,

                    PaidAmount = ledger.x.PaidAmount,

                    CurrentMonthBalance = ledger.x.BalanceAmount,

                    TotalOutstanding =
                        previousOutstanding +
                        ledger.x.BalanceAmount
                },

                Deliveries = deliveries,

                Payments = payments,

                Adjustments = adjustments
            };
        }

        public async Task<SummaryBillResponse> GetSummaryBillAsync(
     long customerId,
     DateOnly billingMonth)
        {
            billingMonth = new DateOnly(
                billingMonth.Year,
                billingMonth.Month,
                1);

            var ledger = await _context.CustomerMonthlyLedgers
                .Where(x =>
                    x.CustomerId == customerId &&
                    x.BillingMonth == billingMonth)
                .Select(x => new
                {
                    x,

                    CustomerName = x.Customer.CustomerName,

                    MobileNo = x.Customer.MobileNo,

                    HouseDoorNo = x.Customer.HouseDoorNo,

                    LocationName = x.Customer.DeliveryLocation != null
                        ? x.Customer.DeliveryLocation.LocationName
                        : null,

                    LocationAddress = x.Customer.DeliveryLocation != null
                        ? x.Customer.DeliveryLocation.Address
                        : null,

                    DoorNoAtEnd = x.Customer.DeliveryLocation != null
                        && x.Customer.DeliveryLocation.DoorNoAtEnd,

                    DeliveryLocation =
                        x.Customer.DeliveryLocation != null
                            ? x.Customer.DeliveryLocation.LocationName
                            : string.Empty
                })
                .FirstOrDefaultAsync();

            if (ledger == null)
                throw new Exception("Monthly bill not found.");

            // Build address in C# instead of inside EF query
            var addressParts = ledger.DoorNoAtEnd
                ? new[]
                {
            $"{ledger.LocationName} {ledger.HouseDoorNo}",
            ledger.LocationAddress
                }
                : new[]
                {
            ledger.HouseDoorNo,
            ledger.LocationName,
            ledger.LocationAddress
                };

            var address = string.Join(
                ", ",
                addressParts.Where(x => !string.IsNullOrWhiteSpace(x)));

            // Previous outstanding
            var previousOutstanding = await _context.CustomerMonthlyLedgers
                .Where(x =>
                    x.CustomerId == customerId &&
                    x.BillingMonth < billingMonth)
                .SumAsync(x => x.BalanceAmount);

            // Products
            var products = await _context.DeliveryDetails
                .Where(x =>
                    x.CustomerId == customerId &&
                    x.BillingMonth == billingMonth &&
                    x.DeliveredQty > 0)
                .GroupBy(x => new
                {
                    x.ProductId,
                    x.Product.ProductName,
                    x.UnitPrice
                })
                .Select(x => new SummaryBillItemDto
                {
                    ProductName = x.Key.ProductName,

                    Quantity = x.Sum(y => y.DeliveredQty),

                    UnitPrice = x.Key.UnitPrice,

                    Amount = x.Sum(y =>
                        y.DeliveredQty * y.UnitPrice),

                    TotalDays = x.Select(y => y.DeliveryDate)
                        .Distinct()
                        .Count()
                })
                .OrderBy(x => x.ProductName)
                .ToListAsync();

            // Current charges
            var currentCharges =
                ledger.x.ProductAmount +
                ledger.x.DeliveryCharge +
                ledger.x.AdjustmentAmount;

            return new SummaryBillResponse
            {
                Farm = new FarmInfoDto
                {
                    FarmName = "Dhariya Farms",

                    MobileNo = "9876543210",

                    BankName = "ICICI Bank",

                    AccountName = "Dhariya Farms",

                    AccountNumber = "610605032340",

                    IfscCode = "ICIC0001696",

                    UpiId = "7338861649@icici",

                    QrCodeUrl = null
                },

                Customer = new CustomerBillDto
                {
                    CustomerName = ledger.CustomerName,

                    MobileNo = ledger.MobileNo,

                    AreaName = address,

                    DeliveryLocation = ledger.DeliveryLocation,

                    BillingMonth = ledger.x.BillingMonth
                },

                Summary = new BillSummaryDto
                {
                    ProductAmount = ledger.x.ProductAmount,

                    DeliveryCharge = ledger.x.DeliveryCharge,

                    AdjustmentAmount = ledger.x.AdjustmentAmount,

                    PreviousOutstanding = previousOutstanding,

                    CurrentCharges = currentCharges,

                    PaidAmount = ledger.x.PaidAmount,

                    TotalOutstanding =
                        previousOutstanding +
                        ledger.x.BalanceAmount
                },

                Products = products
            };
        }
    }
}