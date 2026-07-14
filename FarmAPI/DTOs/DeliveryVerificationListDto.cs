namespace FarmAPI.DTOs
{
    public class DeliveryVerificationListDto
    {
        public long CustomerId { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string Area { get; set; } = string.Empty;

        public DateOnly DeliveryDate { get; set; }

        public string PlannedItems { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
    }
}
