namespace Orrento.Models
{
    public class RentalRequest
    {
        public int Id { get; set; }

        public int ItemId { get; set; }

        public int RenterId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties (optional for EF)
        public Item Item { get; set; }
        public User Renter { get; set; }
    }

}
