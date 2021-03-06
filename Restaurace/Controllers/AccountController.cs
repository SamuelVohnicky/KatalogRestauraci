using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Restaurace.InputModels;
using Restaurace.Models;

namespace Restaurace.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private IConfiguration _config;
        private UserManager<ApplicationUser> _userManager;
        private SignInManager<ApplicationUser> _signInManager;

        public AccountController(IConfiguration config, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _config = config;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        [Authorize]
        public ActionResult<string> GetAccount()
        {
            var c = User.Claims.Where(c => c.Type == ClaimTypes.Name).FirstOrDefault();
            if (c != null)
            {
                return Ok(c.Value);
            }
            return NotFound();
        }

        [HttpGet("id")]
        [Authorize]
        public ActionResult<string> GetAccountId()
        {
            var c = User.Claims.Where(c => c.Type == ClaimTypes.NameIdentifier).FirstOrDefault();
            if (c != null)
            {
                return Ok(c.Value);
            }
            return NotFound();
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetAccount(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                return NotFound();
            }
            return Ok(new { Id = user.Id, UserName = user.UserName });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserIM userData)
        {
            var user = await _userManager.FindByNameAsync(userData.Email);
            if (user != null)
            {
                return BadRequest("user already registered");
            }
            var hasher = new PasswordHasher<ApplicationUser>();
            var newUser = new ApplicationUser
            {
                UserName = userData.Email,
                Email = userData.Email,
                EmailConfirmed = true,
                PasswordHash = hasher.HashPassword(null, userData.Password)
            };
            var result = await _userManager.CreateAsync(newUser);
            if (result.Succeeded)
            {
                return CreatedAtAction("GetAccount", new { id = newUser.Id }, newUser);
            }
            else
            {
                return BadRequest(result.Errors);
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserIM userData)
        {
            var result = await _signInManager.PasswordSignInAsync(userData.Email, userData.Password, false, false);
            if (result.Succeeded)
            {
                var user = await _userManager.FindByNameAsync(userData.Email);
                AuthorizationToken token = GenerateJSONWebToken(user);
                return Ok(token);
            }
            return Unauthorized();
        }

        private AuthorizationToken GenerateJSONWebToken(ApplicationUser user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var claims = new[] {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            var expiration = DateTime.Now.AddMinutes(120);
            var token = new JwtSecurityToken(_config["Jwt:Issuer"],
                _config["Jwt:Issuer"],
                claims,
                expires: expiration,
                signingCredentials: credentials);
            var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
            return new AuthorizationToken { AccessToken = accessToken };
        }

        [HttpPost("logout")]
        public async Task<IActionResult> CancelAccessToken()
        {
            await _signInManager.SignOutAsync();

            return NoContent();
        }
    }
}
