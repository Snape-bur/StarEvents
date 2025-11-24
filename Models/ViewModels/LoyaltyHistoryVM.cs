using System.Collections.Generic;
using StarEvents.Models;

namespace StarEvents.Models.ViewModels
{
    public class LoyaltyHistoryVM
    {
        public int CurrentPoints { get; set; }
        public List<LoyaltyTransaction> Transactions { get; set; } = new();
    }
}
