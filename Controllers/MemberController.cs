using chatApp.Hubs;
using chatApp.models;
using chatApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NuGet.Packaging.Signing;
using System.Text.RegularExpressions;
using static Data;

namespace chatApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MemberController : ControllerBase
    {
        private readonly AuthDbContext _context;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ILogger<MemberController> _logger;
        public MemberController(AuthDbContext context, IHubContext<ChatHub> hubContext, ILogger<MemberController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _hubContext = hubContext;
            _logger = logger;
        }

        [HttpPost("AddMember")]
        public async Task<IActionResult> AddMember(Data.Member member)
        {
            if (member == null)
            {
                _logger.LogWarning("AddMember: member info is null.");

                return BadRequest("member info is null.");
            }

            if (string.IsNullOrEmpty(member.UserId))
            {
                _logger.LogWarning("AddMember: User Id is empty.");
                return BadRequest("User Id is empty.");
            }

            if (member.GroupId == Guid.Empty)
            {
                _logger.LogWarning("AddMember:Group Id is empty.");
                return BadRequest("Group Id is empty.");
            }
            if (member.IsAdmin == null)
            {
                _logger.LogWarning("AddMember:Is that user admin or not.");
                return BadRequest("Is that user admin or not.");
            }
            GroupMember member1 = new GroupMember
            {
                GroupMemberId = Guid.NewGuid(),
                GroupId = member.GroupId,
                UserId = member.UserId,
                JoinedAt = DateTime.UtcNow,
                IsAdmin = member.IsAdmin
            };
            
            _context.Members.Add(member1);
            await _context.SaveChangesAsync();

            await _hubContext.Clients.User(member.UserId).SendAsync("AddUserToGroup", member.UserId, member.GroupId);

            return Ok("Member has been added");
        }
        [HttpDelete("deleteMember/{userId}/{MemberId}")]
        public async Task<IActionResult> DeleteMember(string userId, Guid MemberId)
        {
            if (userId == null)
            {
                _logger.LogWarning("DeleteMember:user id is null.");
                return BadRequest("user id is null.");
            }
                
            if (MemberId == Guid.Empty)
            {
                _logger.LogWarning("DeleteMember:ID of the member you want to delete is null.");
                return BadRequest("ID of the member you want to delete is null.");
            }
                
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                _logger.LogWarning("DeleteMember:this user doesn't exist.");
                return BadRequest("this user doesn't exist");
            }
            var memberToDelete = await _context.Members.FirstOrDefaultAsync(m=>m.GroupMemberId== MemberId);
            if (memberToDelete == null)
            {
                _logger.LogWarning("DeleteMember:this member doesn't exist.");
                return BadRequest("this member doesn't exist.");
            }
            var member = await _context.Members.FirstOrDefaultAsync(m => m.GroupId == memberToDelete.GroupId&&m.UserId==userId);
            if (member.IsAdmin || member.UserId == memberToDelete.UserId)
            {
                _context.Members.Remove(memberToDelete);
                await _context.SaveChangesAsync();

                await _hubContext.Clients.User(memberToDelete.UserId).SendAsync("RemoveUserFromGroup", memberToDelete.UserId, memberToDelete.GroupId);

                return Ok("member has been deleted");
            }
            else
            {
                _logger.LogWarning("DeleteMember:User is not authorized to delete this member.");
                return Unauthorized("User is not authorized to delete this member.");
            }
            
        }
        [HttpGet("GetGroupMembers/{groupId}")]
        public async Task<IActionResult> GetGroupMembers(string groupId)
        {
            if (!Guid.TryParse(groupId, out Guid parsedGroupId))
            {
                _logger.LogWarning("GetGroupMembers: Invalid GroupId format: {GroupId}", groupId);
                return BadRequest("Invalid GroupId format.");
            }

            var group = await _context.Groups.FirstOrDefaultAsync(g => g.GroupId == parsedGroupId);

            if (group == null)
            {
                _logger.LogWarning("GetGroupMembers: Group with GroupId {GroupId} not found.", parsedGroupId);
                return NotFound("Group not found.");
            }

            var groupMembers = await _context.Members
                                             .Where(m => m.GroupId == parsedGroupId)
                                             .Select(m => new Gmember
                                             {
                                                 GroupMemberId = m.GroupMemberId,
                                                 UserId = m.UserId,
                                                 IsAdmin = m.IsAdmin,
                                                 JoinedAt = m.JoinedAt
                                             })
                                             .ToListAsync();

            if (groupMembers == null || groupMembers.Count == 0)
            {
                _logger.LogInformation("GetGroupMembers: No members found for GroupId {GroupId}.", parsedGroupId);
                return NotFound("No members found for the group.");
            }

            return Ok(new GroupMembers { members = groupMembers });
        }
        [HttpPut("UpdateAdminRole/{userId}/{memberId}")]
        public async Task<IActionResult> UpdateAdminRole(string userId,string memberId)
        {
            if (userId == null || memberId == null)
            {
                _logger.LogWarning("UpdateAdminRole: null data");
                return BadRequest("null data.");
            }
            if (!Guid.TryParse(memberId, out Guid parsedMemberId))
            {
                _logger.LogWarning("UpdateAdminRole: Invalid memberId format: {GroupId}", memberId);
                return BadRequest("Invalid memberId format.");
            }

            var member = await _context.Members.FirstOrDefaultAsync(m => m.GroupMemberId == parsedMemberId);
            
            if (member == null)
            {
                _logger.LogWarning("UpdateAdminRole: member with not found.");
                return NotFound("member not found.");
            }
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var userM = await _context.Members.FirstOrDefaultAsync(m => m.UserId == userId && m.GroupId == member.GroupId);
            if (userM == null)
            {
                _logger.LogWarning("UpdateAdminRole: This user isn't in that group.");
                return BadRequest("This user isn't in that group.");
            }
            if (userM.IsAdmin)
            {
                member.IsAdmin = !member.IsAdmin;
                _context.Members.Update(member);
                await _context.SaveChangesAsync();

                return Ok("The member is now an " + (member.IsAdmin ? "admin" : "non-admin"));
            }
            else
            {
                _logger.LogWarning("UpdateAdminRole:User is not authorized to change this member's role.");
                return Unauthorized("User is not authorized to change this member's role.");

            }
        }
    }
}
