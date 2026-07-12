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

            public List<DriverLoadingItemDto> Products { get; set; } = new();
        }

        public class DriverLoadingItemDto
        {
            public long ProductId { get; set; }

            public string ProductCode { get; set; } = string.Empty;

            public string ProductName { get; set; } = string.Empty;

            public decimal Quantity { get; set; }
        }

        public class DeliveryBoySheetDto
        {
            public long CustomerId { get; set; }

            public string AreaCode { get; set; } = string.Empty;

            public string CustomerName { get; set; } = string.Empty;

            public string Address { get; set; } = string.Empty;

            public List<DeliveryBoyProductDto> MilkProducts { get; set; } = new();

            public List<DeliveryBoyProductDto> OtherProducts { get; set; } = new();
        }

        public class DeliveryBoyProductDto
        {
            public long ProductId { get; set; }

            public string ProductCode { get; set; } = string.Empty;

            public decimal Quantity { get; set; }

            public int? DisplayOrder { get; set; }
        }
    }
}
