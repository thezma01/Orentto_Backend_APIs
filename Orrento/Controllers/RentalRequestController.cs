using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Orrento.DTOs;
using Orrento.Models;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Orrento.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RentalRequestController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RentalRequestController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CreateRequest(CreateRentalRequestDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var renterIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(renterIdString, out int renterId))
                return Unauthorized(new { message = "Invalid user identity." });

            var item = await _context.Items.FindAsync(dto.ItemId);
            if (item == null)
                return NotFound(new { message = "Item not found." });

            if (item.OwnerId == renterId)
                return BadRequest(new { message = "You cannot request your own item." });

            bool isDuplicate = await _context.RentalRequests.AnyAsync(r =>
                r.ItemId == dto.ItemId &&
                r.RenterId == renterId &&
                r.Status == "Pending"
            );

            if (isDuplicate)
                return BadRequest(new { message = "You already have a pending request for this item." });

            bool hasDateConflict = await _context.RentalRequests.AnyAsync(r =>
                r.ItemId == dto.ItemId &&
                r.Status == "Approved" &&
                (dto.StartDate <= r.EndDate && dto.EndDate >= r.StartDate)
            );

            if (hasDateConflict)
                return BadRequest(new { message = "This item is already booked during the selected dates." });

            var request = new RentalRequest
            {
                ItemId = dto.ItemId,
                RenterId = renterId,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Status = "Pending"
            };

            _context.RentalRequests.Add(request);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Request sent successfully", requestId = request.Id });
        }

        [HttpGet("item/{itemId}")]
        public async Task<IActionResult> GetRequestsForItem(int itemId)
        {
            var requests = await _context.RentalRequests
                .Where(r => r.ItemId == itemId)
                .Include(r => r.Renter)
                .ToListAsync();

            return Ok(requests.Select(r => new
            {
                r.Id,
                r.Status,
                r.StartDate,
                r.EndDate,
                Renter = new
                {
                    r.Renter.Id,
                    r.Renter.FullName,
                    r.Renter.Email,
                    r.Renter.PhoneNumber
                }
            }));
        }

        [HttpPut("update-status/{id}")]
        public async Task<IActionResult> UpdateStatus(int id, [FromQuery] string status)
        {
            return await ChangeStatus(id, status);
        }

        [HttpGet("received/{ownerId}")]
        public async Task<IActionResult> GetRequestsReceivedByUser(int ownerId)
        {
            var requests = await _context.RentalRequests
                .Include(r => r.Item)
                    .ThenInclude(i => i.Owner)
                .Include(r => r.Renter)
                .Where(r => r.Item != null && r.Item.OwnerId == ownerId)
                .ToListAsync();

            var result = requests.Select(r => new
            {
                r.Id,
                r.Status,
                r.StartDate,
                r.EndDate,
                Item = new
                {
                    r.Item.Id,
                    r.Item.Title,
                    r.Item.PricePerDay,
                    ImageUrls = r.Item.ImageUrls?
                        .Split(new[] { ';', ',' }, System.StringSplitOptions.RemoveEmptyEntries)
                        ?? System.Array.Empty<string>()
                },
                Renter = new
                {
                    r.Renter.Id,
                    r.Renter.FullName,
                    r.Renter.Email,
                    r.Renter.PhoneNumber
                }
            });

            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> CancelRequest(int id)
        {
            var request = await _context.RentalRequests.FindAsync(id);
            if (request == null)
                return NotFound(new { message = "Rental request not found." });

            _context.RentalRequests.Remove(request);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Rental request cancelled successfully." });
        }

        [HttpPut("{id}/accept")]
        public async Task<IActionResult> AcceptRequest(int id)
        {
            return await ChangeStatus(id, "Approved");
        }

        [HttpPut("{id}/reject")]
        public async Task<IActionResult> RejectRequest(int id)
        {
            return await ChangeStatus(id, "Rejected");
        }

        private async Task<IActionResult> ChangeStatus(int id, string status)
        {
            var request = await _context.RentalRequests.FindAsync(id);
            if (request == null)
                return NotFound(new { message = "Rental request not found." });

            if (string.IsNullOrWhiteSpace(status))
                return BadRequest(new { message = "Status cannot be empty." });

            request.Status = status;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Status updated to {status}.", request });
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetRequestsByUser(int userId)
        {
            var requests = await _context.RentalRequests
                .Include(r => r.Item)
                    .ThenInclude(i => i.Owner)
                .Include(r => r.Renter)
                .Where(r => r.RenterId == userId)
                .ToListAsync();

            var result = requests.Select(r => new
            {
                r.Id,
                r.Status,
                r.StartDate,
                r.EndDate,
                Item = new
                {
                    r.Item.Id,
                    r.Item.Title,
                    r.Item.PricePerDay,
                    ImageUrls = r.Item.ImageUrls?
                        .Split(new[] { ';', ',' }, System.StringSplitOptions.RemoveEmptyEntries)
                        ?? System.Array.Empty<string>(),
                    Owner = new
                    {
                        r.Item.Owner.Id,
                        r.Item.Owner.FullName
                    }
                }
            });

            return Ok(result);
        }

        // ✅ NEW ENDPOINT: Delete Listed Item
        [HttpDelete("item/{itemId}")]
        public async Task<IActionResult> DeleteItem(int itemId)
        {
            var item = await _context.Items.FindAsync(itemId);
            if (item == null)
                return NotFound(new { message = "Item not found." });

            _context.Items.Remove(item);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Item deleted successfully." });
        }
    }
}
