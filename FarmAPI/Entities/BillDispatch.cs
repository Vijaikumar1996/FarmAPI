using System.ComponentModel.DataAnnotations.Schema;
namespace FarmAPI.Entities
{
   

    [Table("bill_dispatches")]
    public class BillDispatch
    {
        public int Id { get; set; }

        public long CustomerId { get; set; }

        public DateOnly BillingMonth { get; set; }

        public decimal TotalAmount { get; set; }

        public string BillStatus { get; set; } = "Pending";

        public DateTime? SentAt { get; set; }

        public long? SentBy { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}