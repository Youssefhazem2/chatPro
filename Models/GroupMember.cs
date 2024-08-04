using chatApp.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace chatApp.models
{
    public class GroupMember
    {
        public Guid GroupMemberId { get; set; }

        [Required]
        public Guid GroupId { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public DateTime JoinedAt { get; set; }
        [Required]
        public bool IsAdmin { get; set; }
        public Group? Group { get; set; }

        public User? User { get; set; }
    }
}
