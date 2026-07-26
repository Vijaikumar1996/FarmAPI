using FarmAPI.Data;
using FarmAPI.DTOs;
using FarmAPI.Interface;
using Microsoft.EntityFrameworkCore;

namespace FarmAPI.Services;

public class DashboardService : IDashboardService
{
    private readonly FarmDbContext _context;
    private readonly ICustomerHelperService _customerHelper;

    public DashboardService(FarmDbContext context, ICustomerHelperService customerHelper)
    {
        _context = context;
        _customerHelper = customerHelper;
    }

    public async Task<DashboardResponse> GetDashboardAsync()
    {
        var subscriptionCustomerIds = await GetSubscriptionCustomerIdsAsync();

        return new DashboardResponse
        {
            TodayRequests = await GetTodayRequestsAsync(),

            TodayCollection = await GetTodayCollectionAsync(),

            SubscriptionPendingCustomers =
                await GetSubscriptionPendingCustomersAsync(subscriptionCustomerIds),

            NonSubscriptionPendingCustomers =
                await GetNonSubscriptionPendingCustomersAsync(subscriptionCustomerIds),

            RecentRequests =
                await GetRecentRequestsAsync(),

            PendingPayments =
                await GetPendingPaymentsAsync(subscriptionCustomerIds)
        };
    }

    #region Summary Cards

    private async Task<int> GetTodayRequestsAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        return await _context.CustomerRequests
            .CountAsync(x =>
                DateOnly.FromDateTime(x.CreatedAt) == today);
    }

    private async Task<decimal> GetTodayCollectionAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        return await _context.Payments
            .Where(x =>
                DateOnly.FromDateTime(x.PaymentDate) == today)
            .SumAsync(x => (decimal?)x.Amount) ?? 0;
    }

    private async Task<HashSet<long>> GetSubscriptionCustomerIdsAsync()
    {
        return (await _context.CustomerSubscriptions
                .Where(x => x.IsActive)
                .Select(x => x.CustomerId)
                .Distinct()
                .ToListAsync())
            .ToHashSet();
    }

    private async Task<int> GetSubscriptionPendingCustomersAsync(
        HashSet<long> subscriptionCustomerIds)
    {
        var currentMonth = new DateOnly(
     DateTime.Today.Year,
     DateTime.Today.Month,
     1);

        return await _context.CustomerMonthlyLedgers
            .Where(x =>
                x.BillingMonth < currentMonth &&
                x.BalanceAmount > 0 &&
                subscriptionCustomerIds.Contains(x.CustomerId))
            .Select(x => x.CustomerId)
            .Distinct()
            .CountAsync();

    }

    private async Task<int> GetNonSubscriptionPendingCustomersAsync(
     HashSet<long> subscriptionCustomerIds)
    {
        return await _context.CustomerOutstanding
            .Where(x =>
                x.OutstandingAmount > 0 &&
                !subscriptionCustomerIds.Contains(x.CustomerId))
            .CountAsync();
    }

    #endregion

    #region Recent Requests

    private async Task<List<RecentRequestDto>> GetRecentRequestsAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var requests = await _context.CustomerRequests
            .Include(x => x.Customer)
            .Include(x => x.Product)
            .Where(x => DateOnly.FromDateTime(x.CreatedAt) == today)
            .OrderByDescending(x => x.CreatedAt)
            .Take(5)
            .ToListAsync();

        return requests.Select(x => new RecentRequestDto
        {
            CustomerName = x.Customer.CustomerName,
            Request = _customerHelper.GetCustomerRequestDescription(x),
            DeliveryDate = x.EffectiveFrom,
            CreatedAt = x.CreatedAt
        }).ToList();
    }

    #endregion

    #region Pending Payments

    private async Task<List<PendingPaymentDto>> GetPendingPaymentsAsync(
     HashSet<long> subscriptionCustomerIds)
    {
        var currentMonth = new DateOnly(
            DateTime.Today.Year,
            DateTime.Today.Month,
            1);

        // Subscription Customers
        var subscriptionPending = await _context.CustomerMonthlyLedgers
            .Include(x => x.Customer)
            .Where(x =>
                x.BillingMonth < currentMonth &&
                x.BalanceAmount > 0 &&
                subscriptionCustomerIds.Contains(x.CustomerId))
            .Select(x => new PendingPaymentDto
            {
                CustomerName = x.Customer!.CustomerName,
                BillingMonth = x.BillingMonth,
                PendingAmount = x.BalanceAmount
            })
            .ToListAsync();

        // Non Subscription Customers
        var nonSubscriptionPending = await _context.CustomerOutstanding
            .Include(x => x.Customer)
            .Where(x =>
                x.OutstandingAmount > 0 &&
                !subscriptionCustomerIds.Contains(x.CustomerId))
            .Select(x => new PendingPaymentDto
            {
                CustomerName = x.Customer!.CustomerName,
                BillingMonth = null,
                PendingAmount = x.OutstandingAmount
            })
            .ToListAsync();

        return subscriptionPending
            .Concat(nonSubscriptionPending)
            .OrderByDescending(x => x.PendingAmount)
            .Take(5)
            .ToList();
    }

    #endregion
}