using System.ComponentModel.DataAnnotations;

namespace FarmAPI.DTOs
{
    public class CustomerDto
    {
        public class CreateCustomerRequest
        {            

            [Required]
            [MaxLength(200)]
            public string CustomerName { get; set; } = string.Empty;

            [Required]
            [MaxLength(20)]
            public string MobileNo { get; set; } = string.Empty;

            public string? AlternateMobileNo { get; set; }

            public string? Email { get; set; }

            [Required]
            public long AreaId { get; set; }

            public long? DeliveryLocationId { get; set; }

            [Required]
            [MaxLength(100)]
            public string HouseDoorNo { get; set; } = string.Empty;

            public string? Landmark { get; set; }

            public string? Remarks { get; set; }

            public string? DeliveryNotes { get; set; }

            public decimal? Latitude { get; set; }

            public decimal? Longitude { get; set; }
        }

        public class UpdateCustomerRequest
        {           

            [Required]
            [MaxLength(200)]
            public string CustomerName { get; set; } = string.Empty;

            [Required]
            [MaxLength(20)]
            public string MobileNo { get; set; } = string.Empty;

            public string? AlternateMobileNo { get; set; }

            public string? Email { get; set; }

            [Required]
            public long AreaId { get; set; }

            public long? DeliveryLocationId { get; set; }

            [Required]
            [MaxLength(100)]
            public string HouseDoorNo { get; set; } = string.Empty;

            public string? Landmark { get; set; }

            public string? Remarks { get; set; }

            public string? DeliveryNotes { get; set; }

            public decimal? Latitude { get; set; }

            public decimal? Longitude { get; set; }

            public bool IsActive { get; set; }
        }

        public class CustomerResponse
        {
            public long Id { get; set; }

            public string CustomerCode { get; set; } = string.Empty;

            public string CustomerName { get; set; } = string.Empty;

            public string MobileNo { get; set; } = string.Empty;

            public string? AlternateMobileNo { get; set; }

            public string? Email { get; set; }

            public long AreaId { get; set; }

            public string AreaCode { get; set; } = string.Empty;

            public string AreaName { get; set; } = string.Empty;

            public long? DeliveryLocationId { get; set; }

            public string? DeliveryLocationName { get; set; }

            public string HouseDoorNo { get; set; } = string.Empty;

            public string? Landmark { get; set; }

            public string? Remarks { get; set; }

            public string? DeliveryNotes { get; set; }

            public decimal? Latitude { get; set; }

            public decimal? Longitude { get; set; }

            public bool IsActive { get; set; }

            public DateTime CreatedAt { get; set; }
        }
    }
}