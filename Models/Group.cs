using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chatApp.models
{
    public class Group
    {
        public Guid GroupId { get; set; }

        [Required]
        [MaxLength(100)]
        public string GroupName { get; set; }

        // Navigation properties
        public ICollection<Message>? Messages { get; set; }
        public ICollection<GroupMember> Members { get; set; }
    }
    
}
