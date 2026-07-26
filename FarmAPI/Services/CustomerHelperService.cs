using FarmAPI.Data;
using FarmAPI.Entities;
using FarmAPI.Interface;
using Microsoft.EntityFrameworkCore;

namespace FarmAPI.Services
{
    public class CustomerHelperService : ICustomerHelperService
    {
        private readonly FarmDbContext _context;

        public CustomerHelperService(FarmDbContext context)
        {
            _context = context;
        }

        public async Task<HashSet<long>> GetSubscriptionCustomerIdsAsync()
        {
            return (await _context.CustomerSubscriptions
                    .Where(x => x.IsActive)
                    .Select(x => x.CustomerId)
                    .Distinct()
                    .ToListAsync())
                .ToHashSet();
        }

        public  string GetCustomerRequestDescription(
                CustomerRequest entity)
        {
            var productName = entity.Product?.ProductName ?? "All Products";

            return entity.RequestAction.ToUpper() switch
            {
                "ADD" =>
                    $"Add - {productName} x {entity.Quantity}",

                "PAUSE" =>
                    $"Pause - {productName}",

                "REPLACE" =>
                    $"Replace - {productName} x {entity.Quantity}",

                _ =>
                    entity.RequestAction
            };
        }
    }
}