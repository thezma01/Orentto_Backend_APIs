using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Orrento.DTOs;
using Orrento.Models;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Orrento.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public UsersController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // ✅ Register Endpoint
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (_context.Users.Any(u => u.Email == dto.Email))
            {
                return BadRequest(new { message = "Email already registered." });
            }

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PasswordHash = hashedPassword,
                PhoneNumber = dto.PhoneNumber,
                Location = dto.Location,
                AvatarUrl = "",
                Bio = "", // default value
                Role = "User",
                IsVerified = false,
                CreatedAt = DateTime.UtcNow,
                LastActiveAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User registered successfully", userId = user.Id });
        }

        // ✅ Login Endpoint with JWT Token Added
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginUserDTO dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            user.LastActiveAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // ✅ Generate JWT token
            var jwtSettings = new JwtSettings
            {
                Key = "mY$up3rS3cur3JWTS3cr3tKey_WithEntropy123456!",
                Issuer = "OrrentoIssuer",
                Audience = "OrrentoAudience",
                ExpireMinutes = 60
            };

            var claims = new[]
            {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role)
    };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtSettings.Issuer,
                audience: jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(jwtSettings.ExpireMinutes),
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new
            {
                message = "Login successful",
                token = tokenString, // ✅ include this for Flutter
                userId = user.Id,
                userName = user.FullName,
                memberSince = user.CreatedAt
            });
        }

        // ✅ Get User Profile by ID
        [HttpGet("profile/{id}")]
        public async Task<IActionResult> GetProfile(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            var profileDto = new UserProfileDTO
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                AvatarUrl = user.AvatarUrl,
                PhoneNumber = user.PhoneNumber,
                Location = user.Location
            };

            return Ok(profileDto);
        }

        // ✅ Update Profile
        [HttpPut("update-profile/{id}")]
        public async Task<IActionResult> UpdateProfile(int id, [FromBody] UpdateProfileDTO dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            if (user.Email != dto.Email && _context.Users.Any(u => u.Email == dto.Email))
            {
                return BadRequest(new { message = "Email is already in use by another account." });
            }

            user.FullName = dto.FullName;
            user.Email = dto.Email;
            user.PhoneNumber = dto.PhoneNumber;
            user.Location = dto.Location;
            user.AvatarUrl = dto.AvatarUrl;
            user.LastActiveAt = DateTime.UtcNow;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Profile updated successfully." });
        }

        [HttpGet("dashboard/{userId}")]
        public async Task<IActionResult> GetUserDashboard(int userId)
        {
            // ✅ My Listed Items
            var listedItems = await _context.Items
                .Where(i => i.OwnerId == userId)
                .ToListAsync();

            // ✅ My Rental Requests
            var rentalRequests = await _context.RentalRequests
                .Where(r => r.RenterId == userId)
                .Include(r => r.Item)
                .ToListAsync();

            // ✅ Requests for My Items
            var receivedRequests = await _context.RentalRequests
                .Where(r => r.Item.OwnerId == userId)   // requests on my items
                .Include(r => r.Item)
                .Include(r => r.Renter) // include renter details
                .ToListAsync();

            return Ok(new
            {
                listedItems = listedItems.Select(item => new
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
                    ImageUrls = item.ImageUrls?.Split(';') ?? new string[0]
                }),

                rentalRequests = rentalRequests.Select(r => new
                {
                    r.Id,
                    r.Status,
                    r.StartDate,
                    r.EndDate,
                    Item = r.Item == null ? null : new
                    {
                        r.Item.Id,
                        r.Item.Title,
                        ImageUrls = r.Item.ImageUrls?.Split(';') ?? new string[0]
                    }
                }),

                receivedRequests = receivedRequests.Select(r => new
                {
                    r.Id,
                    r.Status,
                    r.StartDate,
                    r.EndDate,
                    Renter = r.Renter == null ? null : new
                    {
                        r.Renter.Id,
                        r.Renter.FullName,
                        r.Renter.Email
                    },
                    Item = r.Item == null ? null : new
                    {
                        r.Item.Id,
                        r.Item.Title,
                        ImageUrls = r.Item.ImageUrls?.Split(';') ?? new string[0]
                    }
                })
            });
        }

    }
}
