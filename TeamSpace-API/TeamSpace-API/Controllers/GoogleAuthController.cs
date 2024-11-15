using Microsoft.AspNetCore.Mvc;
using TeamSpace_API.Data;
using Google.Apis.Auth;
using System.Threading.Tasks;
using TeamSpace_API.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System;
using System.Linq;
using TeamSpace_API.Requests;
using Microsoft.AspNetCore.Identity;
using System.Net.Http;
using System.IO;

namespace TeamSpace_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GoogleAuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public GoogleAuthController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            try
            {
                var clientId = _configuration["GoogleAuth:ClientId"];
                if (string.IsNullOrEmpty(clientId))
                {
                    throw new InvalidOperationException("Google ClientId is not configured.");
                }

                var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { clientId }
                });

                Console.WriteLine($"Google Payload Picture URL: {payload.Picture}");

                string userPhoto;
                if (!string.IsNullOrEmpty(payload.Picture))
                {
                    userPhoto = await ConvertImageToBase64(payload.Picture);
                    Console.WriteLine($"Photo converted to Base64: {userPhoto.Substring(0, 50)}...");
                }
                else
                {
                    userPhoto = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/wcAAwAB/7WbWz8AAAAASUVORK5CYII=";
                    Console.WriteLine("No photo URL found, using default.");
                }

                var user = _context.Users.FirstOrDefault(u => u.Email == payload.Email);

                if (user == null)
                {
                    user = new User
                    {
                        Nickname = payload.Name ?? "New User",
                        Email = payload.Email,
                        Password = "temporary_password",
                        Photo = userPhoto,
                        Description = "New user registered via Google",
                        Country = "UKR"
                    };
                    Console.WriteLine($"Saving new user with photo: {user.Photo.Substring(0, 50)}...");
                    _context.Users.Add(user);
                    _context.SaveChanges();

                    return Ok(new
                    {
                        needProfileUpdate = true,
                        email = user.Email,
                        message = "Account created. Please update your nickname and password."
                    });
                }

                if (!string.IsNullOrEmpty(payload.Picture))
                {
                    user.Photo = userPhoto;
                    Console.WriteLine($"Updating existing user photo: {user.Photo.Substring(0, 50)}...");
                }
                _context.SaveChanges();

                var jwtToken = GenerateJwtToken(user);

                return Ok(new
                {
                    token = jwtToken,
                    user = new
                    {
                        user.UserId,
                        user.Nickname,
                        user.Email,
                        user.Photo,
                        user.Description,
                        user.Country
                    }
                });
            }
            catch (InvalidJwtException)
            {
                return Unauthorized("Invalid Google token");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        private async Task<string> ConvertImageToBase64(string imageUrl)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
                    var base64Image = Convert.ToBase64String(imageBytes);

                    Console.WriteLine($"Image URL: {imageUrl}, Base64: {base64Image.Substring(0, 50)}...");

                    return base64Image;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting image: {ex.Message}");
                return GetDefaultBase64Photo();
            }
        }

        private string GetDefaultBase64Photo()
        {
            string defaultImage = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/wcAAwAB/7WbWz8AAAAASUVORK5CYII=";
            return defaultImage;
        }

        [HttpPost("update-profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] CompleteProfileRequest request)
        {
            try
            {
                var user = _context.Users.FirstOrDefault(u => u.Email == request.Email);
                if (user == null)
                {
                    return NotFound("User not found.");
                }

                user.Nickname = request.Nickname;
                user.Password = new PasswordHasher<User>().HashPassword(user, request.Password);
                _context.SaveChanges();

                var jwtToken = GenerateJwtToken(user);

                return Ok(new
                {
                    message = "Profile updated successfully.",
                    token = jwtToken,
                    user = new
                    {
                        user.UserId,
                        user.Nickname,
                        user.Email,
                        user.Photo,
                        user.Description,
                        user.Country
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating profile: {ex.Message}");
            }
        }

        private string GenerateJwtToken(User user)
        {
            var key = _configuration["JwtSettings:SecretKey"];
            if (string.IsNullOrEmpty(key) || key.Length < 32)
            {
                throw new ArgumentException("Secret key must be at least 32 characters long.");
            }

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(120),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}


