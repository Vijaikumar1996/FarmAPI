using System.ComponentModel.DataAnnotations;

namespace FarmAPI.DTOs
{
    public class DeliveryLocationDto
    {
        public class CreateDeliveryLocationRequest
        {
            [Required]
            public long AreaId { get; set; }

            [Required]
            [MaxLength(200)]
            public string LocationName { get; set; } = string.Empty;

            public string? Address { get; set; }

            public int DeliveryOrder { get; set; }
        }

        public class UpdateDeliveryLocationRequest
        {
            [Required]
            public long AreaId { get; set; }

            [Required]
            [MaxLength(200)]
            public string LocationName { get; set; } = string.Empty;

            public string? Address { get; set; }

            public int DeliveryOrder { get; set; }

            public bool IsActive { get; set; }
        }

        public class DeliveryLocationResponse
        {
            public long Id { get; set; }

            public long AreaId { get; set; }

            public string AreaCode { get; set; } = string.Empty;

            public string AreaName { get; set; } = string.Empty;

            public string LocationName { get; set; } = string.Empty;

            public int DeliveryOrder { get; set; }

            public string? Address { get; set; }

            public bool IsActive { get; set; }

            public DateTime CreatedAt { get; set; }
        }
    }
}