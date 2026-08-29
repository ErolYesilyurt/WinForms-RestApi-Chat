using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;


namespace EfCoreExample
{
    public class User
    {
        [Key]
        public string Gid { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public List<string> messagedms { get; set; } = new List<string>();
    }

    public class Message
    {   public int Id { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
        public string SenderId { get; set; }
        public string ReceiverId { get; set; }
        public bool Seen { get; set; } = false;
        
    }

    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Message> Messages { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=WinFormsApp2Db;Trusted_Connection=True;");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Message>()
                .Property(m => m.Seen)
                .HasDefaultValue(false);

            modelBuilder.Entity<User>()
                .Property(u => u.messagedms)
                .HasConversion(
                    v => string.Join(',', v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                );
        }
    }
}
