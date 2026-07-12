namespace FarmAPI.DTOs
{
    public class CustomerRequestDto
    {
        public class CustomerRequestFilterDto : PaginationRequestDto
        {
            public long? CustomerId { get; set; }

            public long? ProductId { get; set; }

            public string? RequestAction { get; set; }

            public string? Status { get; set; }

            public DateOnly? RequestDate { get; set; }

            public bool? IsActive { get; set; }
        }

        public class CustomerRequestListDto
        {
            public long Id { get; set; }

            public long CustomerId { get; set; }

            public string CustomerName { get; set; } = string.Empty;

            public string RequestAction { get; set; } = string.Empty;

            public string RequestDescription { get; set; } = string.Empty;

            public long? ProductId { get; set; }

            public string? ProductName { get; set; }

            public decimal? Quantity { get; set; }

            public DateOnly EffectiveFrom { get; set; }

            public DateOnly? EffectiveTo { get; set; }

            public string Status { get; set; } = string.Empty;

            public bool IsActive { get; set; }

            public bool CanEdit { get; set; }
        }

        public class CustomerRequestResponseDto 
        {
            public long Id { get; set; }

            public long CustomerId { get; set; }

            public string CustomerName { get; set; } = string.Empty;

            public string RequestAction { get; set; } = string.Empty;

            public long? ProductId { get; set; }

            public string? ProductName { get; set; }

            public decimal? Quantity { get; set; }

            public DateOnly EffectiveFrom { get; set; }

            public DateOnly? EffectiveTo { get; set; }

            public string? Remarks { get; set; }

            public string Status { get; set; } = string.Empty;

            public bool IsActive { get; set; }
        }

        public class CustomerRequestLookupDto
        {
            public CustomerLookupDto Customer { get; set; } = new();

            public List<CustomerSubscriptionLookupDto> Subscriptions { get; set; } = [];

            public List<ActiveCustomerRequestDto> Requests { get; set; } = [];
        }

        public class CustomerLookupDto
        {
            public long Id { get; set; }

            public string CustomerName { get; set; } = string.Empty;
        }

        public class CustomerSubscriptionLookupDto
        {
            public long SubscriptionId { get; set; }

            public long ProductId { get; set; }
            public string ProductName { get; set; }

            public int FrequencyId { get; set; }
            public string FrequencyName { get; set; }

            public int? IntervalDays { get; set; }

            public DateOnly StartDate { get; set; }
            public DateOnly? EndDate { get; set; }

            public bool IsActive { get; set; }

            public decimal Quantity { get; set; }

            // New
            public string ScheduleDescription { get; set; }

            // Existing
            public bool HasPendingRequest { get; set; }
            public string? PendingRequestAction { get; set; }
        }

        public class CreateCustomerRequestDto
        {
            public long CustomerId { get; set; }

            public string RequestAction { get; set; } = string.Empty;

            public long? ProductId { get; set; }

            public decimal? Quantity { get; set; }

            public DateOnly EffectiveFrom { get; set; }

            public DateOnly? EffectiveTo { get; set; }

            public string? Remarks { get; set; }

            public long? SubscriptionId { get; set; }
        }

        public class UpdateCustomerRequestDto
        {
            public long Id { get; set; }

            public long CustomerId { get; set; }

            public string RequestAction { get; set; } = string.Empty;

            public long? ProductId { get; set; }

            public decimal? Quantity { get; set; }

            public DateOnly EffectiveFrom { get; set; }

            public DateOnly? EffectiveTo { get; set; }

            public string? Remarks { get; set; }

            public bool IsActive { get; set; }

            public string Status { get; set; } = string.Empty;

            public long? SubscriptionId { get; set; }
        }

        public class ActiveCustomerRequestDto
        {
            public long Id { get; set; }

            public string RequestAction { get; set; } = string.Empty;

            public long? ProductId { get; set; }

            public string? ProductName { get; set; }

            public decimal? Quantity { get; set; }

            public DateOnly EffectiveFrom { get; set; }

            public DateOnly? EffectiveTo { get; set; }

            public string Status { get; set; } = string.Empty;

            public long? SubscriptionId { get; set; }

            public string RequestDescription { get; set; } = string.Empty;
        }
    }
}
