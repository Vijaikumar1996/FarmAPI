namespace FarmAPI.DTOs
{
    public class BillingDto
    {
        public class BillingFilterRequest
        {
            public DateOnly BillingMonth { get; set; }

            public long? CustomerId { get; set; }

            public long? AreaId { get; set; }

            public string? PaymentStatus { get; set; }
        }

        public class BillingSearchResponse
        {
            public BillingSummaryResponse Summary { get; set; } = new();

            public List<BillingListResponse> Items { get; set; } = new();
        }

        public class BillingSummaryResponse
        {
            public int CustomerCount { get; set; }

            public decimal TotalBill { get; set; }

            public decimal TotalCollected { get; set; }

            public decimal TotalOutstanding { get; set; }
        }

        public class BillingListResponse
        {
            public long BillingId { get; set; }

            public long CustomerId { get; set; }

            public string CustomerName { get; set; } = string.Empty;

            public string AreaCode { get; set; } = string.Empty;

            public string? DeliveryLocationName { get; set; }

            public DateOnly BillingMonth { get; set; }

            public decimal PreviousOutstanding { get; set; }

            public decimal ProductAmount { get; set; }

            public decimal DeliveryCharge { get; set; }

            public decimal AdjustmentAmount { get; set; }

            public decimal CurrentCharges { get; set; }

            public decimal TotalDue { get; set; }

            public decimal PaidAmount { get; set; }

            public decimal CurrentMonthBalance { get; set; }

            public decimal TotalOutstanding { get; set; }

            public string PaymentStatus { get; set; } = string.Empty;
        }     

        public class ReceivePaymentRequest
        {
            public long CustomerId { get; set; }

            public DateTime BillingMonth { get; set; }

            public DateTime PaymentDate { get; set; }

            public decimal Amount { get; set; }

            public string PaymentMode { get; set; } = string.Empty;

            public string? Remarks { get; set; }
        }

        public class BillingAdjustmentRequest
        {
            public long CustomerId { get; set; }

            public DateTime BillingMonth { get; set; }

            public DateTime AdjustmentDate { get; set; }

            public decimal Amount { get; set; }

            public string Reason { get; set; } = string.Empty;

            public string? Remarks { get; set; }
        }

        public class BillingDetailsResponse
        {
            public BillingSummaryDto Summary { get; set; } = new();

            public List<DeliveryDto> Deliveries { get; set; } = [];

            public List<PaymentDto> Payments { get; set; } = [];

            public List<AdjustmentDto> Adjustments { get; set; } = [];
        }

        public class BillingSummaryDto
        {
            public string CustomerName { get; set; } = string.Empty;

            public string AreaName { get; set; } = string.Empty;

            public string DeliveryLocation { get; set; } = string.Empty;

            public DateOnly BillingMonth { get; set; }

            public decimal PreviousOutstanding { get; set; }

            public decimal ProductAmount { get; set; }

            public decimal DeliveryCharge { get; set; }

            public decimal AdjustmentAmount { get; set; }

            public decimal CurrentCharges { get; set; }

            public decimal PaidAmount { get; set; }

            public decimal CurrentMonthBalance { get; set; }

            public decimal TotalOutstanding { get; set; }
        }

        public class DeliveryDto
        {
            public DateOnly DeliveryDate { get; set; }

            public string ProductName { get; set; } = string.Empty;

            public decimal Quantity { get; set; }

            public decimal Amount { get; set; }
        }

        public class PaymentDto
        {
            public DateTime PaymentDate { get; set; }

            public decimal Amount { get; set; }

            public string PaymentMode { get; set; } = string.Empty;

            public string? Remarks { get; set; }
        }

        public class AdjustmentDto
        {
            public DateTime AdjustmentDate { get; set; }

            public decimal Amount { get; set; }

            public string Reason { get; set; } = string.Empty;

            public string? Remarks { get; set; }
        }
    }
}