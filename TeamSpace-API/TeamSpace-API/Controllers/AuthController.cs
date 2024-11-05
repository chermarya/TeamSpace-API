using Microsoft.AspNetCore.Mvc;
using TeamSpace_API.Data;
using TeamSpace_API.Models;
using System.Linq;
using BCrypt.Net;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using TeamSpace_API.Requests;


namespace TeamSpace_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;


        public AuthController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            if (_context.Users.Any(u => u.Email == user.Email))
                return BadRequest("User already exists.");




            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();


            return Ok("User registered successfully.");
        }


        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest loginData)
        {
            var user = _context.Users.SingleOrDefault(u => u.Email == loginData.Email);


            if (user == null)
            {
                return Unauthorized("No such user found.");
            }


            if (!BCrypt.Net.BCrypt.Verify(loginData.Password, user.Password))
            {
                return Unauthorized("Incorrect password entered.");
            }


            var token = GenerateJwtToken(user);


            var userData = new
            {
                user.UserId,
                user.Nickname,
                user.Email,
                user.Photo,
                user.Description,
                user.Country,
                Token = token
            };


            return Ok(userData);
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

