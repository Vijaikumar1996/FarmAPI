using System.ComponentModel.DataAnnotations;

namespace FarmAPI.DTOs
{
    public class AreaDto
    {
        public class CreateAreaRequest
        {
            [Required]
            [MaxLength(10)]
            public string AreaCode { get; set; } = string.Empty;

            [Required]
            [MaxLength(100)]
            public string AreaName { get; set; } = string.Empty;
        }

        public class UpdateAreaRequest
        {
            [Required]
            [MaxLength(10)]
            public string AreaCode { get; set; } = string.Empty;

            [Required]
            [MaxLength(100)]
            public string AreaName { get; set; } = string.Empty;

            public bool IsActive { get; set; }
        }
    }
}
