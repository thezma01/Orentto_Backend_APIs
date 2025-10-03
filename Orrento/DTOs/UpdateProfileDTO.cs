namespace Orrento.DTOs
{
    public class UpdateProfileDTO
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Location { get; set; }
        public string AvatarUrl { get; set; }
        public string Email { get; set; } // ✅ Added email
    }
}
