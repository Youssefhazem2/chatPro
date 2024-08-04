
using chatApp.models;
using Microsoft.AspNetCore.Identity;

namespace chatApp.Models
{
    public class User:IdentityUser
    {
        public ICollection<Message> Messages { get; set; }
        public ICollection<GroupMember> GroupMemberships { get; set; }
    }
}
