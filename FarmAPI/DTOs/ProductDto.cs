using System.ComponentModel.DataAnnotations;

namespace FarmAPI.DTOs
{
    public class ProductDto
    {
        public class CreateProductRequestDto
        {
            public string ProductCode { get; set; } = string.Empty;

            public string ProductName { get; set; } = string.Empty;

            public short CategoryId { get; set; }

            public decimal? LitresPerUnit { get; set; }

            public bool TrackInventory { get; set; }

            public int DisplayOrder { get; set; }

            public decimal SellingPrice { get; set; }
        }

        public class UpdateProductRequestDto
        {
            public string ProductCode { get; set; } = string.Empty;

            public string ProductName { get; set; } = string.Empty;

            public short CategoryId { get; set; }

            public decimal? LitresPerUnit { get; set; }

            public bool TrackInventory { get; set; }

            public int DisplayOrder { get; set; }

            public bool IsActive { get; set; }
        }

        public class ProductResponseDto
        {
            public long Id { get; set; }

            public string ProductCode { get; set; } = string.Empty;

            public string ProductName { get; set; } = string.Empty;

            public short CategoryId { get; set; }

            public string CategoryName { get; set; } = string.Empty;

            public decimal? LitresPerUnit { get; set; }

            public bool TrackInventory { get; set; }

            public int? DisplayOrder { get; set; }

            public bool IsActive { get; set; }
            public decimal? CurrentPrice { get; set; }

            public DateTime CreatedAt { get; set; }
        }       

        public class ProductSearchRequestDto : PaginationRequestDto
        {
            public string? SearchText { get; set; }

            public short? CategoryId { get; set; }

            public bool? IsActive { get; set; }
        }

        public class ProductPriceResponseDto
        {
            public long Id { get; set; }

            public decimal SellingPrice { get; set; }

            public DateOnly EffectiveFrom { get; set; }

            public DateTime CreatedAt { get; set; }
        }

        public class ProductCategoryDropdownDto
        {
            public long Id { get; set; }

            public string Name { get; set; } = string.Empty;

            public bool TrackInventoryDefault { get; set; }
        }
    }
}
