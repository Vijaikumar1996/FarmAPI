using Farm.API.Enums;
using System.ComponentModel.DataAnnotations;

namespace FarmAPI.DTOs
{
    public class InventoryDto
    {
        public class InventorySummaryDto
        {
            public long Id { get; set; }

            public DateOnly StockDate { get; set; }

            public long ProductId { get; set; }

            public string ProductCode { get; set; } = string.Empty;

            public string ProductName { get; set; } = string.Empty;

            public decimal OpeningStock { get; set; }

            public decimal AvailableStock { get; set; }

            public string? Remarks { get; set; }
        }

        public class InventoryTransactionDto
        {
            public long Id { get; set; }

            public DateOnly TransactionDate { get; set; }

            public long ProductId { get; set; }

            public string ProductName { get; set; } = string.Empty;

            public InventoryTransactionType TransactionType { get; set; }

            public decimal Quantity { get; set; }

            public string? Remarks { get; set; }

            public long? ReferenceId { get; set; }

            public string? ReferenceType { get; set; }

            public DateTime CreatedAt { get; set; }
        }

        public class AddInventoryTransactionRequest
        {
            [Required]
            public DateOnly TransactionDate { get; set; }

            [Required]
            public long ProductId { get; set; }

            [Required]
            public InventoryTransactionType TransactionType { get; set; }

            [Range(0.01, 999999)]
            public decimal Quantity { get; set; }

            public string? Remarks { get; set; }

            public long? ReferenceId { get; set; }

            public string? ReferenceType { get; set; }
        }

        public class CreateDailyStockRequest
        {
            [Required]
            public DateOnly StockDate { get; set; }
        }

        public class InventoryFilterRequest
        {
            public DateOnly StockDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

            public long? ProductId { get; set; }
        }
    }
}
