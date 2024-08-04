using chatApp.models;
using chatApp.Models;
using Microsoft.EntityFrameworkCore;

namespace chatApp
{
    public class AuthDbContext : DbContext
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<GroupMember> Members { get; set; }

        
    }
}
