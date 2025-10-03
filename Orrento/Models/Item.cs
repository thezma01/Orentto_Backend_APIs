using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Orrento.Models
{
    public class Item
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Title { get; set; }

        [Required, MaxLength(1000)]
        public string Description { get; set; }

        [Required, MaxLength(50)]
        public string Category { get; set; }

        [Required]
        [MaxLength(30)]
        public string Condition { get; set; }  // New field

        [Required, Column(TypeName = "decimal(18,2)")]
        public decimal PricePerDay { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? SecurityDeposit { get; set; } // Optional

        [Required, MaxLength(255)]
        public string PickupLocation { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }


        [Required]
        public int OwnerId { get; set; }

        [ForeignKey("OwnerId")]
        public User Owner { get; set; }

        [MaxLength(2000)]
        public string ImageUrls { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
