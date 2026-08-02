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

        public string GetCustomerRequestDescription(CustomerRequest entity)
        {
            var productName = entity.Product?.ProductCode ?? "All Products";
            var quantity = entity.Quantity.HasValue
                            ? entity.Quantity.Value.ToString("0.##")
                            : "0";

            return entity.RequestAction.ToUpper() switch
            {
                "ADD" =>
                    $"Add - {productName} x {quantity}",

                "PAUSE" =>
                    $"Pause - {productName}",

                "REPLACE" =>
                    $"Replace - {productName} x {quantity}",

                _ =>
                    entity.RequestAction
            };
        }

        public  string FormatQuantity(decimal quantity)
        {
            return quantity.ToString("0.##");
        }
    }
}