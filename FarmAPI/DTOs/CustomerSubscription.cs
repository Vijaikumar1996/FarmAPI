namespace FarmAPI.DTOs
{

       public class CreateCustomerSubscriptionDto
        {
            public long CustomerId { get; set; }

            public long ProductId { get; set; }          

            public short FrequencyId { get; set; }

            public DateOnly StartDate { get; set; }

            public DateOnly? EndDate { get; set; }

            public bool IsActive { get; set; } = true;

        public short? IntervalDays { get; set; }

        public List<SubscriptionScheduleDto> Schedules { get; set; } = [];
        }

        public class UpdateCustomerSubscriptionDto
        {
            public long Id { get; set; }

            public long CustomerId { get; set; }

            public long ProductId { get; set; }           

            public short FrequencyId { get; set; }

            public DateOnly StartDate { get; set; }

            public DateOnly? EndDate { get; set; }

            public bool IsActive { get; set; }

        public short? IntervalDays { get; set; }

        public List<SubscriptionScheduleDto> Schedules { get; set; } = [];
        }

        public class CustomerSubscriptionDto
        {
            public long Id { get; set; }

            public long CustomerId { get; set; }

            public string CustomerName { get; set; } = string.Empty;

            public long ProductId { get; set; }

            public string ProductName { get; set; } = string.Empty;           

            public short FrequencyId { get; set; }

            public string FrequencyName { get; set; } = string.Empty;

            public DateOnly StartDate { get; set; }

            public DateOnly? EndDate { get; set; }

            public bool IsActive { get; set; }

        public short? IntervalDays { get; set; }

        public List<SubscriptionScheduleDto> Schedules { get; set; } = [];
        }

    public class CustomerSubscriptionListDto
    {
        public long Id { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;

        public string FrequencyName { get; set; } = string.Empty;

        public string ScheduleSummary { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }

    public class SubscriptionScheduleDto
    {
        public short? DayOfWeek { get; set; }

        public short? DayOfMonth { get; set; }

        public short? PatternOrder { get; set; }

        public decimal Quantity { get; set; }
    }

    public class CustomerSubscriptionFilterDto : PaginationRequestDto
        {
            public long? CustomerId { get; set; }

            public long? ProductId { get; set; }

            public bool? IsActive { get; set; }
        }
    }
