using chatApp.Hubs;
using chatApp.models;
using chatApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NuGet.Protocol.Plugins;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Data;

namespace chatApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessageController : ControllerBase
    {
        private readonly AuthDbContext _context;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ILogger<MessageController> _logger;
        public MessageController(AuthDbContext context, IHubContext<ChatHub> hubContext, ILogger<MessageController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _hubContext = hubContext;
            _logger = logger;
        }

        

        [HttpPost("SendMessage")]
        public async Task<IActionResult> SendMessage( Data.smessage mes)
        {
            if (mes == null)
            {
                _logger.LogWarning("SendMessage: Message content is null.");
                return BadRequest("Message content is null.");
            }

            if (string.IsNullOrEmpty(mes.Content))
            {
                _logger.LogWarning("SendMessage: Message content is empty.");
                return BadRequest("Message content is empty.");
            }

            if (string.IsNullOrEmpty(mes.SenderId))
            {
                _logger.LogWarning("SendMessage: SenderId is empty.");
                return BadRequest("SenderId is empty.");
            }


            var sender = _context.Users.FirstOrDefault(u => u.Id == mes.SenderId);
            if (mes.GroupId != null)
            {
                var member = await _context.Members.FirstOrDefaultAsync(m => m.GroupId == mes.GroupId && m.UserId == mes.SenderId);
                if (sender == null)
                {
                    _logger.LogWarning("SendMessage: Sender not found.");
                    return NotFound("Sender not found.");
                }
                if (member == null)
                {
                    _logger.LogWarning("SendMessage:this user is not in that group.");
                    return Unauthorized("this user is not in that group.");
                }
            }
            var message = new models.Message
            {
                Content = mes.Content,
                SenderId = mes.SenderId,
                receiverId = mes.GroupId == null ? mes.ReceiverId : (string?)null,
                GroupId = mes.ReceiverId == null ? mes.GroupId : (Guid?)null,
                Timestamp = DateTime.UtcNow,
                User = sender
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
            if (mes.ReceiverId != null)
            {
                await _hubContext.Clients.User(mes.ReceiverId).SendAsync("ReceiveMessage", mes.SenderId, mes.Content);
            }
            else
            {
                await _hubContext.Clients.Group(mes.GroupId.ToString()).SendAsync("ReceiveGroupMessage", mes.SenderId, mes.Content);
            }
            return Ok("Message has been sent");
        }
        [HttpGet("GetChat/{fUserId}/{sUserId}")]
        public async Task<IActionResult> GetChat(string fUserId,string sUserId)
        {
            if (fUserId == null|| sUserId== null)
            {
                _logger.LogWarning("GetChat: User IDs are null.");
                return BadRequest("data is null.");
            }

            var user1 = await _context.Users.FirstOrDefaultAsync(u => u.Id == fUserId);
            var user2 = await _context.Users.FirstOrDefaultAsync(u => u.Id == sUserId);

            if (user1 == null)
            {
                _logger.LogWarning("GetChat: Sender doesn't exist.");
                return NotFound("sender doesn't exist");
            }
            if (user2 == null)
            {
                _logger.LogWarning("GetChat: Receiver doesn't exist.");
                return NotFound("receiver doesn't exist");
            }
            var fUserMessages = await _context.Messages
                                      .Where(m => m.SenderId == fUserId && m.receiverId == sUserId)
                                      .Select(m => new MessageDto
                                      {
                                          Id = m.MessageId,
                                          Content = m.Content,
                                          SenderId = m.SenderId,
                                          ReceiverId = m.receiverId,
                                          Timestamp = m.Timestamp
                                      })
                                      .ToListAsync();

            var sUserMessages = await _context.Messages
                                              .Where(m => m.receiverId == fUserId && m.SenderId == sUserId)
                                              .Select(m => new MessageDto
                                              {
                                                  Id = m.MessageId,
                                                  Content = m.Content,
                                                  SenderId = m.SenderId,
                                                  ReceiverId = m.receiverId,
                                                  Timestamp = m.Timestamp
                                              })
                                              .ToListAsync();
            return Ok(new ChatMessagesDto { FUserMessages = fUserMessages, SUserMessages = sUserMessages });
        }
        [HttpGet("GetGroupChat/{groupId}")]
        public async Task<IActionResult> GetGroupChat(Guid groupId)
        {
            if (groupId == Guid.Empty)
            {
                return BadRequest("data is null.");
            }

            var group = await _context.Groups.FirstOrDefaultAsync(u => u.GroupId == groupId);

            if (group == null)
            {
                _logger.LogWarning("GetGroupChat: Group doesn't exist.");

                return NotFound("sender doesn't exist");
            }
            
            var GroupMessages = await _context.Messages
                                      .Where(m => m.GroupId == group.GroupId)
                                      .Select(m => new MessageDto
                                      {
                                          Id = m.MessageId,
                                          Content = m.Content,
                                          SenderId = m.SenderId,
                                          GroupId = m.GroupId,
                                          Timestamp = m.Timestamp
                                      }).ToListAsync();

            
            return Ok(new GChatMessagesDto { groupMessages=GroupMessages});
        }
        [HttpDelete("DeleteMessage/{userId}/{messageId}")]
        public async Task<IActionResult> DeleteMessage(string userId,int messageId)
        {
            if (userId == null||messageId==null)
            {
                _logger.LogWarning("DeleteMessage: User ID or message ID is invalid.");

                return BadRequest("data is null.");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {

                _logger.LogWarning("DeleteMessage: User not found.");

                return NotFound("There is no user with that ID");
            }

            var Message = await _context.Messages.FirstOrDefaultAsync(m => m.MessageId == messageId);
            if (Message == null)
            {
                _logger.LogWarning("DeleteMessage: Message not found.");

                return NotFound("There is no message with that ID");
            }
            var memeber = await _context.Members.FirstOrDefaultAsync(m => m.UserId == userId && m.GroupId == Message.GroupId && m.IsAdmin);

            if (Message.SenderId == userId)
            {
                _context.Messages.Remove(Message);
            }
            else if (Message.GroupId!=null&& memeber!=null)
            {
                _context.Messages.Remove(Message);
            }
            else
            {
                _logger.LogWarning("DeleteMessage: User is not authorized to delete this message.");

                return Unauthorized("User is not authorized to delete this message.");
            }
            await _context.SaveChangesAsync();

            return Ok("Message has been deleted");
        }
        [HttpDelete("clearChat/{userId}/{recId}")]
        public async Task<IActionResult> clearChat(string userId,string recId)
        {
            if (userId == null)
            {
                _logger.LogWarning("DeleteChat:The User ID or message ID is invalid.");

                return BadRequest("The User ID or message ID is invalid.");
            }
            if(recId == null)
            {
                _logger.LogWarning("DeleteChat:the receiver User ID is invalid.");

                return BadRequest("the receiver User ID is invalid.");
            }
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var recUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == recId);
            if (user == null)
            {

                _logger.LogWarning("DeleteChat: User not found.");

                return NotFound("There is no user with that ID");
            }
            if (recUser == null)
            {

                _logger.LogWarning("DeleteChat: User not found.");

                return NotFound("There is no user with that ID");
            }
            var Messages = await _context.Messages.Where(m => m.SenderId == userId && m.receiverId == recId|| m.SenderId == recId && m.receiverId == userId).ToListAsync();
            if (Messages == null)
            {
                _logger.LogWarning("DeleteChat: there is no messages between those users.");

                return NotFound("there is no messages between those users.");
            }
            _context.Messages.RemoveRange(Messages);
            await _context.SaveChangesAsync();

            return Ok("Messages has been deleted");
        }
    }
}
