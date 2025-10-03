using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Orrento.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        public string AvatarUrl { get; set; }
        public string PhoneNumber { get; set; }
        public string Location { get; set; }
        public string Bio { get; set; }
        public string Role { get; set; } = "User";
        public bool IsVerified { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? LastActiveAt { get; set; }

        // Navigation property
        public ICollection<Item> Items { get; set; }
    }
}
