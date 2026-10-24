namespace FarmAPI.DTOs
{
    public class ReportDto
    {
      
        public class DeliveredItemsDto
        {
            public long ProductId { get; set; }
            public string ProductCode { get; set; } = "";
            public string ProductName { get; set; } = "";
            public int? DisplayOrder { get; set; }
            public decimal Quantity { get; set; }
            public decimal? Litres { get; set; }
        
    }
}
}
