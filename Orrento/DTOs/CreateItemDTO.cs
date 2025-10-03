using System.ComponentModel.DataAnnotations;

namespace Orrento.DTOs
{
    public class CreateItemDTO
    {
        [Required]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public string Category { get; set; }

        [Required]
        public string Condition { get; set; }

        [Required]
        public decimal PricePerDay { get; set; }

        public decimal? SecurityDeposit { get; set; }  // Optional

        [Required]
        public string PickupLocation { get; set; }

        [Required]
        public int OwnerId { get; set; }

        [Required(ErrorMessage = "Latitude is required")]
        public double Latitude { get; set; }

        [Required(ErrorMessage = "Longitude is required")]
        public double Longitude { get; set; }
    }
}
