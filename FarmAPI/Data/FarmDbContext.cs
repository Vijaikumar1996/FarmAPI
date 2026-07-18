using Farm.API.Entities;
using FarmAPI.Entities;
using FarmManagement.Entities;
using Microsoft.EntityFrameworkCore;

namespace FarmAPI.Data
{
    public class FarmDbContext : DbContext
    {
        public FarmDbContext(
            DbContextOptions<FarmDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();

        public DbSet<Area> Areas => Set<Area>();
        public DbSet<DeliveryLocation> DeliveryLocations => Set<DeliveryLocation>();
        public DbSet<Customer> Customers => Set<Customer>();

        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
        public DbSet<ProductPrice> ProductPrices => Set<ProductPrice>();

        public DbSet<CustomerSubscription> CustomerSubscriptions => Set<CustomerSubscription>();

        public DbSet<SubscriptionFrequency> SubscriptionFrequencies => Set<SubscriptionFrequency>();

        public DbSet<SubscriptionSchedule> SubscriptionSchedules => Set<SubscriptionSchedule>();

        public DbSet<InventoryDailySummary> InventoryDailySummaries { get; set; }

        public DbSet<InventoryTransaction> InventoryTransactions { get; set; }

        public DbSet<CustomerRequest> CustomerRequests { get; set; }

        public DbSet<DeliveryDetail> DeliveryDetails { get; set; }

        public DbSet<CustomerMonthlyLedger> CustomerMonthlyLedgers { get; set; }

        public DbSet<Payment> Payments { get; set; }

        public DbSet<BillingAdjustment> BillingAdjustments { get; set; }

        public DbSet<DeliveryChargeMaster> DeliveryChargeMasters { get; set; }

        public DbSet<CustomerOutstanding> CustomerOutstanding { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(FarmDbContext).Assembly);
        }
    }
}