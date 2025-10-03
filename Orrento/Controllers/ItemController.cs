using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Orrento.DTOs;
using Orrento.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Orrento.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ItemController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ItemController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: api/Item
        [HttpGet]
        public async Task<IActionResult> GetAllItems()
        {
            var items = await _context.Items
                .Include(i => i.Owner) // include the owner entity
                .ToListAsync();

            var result = items.Select(i => new
            {
                i.Id,
                i.Title,
                i.Description,
                i.Category,
                i.Condition,
                i.PricePerDay,
                i.SecurityDeposit,
                i.PickupLocation,
                i.Latitude,
                i.Longitude,
                i.CreatedAt,
                OwnerName = i.Owner != null ? i.Owner.FullName : "Unknown", // safe
                ImageUrls = i.ImageUrls?.Split(',') ?? Array.Empty<string>()
            });

            return Ok(result);
        }

        // GET: api/Item/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetItem(int id)
        {
            var item = await _context.Items
                .Include(i => i.Owner)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (item == null) return NotFound();

            var result = new
            {
                item.Id,
                item.Title,
                item.Description,
                item.Category,
                item.Condition,
                item.PricePerDay,
                item.SecurityDeposit,
                item.PickupLocation,
                item.Latitude,
                item.Longitude,
                item.CreatedAt,
                OwnerName = item.Owner != null ? item.Owner.FullName : "Unknown",
                ImageUrls = item.ImageUrls?.Split(',') ?? Array.Empty<string>()
            };

            return Ok(result);
        }

        // GET: api/Item/user
        [HttpGet("user")]
        [Authorize]
        public async Task<IActionResult> GetUserItems()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var items = await _context.Items
                .Include(i => i.Owner)
                .Where(i => i.OwnerId == userId)
                .ToListAsync();

            var result = items.Select(i => new
            {
                i.Id,
                i.Title,
                i.Description,
                i.Category,
                i.Condition,
                i.PricePerDay,
                i.SecurityDeposit,
                i.PickupLocation,
                i.Latitude,
                i.Longitude,
                i.CreatedAt,
                OwnerName = i.Owner != null ? i.Owner.FullName : "Unknown",
                ImageUrls = i.ImageUrls?.Split(',') ?? Array.Empty<string>()
            });

            return Ok(result);
        }

        // POST: api/Item
        [HttpPost]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateItem([FromForm] CreateItemDTO dto, [FromForm] List<IFormFile> images)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (images == null || images.Count == 0) return BadRequest("At least one image is required.");

            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var uploadsFolder = Path.Combine(_env.WebRootPath, "images");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var imageUrls = new List<string>();
                foreach (var image in images)
                {
                    var uniqueName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, uniqueName);
                    using var stream = new FileStream(filePath, FileMode.Create);
                    await image.CopyToAsync(stream);
                    imageUrls.Add($"/images/{uniqueName}");
                }

                var item = new Item
                {
                    Title = dto.Title,
                    Description = dto.Description,
                    Category = dto.Category,
                    Condition = dto.Condition,
                    PricePerDay = dto.PricePerDay,
                    SecurityDeposit = dto.SecurityDeposit,
                    PickupLocation = dto.PickupLocation,
                    Latitude = dto.Latitude,
                    Longitude = dto.Longitude,
                    OwnerId = userId,
                    CreatedAt = DateTime.Now,
                    ImageUrls = string.Join(",", imageUrls)
                };

                _context.Items.Add(item);
                await _context.SaveChangesAsync();

                // Return item including OwnerName
                var createdItem = await _context.Items
                    .Include(i => i.Owner)
                    .FirstOrDefaultAsync(i => i.Id == item.Id);

                return Ok(new
                {
                    createdItem.Id,
                    createdItem.Title,
                    createdItem.Description,
                    createdItem.Category,
                    createdItem.Condition,
                    createdItem.PricePerDay,
                    createdItem.SecurityDeposit,
                    createdItem.PickupLocation,
                    createdItem.Latitude,
                    createdItem.Longitude,
                    createdItem.CreatedAt,
                    OwnerName = createdItem.Owner != null ? createdItem.Owner.FullName : "Unknown",
                    ImageUrls = createdItem.ImageUrls?.Split(',') ?? Array.Empty<string>()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error creating item: {ex.Message}");
            }
        }
        // DELETE: api/Item/{itemId}
        [HttpDelete("{itemId}")]
        [Authorize]
        public async Task<IActionResult> DeleteItem(int itemId)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId))
                return Unauthorized(new { message = "Invalid user identity." });

            var item = await _context.Items.FindAsync(itemId);
            if (item == null)
                return NotFound(new { message = "Item not found." });

            // Check ownership
            if (item.OwnerId != userId)
                return Unauthorized(new { message = "You can only delete your own items." });

            // Optional: delete associated rental requests first
            var requests = _context.RentalRequests.Where(r => r.ItemId == itemId);
            _context.RentalRequests.RemoveRange(requests);

            _context.Items.Remove(item);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Item deleted successfully." });
        }

    }
}
