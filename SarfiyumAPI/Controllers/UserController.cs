using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SarfiyumAPI.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SarfiyumAPI.Controllers
{
    [Authorize(Roles = "User")]
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;

        public UserController(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        [Authorize(Roles = "User")]
        [HttpGet]
        public IActionResult Get()
        {
            // Header'dan token al
            var tokenString = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(tokenString);

            // Claim'leri al
            var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            var username = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
            var email = jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
            var roles = jwtToken.Claims
                            .Where(c => c.Type == ClaimTypes.Role)
                            .Select(c => c.Value)
                            .ToList();

            return Ok(new
            {
                Id = userId,
                Username = username,
                Email = email,
                Roles = roles
            });
        }

    }
}
