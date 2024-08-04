using chatApp.Hubs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using static Data;

namespace chatApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AuthDbContext _context;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ILogger<UserController> _logger;
        public UserController(AuthDbContext context, IHubContext<ChatHub> hubContext, ILogger<UserController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _hubContext = hubContext;
            _logger = logger;
        }
        [HttpGet("GetUserChats/{UserId}")]
        public async Task<IActionResult> GetUserChats(string UserId)
        {
            if (UserId == null)
            {
                _logger.LogWarning("GetUserChats: null data.");

                return NotFound("id is null");
            }

            var groups = await _context.Members.Where(g => g.UserId == UserId).ToListAsync();
            var userMessages = await _context.Messages.Where(g => g.receiverId == UserId|| g.SenderId == UserId).ToListAsync();
            var userIds = userMessages
                .SelectMany(g => new[] { g.receiverId, g.SenderId })
                .Distinct()
                .Where(id => id != UserId) 
                .ToList();
            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .Select(m => new getuser
                {
                    Id=m.Id,
                    email=m.Email,
                    username=m.UserName
                })
                .ToListAsync();

            if (users == null && groups==null || users.Count == 0 && groups.Count == 0)
            {
                _logger.LogInformation("GetUserChats: No users found for UserId {UserId}.", UserId);
                return NotFound("No users found.");
            }


            return Ok(new getusers { users = users });
        }
    }
}
