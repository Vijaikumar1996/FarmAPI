namespace FarmAPI.Utils
{
    public static class Constant
    {
        public static class SubscriptionFrequency
        {
            public const short Daily = 1;
            public const short Weekly = 2;
            public const short Monthly = 3;
            public const short Interval = 4;
        }

        public static class CustomerRequestAction
        {
            public const string Pause = "PAUSE";         
            public const string Replace = "REPLACE";
            public const string Add = "ADD";
        }

        public static class CustomerRequestStatus
        {
            public const string Pending = "PENDING";
            public const string InProgress = "INPROGRESS";
            public const string Processed = "PROCESSED";
            public const string Cancelled = "CANCELLED";
        }

        public static class CustomerDeliveryStatus
        {
            public const string Pending = "PENDING";
            public const string Delivered = "DELIVERED";
            public const string PartialDelivered = "PARTIAL_DELIVERED";
            public const string NotDelivered = "NOT_DELIVERED";
        }

        public static class ProductCategory
        {
            public const short Milk = 1;
            public const short Others = 2;
         
        }
        
    }
}
