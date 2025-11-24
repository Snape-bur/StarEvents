namespace StarEvents.Models
{
    public class Discount
    {
        public int DiscountId { get; set; }
        public string Code { get; set; } = null!;
        public decimal Percentage { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public int? EventId { get; set; }
        public Event? Event { get; set; }


        public bool IsActive { get; set; }
    }

}
