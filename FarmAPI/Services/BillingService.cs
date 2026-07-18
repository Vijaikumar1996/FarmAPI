using FarmAPI.Data;
using FarmAPI.Entities;
using FarmAPI.Interface;
using Microsoft.EntityFrameworkCore;
using static FarmAPI.DTOs.BillingDto;

namespace FarmAPI.Services
{
    public partial class BillingService : IBillingService
    {
        private readonly FarmDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public BillingService(
            FarmDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<BillingSearchResponse> GetMonthlyBillingAsync(
     BillingFilterRequest request)
        {
            var billingMonth = new DateOnly(
                request.BillingMonth.Year,
                request.BillingMonth.Month,
                1);

            var query = _context.CustomerMonthlyLedgers
                .Where(x => x.BillingMonth == billingMonth);

            if (request.CustomerId.HasValue)
            {
                query = query.Where(x =>
                    x.CustomerId == request.CustomerId.Value);
            }

            if (request.AreaId.HasValue)
            {
                query = query.Where(x =>
                    x.Customer.AreaId == request.AreaId.Value);
            }

            var billingList = await query

                .OrderBy(x => x.Customer.CustomerName)

                .Select(x => new BillingListResponse
                {
                    BillingId = x.Id,

                    CustomerId = x.CustomerId,

                    CustomerName = x.Customer.CustomerName,

                    AreaCode = x.Customer.Area.AreaCode,

                    DeliveryLocationName =
                        x.Customer.DeliveryLocation != null
                            ? x.Customer.DeliveryLocation.LocationName
                            : null,

                    BillingMonth = x.BillingMonth,

                    ProductAmount = x.ProductAmount,

                    DeliveryCharge = x.DeliveryCharge,

                    AdjustmentAmount = x.AdjustmentAmount,

                    PaidAmount = x.PaidAmount,

                    CurrentMonthBalance = x.BalanceAmount
                })

                .ToListAsync();

            var summary = new BillingSummaryResponse
            {
                CustomerCount = billingList.Count,

                TotalBill = billingList.Sum(x =>
                    x.ProductAmount +
                    x.DeliveryCharge +
                    x.AdjustmentAmount),

                TotalCollected = billingList.Sum(x =>
                    x.PaidAmount),

                TotalOutstanding = billingList.Sum(x =>
                    x.CurrentMonthBalance)
            };

            return new BillingSearchResponse
            {
                Summary = summary,

                Items = billingList
            };
        }

    //    public async Task<BillingSummaryResponse> GetSummaryAsync(
    //DateOnly billingMonth)
    //    {
    //        billingMonth = new DateOnly(
    //            billingMonth.Year,
    //            billingMonth.Month,
    //            1);

    //        var summary = await _context.CustomerMonthlyLedgers

    //            .Where(x => x.BillingMonth == billingMonth)

    //            .GroupBy(x => 1)

    //            .Select(x => new BillingSummaryResponse
    //            {
    //                TotalBills = x.Count(),

    //                TotalCharges =
    //                    x.Sum(y =>
    //                        y.ProductAmount +
    //                        y.DeliveryCharge +
    //                        y.AdjustmentAmount),

    //                TotalCollected =
    //                    x.Sum(y => y.PaidAmount),

    //                TotalOutstanding =
    //                    x.Sum(y => y.BalanceAmount),

    //                PendingCustomers =
    //                    x.Count(y => y.BalanceAmount > 0)
    //            })

    //            .FirstOrDefaultAsync();

    //        return summary ?? new BillingSummaryResponse();
    //    }

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

            var ledger = await _context.CustomerMonthlyLedgers

                .Where(x =>
                    x.CustomerId == customerId &&
                    x.BillingMonth == billingMonth)

                .Select(x => new
                {
                    x,

                    CustomerName = x.Customer.CustomerName,

                    AreaName = x.Customer.Area.AreaName,

                    DeliveryLocation =
                        x.Customer.DeliveryLocation != null
                            ? x.Customer.DeliveryLocation.LocationName
                            : string.Empty
                })

                .FirstOrDefaultAsync();

            if (ledger == null)
                throw new Exception("Monthly bill not found.");

            var previousOutstanding = await _context.CustomerMonthlyLedgers

                .Where(x =>
                    x.CustomerId == customerId &&
                    x.BillingMonth < billingMonth)

                .SumAsync(x => x.BalanceAmount);

            //var billMonth = DateOnly.FromDateTime(billingMonth);

            var deliveries = await _context.DeliveryDetails
                .Where(x =>
                    x.CustomerId == customerId &&
                    x.BillingMonth == billingMonth)
                .OrderBy(x => x.DeliveryDate)
                .Select(x => new DeliveryDto
                {
                    DeliveryDate = x.DeliveryDate,
                    ProductName = x.Product.ProductName,
                    Quantity = x.DeliveredQty,
                    Amount = x.DeliveredQty * x.UnitPrice
                })
                .ToListAsync();

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

            var currentCharges =
                ledger.x.ProductAmount +
                ledger.x.DeliveryCharge +
                ledger.x.AdjustmentAmount;

            return new BillingDetailsResponse
            {
                Summary = new BillingSummaryDto
                {
                    CustomerName = ledger.CustomerName,

                    AreaName = ledger.AreaName,

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

        private static string GetPaymentStatus(
    decimal balance,
    decimal paidAmount)
        {
            if (balance <= 0)
                return "PAID";

            if (paidAmount > 0)
                return "PARTIAL";

            return "PENDING";
        }
    }
}