using System;

public class Data
{
	

    public class Member
    {

        public Guid GroupId { get; set; }

        public string UserId { get; set; }

        public bool IsAdmin { get; set; }

    }
    public class CreateGroup
    {

        public string GroupName { get; set; }

        // Navigation properties
        public string CreatorId { get; set; }
    }
    public class smessage
    {
        public string Content { get; set; }
        public string SenderId { get; set; }
        public string? ReceiverId { get; set; }
        public Guid? GroupId { get; set; }
    }
    public class MessageDto
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public string SenderId { get; set; }
        public string? ReceiverId { get; set; }
        public Guid? GroupId { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class ChatMessagesDto
    {
        public List<MessageDto> FUserMessages { get; set; }
        public List<MessageDto> SUserMessages { get; set; }
    }
    public class GChatMessagesDto
    {
        public List<MessageDto> groupMessages { get; set; }
    }
    public class Gmember
    {
        public DateTime JoinedAt { get; set; }
        public Guid GroupMemberId { get; set; }
        public string UserId { get; set; }
        public bool IsAdmin { get; set; }

    }
    public class GroupMembers
    {
        public List<Gmember> members { get; set; }
    }
    public class getuser
    {
        public string Id { get; set; }
        public string username { get; set; }
        public string email { get; set; }

    }
    public class getusers
    {
        public List<getuser> users { get; set; }
    }
    public class EditGroup
    {
        public Guid Id { get; set; }
        public string groupname { get; set; }

    }
}

