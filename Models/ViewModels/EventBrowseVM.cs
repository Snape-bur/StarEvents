using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using StarEvents.Models;

namespace StarEvents.Models.ViewModels
{
    public class EventBrowseVM
    {
        public string? Keyword { get; set; }
        public int? CategoryId { get; set; }
        public int? VenueId { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }

        public List<SelectListItem> Categories { get; set; } = new();
        public List<SelectListItem> Venues { get; set; } = new();
        public List<Event> Results { get; set; } = new();

        public List<Discount>? Discounts { get; set; }

    }
}
