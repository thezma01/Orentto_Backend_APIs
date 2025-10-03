using System;
using System.Collections.Generic;

namespace Orrento.DTOs
{
    public class ItemDTO
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string Condition { get; set; }
        public decimal PricePerDay { get; set; }
        public decimal? SecurityDeposit { get; set; }
        public string PickupLocation { get; set; }
        public int OwnerId { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> ImageUrls { get; set; }

        // Added for nearby filtering/display
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
