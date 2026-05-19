using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TriviaBackend.Models.Entities;
using TriviaBackend.Services.Interfaces.DB;

namespace TriviaBackend.Controllers
{
    /// <summary>
    /// Controller for handling authentication and authorization
    /// </summary>
    /// <param name="_DBService"></param>
    /// <param name="configuration"></param>
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(IUserService _DBService, IConfiguration configuration) : ControllerBase
    {
        /// <summary>
        /// Register a new player
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("register")]
        public async Task<ActionResult<BaseUser>> Register(BaseUserDTO request)
        {
            var existingUser = await _DBService.GetUserByUsernameAsync(request.Username);
            if (existingUser != null)
            {
                return Conflict("Username already exists");
            }

            Player user = new Player
            {
                Username = request.Username,
                Elo = 1000,
                GamesPlayed = 0,
                TotalPoints = 0
            };

            var hashedPassword = new PasswordHasher<Player>().HashPassword(user, request.Password);
            user.PasswordHash = hashedPassword;

            await _DBService.AddUserAsync(user);
            return Ok(user);
        }

        /// <summary>
        /// Elevate player to admin, admin does not have elo and points
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        [HttpPost("elevate-to-admin")]
        public async Task<ActionResult<BaseUser>> ElevatePriveleges(string Id)
        {
            var user = await _DBService.GetUserByIdAsync(Id);
            if (user == null)
            {
                return BadRequest("User not found");
            }

            var admin = new Admin
            {
                Id = user.Id,
                Created = user.Created,
                Username = user.Username,
                PasswordHash = user.PasswordHash
            };

            await _DBService.RemoveUserAsync(user);
            await _DBService.AddUserAsync(admin);
            return Ok(user);
        }

        /// <summary>
        /// Login user with jwt
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("login")]
        public async Task<ActionResult<string>> Login(BaseUserDTO request)
        {
            var user = await _DBService.GetUserByUsernameAsync(request.Username);

            if (user == null)
            {
                return BadRequest("User not found");
            }

            if (new PasswordHasher<BaseUser>().VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
            {
                return BadRequest("Incorrect credentials");
            }
            return CreateToken(user);
        }

        private string CreateToken(BaseUser user)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.NameIdentifier, user.Id),
            };
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(configuration.GetValue<string>("AppSettings:Token")!));
            var cred = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: configuration.GetValue<string>("AppSettings:Issuer"),
                audience: configuration.GetValue<string>("AppSettings:Audience"),
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: cred
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}