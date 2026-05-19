using Microsoft.AspNetCore.Mvc;
using TriviaBackend.Models.Entities;
using TriviaBackend.Services.Interfaces.DB;

namespace TriviaBackend.Controllers
{
    /// <summary>
    /// Controller for handling actions related to clans
    /// </summary>
    /// <param name="_ClanService"></param>
    /// <param name="_UserService"></param>
    [Route("api/[controller]")]
    [ApiController]
    public class ClanController(IClanService _ClanService, IUserService _UserService) : Controller
    {
        [HttpGet("getuser/{usn}")]
        public async Task<ActionResult<BaseUser>> GetUserByName(string usn)
        {
            var user = await _UserService.GetUserByUsernameAsync(usn);

            if(user == null)
                return NotFound();

            return Ok(user);
        }

        [HttpGet("getclan/{clanId}")]
        public async Task<ActionResult<Clan>> GetClanById(int clanId)
        {
            var clan = await _ClanService.GetClanByIdAsync(clanId);

            if (clan == null)
                return NotFound();

            return Ok(clan);
        }

        [HttpGet("getclanbyname/{clanName}")]
        public async Task<ActionResult<Clan>> GetClanByName(string clanName)
        {
            var clan = await _ClanService.GetClanByNameAsync(clanName);

            if(clan == null)
                return NotFound();

            return Ok(clan);
        }

        [HttpGet("getmembers/{clanId}")]
        public async Task<ActionResult<List<BaseUser>>> GetClanMembers(int clanId)
        {
            var clan = await _ClanService.GetClanByIdAsync(clanId);

            if (clan == null)
                return NotFound("Clan does not exist");

            var members = await _ClanService.GetAllClanMembersAsnyc(clan);

            if(members == null)
                return NotFound("No members found");

            return Ok(members);
        }

        [HttpPost("join/{clanId}")]
        public async Task<ActionResult> JoinClan(int clanId, string userId)
        {
            var user = await _UserService.GetUserByIdAsync(userId);

            if(user == null)
                return NotFound("User does not exist");

            var clan = await _ClanService.GetClanByIdAsync(clanId);

            if (clan == null)
                return NotFound("Clan does not exist");

            if (user.ClanId == clan.Id)
                return BadRequest("User is already in the clan");

            if (user.ClanId != null)
                return BadRequest("User is already in another clan");

            await _ClanService.AddMemberToClanAsync(clan, user);
            return Ok();
        }

        [HttpDelete("leave/{clanId}")]
        public async Task<ActionResult> LeaveClan(int clanId, string userId)
        {
            var user = await _UserService.GetUserByIdAsync(userId);

            if (user == null)
                return NotFound("User does not exist");

            var clan = await _ClanService.GetClanByIdAsync(clanId);

            if (clan == null)
                return NotFound("Clan does not exist");

            if (user.ClanId != clan.Id)
                return BadRequest("User is not in the clan");

            if (user.ClanId == null)
                return BadRequest("User is not in any clan");

            await _ClanService.RemoveMemberFromClanAsync(clan, user);    
            return Ok();
        }

        [HttpPost("kick")]
        public async Task<ActionResult> KickUser(string kickeeId, string adminId)
        {
            var offender = await _UserService.GetUserByIdAsync(kickeeId);
            var admin = await _UserService.GetUserByIdAsync(adminId);

            if (offender == null || admin == null)
                return NotFound();

            if (admin is not Admin)
                return Unauthorized("User is not an admin");

            if(admin is Admin ad && !ad.CanKickUsers)
                return Unauthorized("Managing user is not allowed to kick clan members");

            if (offender.ClanId == null)
                return BadRequest("User is not in any clan");

            var clan = await _ClanService.GetClanByIdAsync(offender.ClanId.Value);

            await _ClanService.RemoveMemberFromClanAsync(clan!, offender);
            return Ok();
        }

        [HttpPost("create")]
        public async Task<ActionResult> CreateClan(string clanName, string userId)
        {
            var user = await _UserService.GetUserByIdAsync(userId);
            if (user == null)
                return NotFound();

            if (user is not Admin)
                return Unauthorized("User is not allowed to manage clans");

            if(user is Admin admin && !admin.CanManageContent)
                return Unauthorized("User is not allowed to manage clans");

            if (await _ClanService.GetClanByNameAsync(clanName) != null)
                return Conflict($"Clan already exists with the name {clanName}");

            var clan = new Clan { Name = clanName };

            await _ClanService.CreateClanAsync(clan);

            return Ok();
        }

        [HttpPatch("rename")]
        public async Task<ActionResult> RenameClan(int clanId, string newName, string userId)
        {
            var user = await _UserService.GetUserByIdAsync(userId);
            if (user == null)
                return NotFound();

            if (user is not Admin)
                return Unauthorized("User is not allowed to manage clans");

            if (user is Admin admin && !admin.CanManageContent)
                return Unauthorized("User is not allowed to manage clans");

            var clan = await _ClanService.GetClanByIdAsync(clanId);

            if (clan == null)
                return NotFound("Clan does not exist");

            await _ClanService.RenameClanAsync(clan, newName);
            return Ok();
        }

        [HttpDelete("delete")]
        public async Task<ActionResult> DeleteClan(int clanId, string userId)
        {
            var user = await _UserService.GetUserByIdAsync(userId);
            if (user == null)
                return NotFound();

            if (user is not Admin)
                return Unauthorized("User is not allowed to manage clans");

            if (user is Admin admin && !admin.CanManageContent)
                return Unauthorized("User is not allowed to manage clans");

            var clan = await _ClanService.GetClanByIdAsync(clanId);

            if (clan == null)
                return NotFound("Clan does not exist");

            await _ClanService.DeleteClanAsync(clan);
            return NoContent();
        }
    }
}
