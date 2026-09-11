namespace FarmAPI.DTOs
{
    public class DeliveryPlanningDto
    {

        public class DeliveryGenerationStatusDto
        {
            public DateOnly DeliveryDate { get; set; }

            public bool IsGenerated { get; set; }

            public int TotalDeliveries { get; set; }

            public DateTime? GeneratedAt { get; set; }

            public string? GeneratedBy { get; set; }
        }
        public class GenerateDeliveryRequest
        {
            public DateOnly DeliveryDate { get; set; }
        }

        public class GenerateDeliveryResponse
        {
            public bool Success { get; set; }

            public string Message { get; set; } = string.Empty;

            public int TotalRecords { get; set; }
        }

        public class FarmSummaryRequest
        {
            public DateOnly DeliveryDate { get; set; }

            public long? CategoryId { get; set; }
        }

        public class FarmSummaryDto
        {
            public long ProductId { get; set; }

            public string ProductCode { get; set; } = string.Empty;

            public string ProductName { get; set; } = string.Empty;

            public decimal Quantity { get; set; }

            public decimal? Litres { get; set; }
            public int? DisplayOrder { get; set; }
        }

        public class DriverLoadingDto
        {
            public long AreaId { get; set; }
            public string AreaCode { get; set; } = string.Empty;
            public string AreaName { get; set; } = string.Empty;

            public decimal TotalLitres { get; set; }

            public List<DriverLoadingItemDto> Products { get; set; } = new();
        }

        public class DriverLoadingItemDto
        {
            public long ProductId { get; set; }

            public string ProductCode { get; set; } = string.Empty;

            public string ProductName { get; set; } = string.Empty;

            public short CategoryId { get; set; }

            public string CategoryName { get; set; } = string.Empty;

            public decimal Quantity { get; set; }
        }

        public class DeliveryOrderDto
        {
            public string AreaCode { get; set; }

            public int? DeliveryOrder { get; set; }

            public string DeliveryLocation { get; set; }

            public string DeliveryLocationAddress { get; set; }

            public bool GroupDeliverySheetByLocation { get; set; }

            public bool ShowSpaceAfterLocation { get; set; }

            public List<DeliveryHouseDto> Houses { get; set; } = new();
        }

        public class DeliveryHouseDto
        {
            public string HouseDoorNo { get; set; }

            public List<DeliveryBoySheetDto> Customers { get; set; } = new();
        }

        public class DeliveryBoySheetDto
        {
            public long CustomerId { get; set; }

            public string AreaCode { get; set; }

            public string CustomerName { get; set; }

            public string DeliveryLocation { get; set; }

            public bool GroupDeliverySheetByLocation { get; set; }

            public string Address { get; set; }

            public string? DeliveryNotes { get; set; }
            public List<DeliveryBoyProductDto> MilkProducts { get; set; } = new();

            public List<DeliveryBoyProductDto> OtherProducts { get; set; } = new();
        }

        public class DeliveryBoyProductDto
        {
            public long ProductId { get; set; }

            public string ProductCode { get; set; }

            public decimal Quantity { get; set; }

            public int? DisplayOrder { get; set; }
        }

        public class ExpectedDeliveryDto
        {
            public long CustomerId { get; set; }

            public string CustomerName { get; set; } = string.Empty;

            public long? SubscriptionId { get; set; }

            public long ProductId { get; set; }

            public string ProductCode { get; set; } = string.Empty;

            public string ProductName { get; set; } = string.Empty;

            public decimal Quantity { get; set; }

            public string Source { get; set; } = string.Empty;

            public long? RequestId { get; set; }
        }

        public class DeliveryBoySheetPreviewDto
        {
            public string DeliveryDate { get; set; } = string.Empty;

            public List<DeliveryAreaPreviewDto> Areas { get; set; } = [];
        }

        public class DeliveryAreaPreviewDto
        {
            public string AreaCode { get; set; } = string.Empty;

            public bool ShowSpaceAfterLocation { get; set; }
            public List<DeliveryGroupPreviewDto> Groups { get; set; } = [];

            public List<LoadingSummaryPreviewDto> LoadingSummary { get; set; } = [];
        }

        public class DeliveryGroupPreviewDto
        {
            public List<DeliveryPreviewRowDto> Rows { get; set; } = [];

            public bool ShowDeliveryTotal { get; set; }

            public DeliveryTotalPreviewDto? DeliveryTotal { get; set; }
        }

        public class DeliveryPreviewRowDto
        {
            public long? CustomerId { get; set; }

            public string AreaCode { get; set; } = string.Empty;

            public string CustomerName { get; set; } = string.Empty;

            public string Address { get; set; } = string.Empty;

            public string Milk { get; set; } = string.Empty;

            public string OtherProducts { get; set; } = string.Empty;

            public string Remarks { get; set; } = string.Empty;

            public bool IsManual { get; set; }

            // Used only for UI highlighting.
            // This keeps Preview behavior closer to Excel.
            public bool HasOtherProducts { get; set; }
        }

        public class DeliveryTotalPreviewDto
        {
            public string Label { get; set; } = string.Empty;

            public string Value { get; set; } = string.Empty;
        }

        public class LoadingSummaryPreviewDto
        {
            public string Product { get; set; } = string.Empty;

            public string Quantity { get; set; } = string.Empty;
        }
    }
}
