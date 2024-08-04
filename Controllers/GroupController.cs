using chatApp.models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using chatApp.Controllers;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using chatApp.Hubs;
using Microsoft.AspNetCore.SignalR;
using static Data;
using chatApp.Models;
namespace chatApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]



    public class GroupController : ControllerBase
    {

        private readonly AuthDbContext _context;
        private readonly MemberController _memberController;
        private readonly IHubContext<Hub> _hubContext;
        private readonly ILogger<GroupController> _logger;
        public GroupController(AuthDbContext context, MemberController memberController, IHubContext<ChatHub> hubContext, ILogger<GroupController> logger)
        {
            _context = context;
            _memberController = memberController;
            _hubContext = hubContext;
            _logger = logger;
        }

        [HttpPost("createGroup")]
        public async Task<IActionResult> createGroup(Data.CreateGroup group)
        {
            if (group == null)
            {
                _logger.LogWarning("createGroup:Group info is null.");
                return BadRequest("Group info is null.");
            }

            if (string.IsNullOrEmpty(group.GroupName))
            {
                _logger.LogWarning("createGroup:Group name is empty.");
                return BadRequest("Group name is empty.");
            }
            if (string.IsNullOrEmpty(group.CreatorId))
            {
                _logger.LogWarning("createGroup:User Id is empty.");
                return BadRequest("User Id is empty.");
            }
            var user = _context.Users.FirstOrDefault(u => u.Id == group.CreatorId);
            if (user == null)
            {
                _logger.LogWarning("createGroup:This user doesn't exist.");
                return NotFound("This user doesn't exist.");
            }
            models.Group group1 = new models.Group
            {
                GroupId = Guid.NewGuid(),
                GroupName = group.GroupName,
                Members = new List<GroupMember>()
            };

            Data.Member member = new Data.Member
            {
                GroupId = group1.GroupId,
                UserId = group.CreatorId,
                IsAdmin = true
            };
            _context.Groups.Add(group1);
            var r = await _memberController.AddMember(member);
            if (r != Ok())
                return r;
            await _context.SaveChangesAsync();


            return Ok("Group has been created");

        }
        [HttpDelete("deleteGroup/{userId}/{GroupId}")]
        public async Task<IActionResult> deleteGroup(string userId, Guid GroupId)
        {
            if (userId == null)
            {
                _logger.LogWarning("deleteGroup:user id is null.");
                return BadRequest("user id is null");
            }
                
            if (GroupId == Guid.Empty)
            {
                _logger.LogWarning("deleteGroup:ID of the group you want to delete is null.");
                return BadRequest("ID of the group you want to delete is null.");
            }
                
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                _logger.LogWarning("deleteGroup:user id is null.");
                return BadRequest("this user doesn't exist");
            }
            var groupToDelete = await _context.Groups.FirstOrDefaultAsync(m => m.GroupId == GroupId);
            if (groupToDelete == null)
            {
                _logger.LogWarning("deleteGroup:this group doesn't exist.");
                return BadRequest("this group doesn't exist");
            }
            var member = await _context.Members.FirstOrDefaultAsync(m => m.GroupId == groupToDelete.GroupId && m.UserId == userId && m.IsAdmin);
            if (member != null)
            {
                _context.Groups.Remove(groupToDelete);
                await _context.SaveChangesAsync();
                return Ok("Group has been deleted");
            }
            else
            {
                _logger.LogWarning("deleteGroup:User is not authorized to delete this group.");
                return Unauthorized("User is not authorized to delete this group.");
            }

        }
        [HttpPut("EditGroupName")]
        public async Task<IActionResult> EditGroupName(EditGroup editedGroup)
        {
            if (editedGroup == null)
            {
                _logger.LogWarning("EditGroupName:You didn't send the data.");
                return BadRequest("You didn't send the data.");
            }
            if (editedGroup.Id==Guid.Empty)
            {
                _logger.LogWarning("EditGroupName:You didn't send the id.");
                return BadRequest("You didn't send the id.");
            }
            if (editedGroup.groupname == null)
            {
                _logger.LogWarning("EditGroupName:You didn't send the new group name.");
                return BadRequest("You didn't send the new group name.");
            }
            var group = await _context.Groups.FirstOrDefaultAsync(g => g.GroupId == editedGroup.Id);
            if (group == null)
            {
                _logger.LogWarning("EditGroupName: this group id isn't exist.");
                return BadRequest("This group id isn't exist.");
            }
            group.GroupName=editedGroup.groupname;
            _context.Groups.Update(group);
            await _context.SaveChangesAsync();

            return Ok("Group name updated successfully");
        }
    }
}
