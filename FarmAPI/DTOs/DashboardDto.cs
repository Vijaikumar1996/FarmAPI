namespace FarmAPI.DTOs
{
    public class DashboardResponse
    {
        public int TodayRequests { get; set; }

        public decimal TodayCollection { get; set; }

        public int SubscriptionPendingCustomers { get; set; }

        public int NonSubscriptionPendingCustomers { get; set; }

        public List<RecentRequestDto> RecentRequests { get; set; } = [];

        public List<PendingPaymentDto> PendingPayments { get; set; } = [];
    }

    public class RecentRequestDto
    {
        public string CustomerName { get; set; } = string.Empty;

        public string Request { get; set; } = string.Empty;

        public DateOnly DeliveryDate { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class PendingPaymentDto
    {
        public string CustomerName { get; set; } = string.Empty;

        public DateOnly? BillingMonth { get; set; }

        public decimal PendingAmount { get; set; }
    }
}
