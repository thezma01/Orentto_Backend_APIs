namespace Orrento.DTOs
{
    public class CreateRentalRequestDTO
    {
        public int ItemId { get; set; }
        public int RenterId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

}
