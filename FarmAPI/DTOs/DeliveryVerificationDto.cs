namespace FarmAPI.DTOs
{
    public class DeliveryVerificationSearchRequestDto : PaginationRequestDto
    {
        public DateOnly DeliveryDate { get; set; }

        public long? CustomerId { get; set; }

        public string? Status { get; set; }
    }

    public class MarkAllDeliveredRequestDto
    {
        public DateOnly DeliveryDate { get; set; }
    }

    public class DeliveryVerificationDto
    {
        public long CustomerId { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public DateOnly DeliveryDate { get; set; }

        public string? Remarks { get; set; }

        public List<DeliveryVerificationItemDto> Items { get; set; } = [];
    }

    public class DeliveryVerificationItemDto
    {
        public long DeliveryDetailId { get; set; }

        public long ProductId { get; set; }

        public string ProductCode { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;

        public decimal PlannedQty { get; set; }

        public decimal DeliveredQty { get; set; }

        public string Status { get; set; } = string.Empty;
    }

    public class SaveDeliveryVerificationRequestDto
    {
        public long CustomerId { get; set; }

        public DateOnly DeliveryDate { get; set; }

        public string? Remarks { get; set; }

        public List<SaveDeliveryVerificationItemDto> Items { get; set; } = [];
    }

    public class SaveDeliveryVerificationItemDto
    {
        public long? DeliveryDetailId { get; set; }

        public long ProductId { get; set; }

        public decimal DeliveredQty { get; set; }
    }

    public class AdditionalProductDto
    {
        public long ProductId { get; set; }

        public decimal DeliveredQty { get; set; }
    }


}
