using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SarfiyumAPI.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SarfiyumAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;

        public AccountController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;

        }

        [Authorize(Roles = "Admin")]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] Register model)
        {
            var userExists = await _userManager.FindByNameAsync(model.Username);
            if (userExists != null)
                return StatusCode(StatusCodes.Status500InternalServerError, new { Status = "Error", Message = "User already exists!" });
            IdentityUser user = new()
            {
                Email = model.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = model.Username
            };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
                return StatusCode(StatusCodes.Status500InternalServerError, new { Status = "Error", Message = "User creation failed! Please check user details and try again." });

            var userRoleName = "User";

            // Eğer "User" rolü yoksa oluştur
            if (!await _roleManager.RoleExistsAsync(userRoleName))
            {
                await _roleManager.CreateAsync(new IdentityRole(userRoleName));
            }

            // Yeni kullanıcıya "User" rolünü ata
            await _userManager.AddToRoleAsync(user, userRoleName);

            return Ok(new { Status = "Success", Message = "User created successfully!" });
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] Login model)
        {
            var user = await _userManager.FindByNameAsync(model.Username);
            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                var userRoles = await _userManager.GetRolesAsync(user);

                var authClaims = new List<Claim>
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.UserName!), // username
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim("userId", user.Id),
                    new Claim("email", user.Email!)
                };

                authClaims.AddRange(userRoles.Select(role => new Claim(ClaimTypes.Role, role)));

                var token = new JwtSecurityToken(
                    issuer: _configuration["Jwt:Issuer"],
                    claims: authClaims,
                    expires: DateTime.Now.AddMinutes(double.Parse(_configuration["Jwt:ExpiryInMinutes"])),
                    signingCredentials: new SigningCredentials(
                        new SymmetricSecurityKey(
                            System.Text.Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!)),
                        SecurityAlgorithms.HmacSha256)
                    );

                return Ok(new { Token = new JwtSecurityTokenHandler().WriteToken(token) });
            }
            return Unauthorized(new { Status = "Error", Message = "Invalid username or password!" });
        }


        [Authorize(Roles = "Admin")]
        [HttpPost("add-role")]
        public async Task<IActionResult> AddRole([FromBody] string roleName)
        {

            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
                if (result.Succeeded)
                {
                    return Ok(new { Status = "Success", Message = "Role created successfully!" });
                }
                return BadRequest(result.Errors);
            }
            return BadRequest("Role already exists");

        }

        [Authorize(Roles = "Admin")]
        [HttpPost("assign-role")]

        public async Task<IActionResult> AssignRole([FromBody] UserRole model)
        {
            var user = await _userManager.FindByNameAsync(model.Username);
            if (user == null)
            {
                return NotFound("User not found");
            }

            var result = await _userManager.AddToRoleAsync(user, model.Role);

            if (result.Succeeded)
            {
                return Ok(new { Status = "Success", Message = "Role assigned successfully!" });
            }
            return BadRequest(result.Errors);
        }

    }
}