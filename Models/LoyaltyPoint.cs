namespace StarEvents.Models
{
    public class LoyaltyPoint
    {
        public int LoyaltyPointId { get; set; }

        public string UserId { get; set; } = null!;
        public AppUser User { get; set; } = null!;

        public int Points { get; set; }
        public DateTime LastUpdated { get; set; }
    }

}
