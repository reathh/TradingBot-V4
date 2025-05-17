using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TradingBot.Data;
using TradingBot.Models;
using TradingBot.Services;

namespace TradingBot.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ITokenService _tokenService;

        public AuthController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ITokenService tokenService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
        }

        public class LoginDto { public string Email { get; set; } = null!; public string Password { get; set; } = null!; }
        public class RegisterDto { public string Email { get; set; } = null!; public string Password { get; set; } = null!; }
        public class UserDto { public string Id { get; set; } = null!; public string Email { get; set; } = null!; public IList<string> Roles { get; set; } = new List<string>(); }
        public class AuthResponse { public string Token { get; set; } = null!; public UserDto User { get; set; } = null!; }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var user = new User { UserName = dto.Email, Email = dto.Email };
            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            await _userManager.AddToRoleAsync(user, Roles.User);

            // Generate token
            var roles = await _userManager.GetRolesAsync(user);
            var token = _tokenService.CreateToken(user, roles);

            var userDto = new UserDto { Id = user.Id, Email = user.Email, Roles = roles };
            return Ok(new AuthResponse { Token = token, User = userDto });
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return Unauthorized("Invalid email or password.");

            var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, false);
            if (!result.Succeeded)
                return Unauthorized("Invalid email or password.");

            var roles = await _userManager.GetRolesAsync(user);
            var token = _tokenService.CreateToken(user, roles);

            var userDto = new UserDto { Id = user.Id, Email = user.Email, Roles = roles };
            return Ok(new AuthResponse { Token = token, User = userDto });
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();
            var roles = await _userManager.GetRolesAsync(user);
            var userDto = new UserDto { Id = user.Id, Email = user.Email, Roles = roles };
            return Ok(userDto);
        }
    }
}
