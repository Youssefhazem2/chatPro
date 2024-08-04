using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace chatApp.Hubs
{
    public sealed class ChatHub : Hub
    {
        // Dictionary to map user IDs to connection IDs
        private static readonly ConcurrentDictionary<string, string> userConnections = new ConcurrentDictionary<string, string>();

        public override Task OnConnectedAsync()
        {
            // You can add logic here to handle when a user connects
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            // Remove the user from the dictionary when they disconnect
            userConnections.TryRemove(Context.ConnectionId, out _);
            return base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(string senderId, string receiverId, string content)
        {
            // Notify the receiver about the new message
            await Clients.User(receiverId).SendAsync("ReceiveMessage", senderId, content);
        }

        public async Task SendGroupMessage(Guid groupId, string senderId, string content)
        {
            // Notify all members of the group about the new message
            await Clients.Group(groupId.ToString()).SendAsync("ReceiveGroupMessage", senderId, content);
        }

        public async Task JoinGroup(Guid groupId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupId.ToString());
        }

        public async Task LeaveGroup(Guid groupId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId.ToString());
        }

        // New method to add a user to a group by their user ID
        public async Task AddUserToGroup(string userId, Guid groupId)
        {
            if (userConnections.TryGetValue(userId, out string connectionId))
            {
                await Groups.AddToGroupAsync(connectionId, groupId.ToString());
            }
        }


        public async Task RemoveUserFromGroup(string userId, Guid groupId)
        {
            if (userConnections.TryGetValue(userId, out string connectionId))
            {
                await Groups.RemoveFromGroupAsync(connectionId, groupId.ToString());
            }
        }

        // Method to register a user ID with a connection ID
        public void RegisterUser(string userId)
        {
            userConnections[userId] = Context.ConnectionId;
        }
    }
}
