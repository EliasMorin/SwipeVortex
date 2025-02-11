using Microsoft.EntityFrameworkCore;

namespace SwipeVortexWb
{
    public class HappnDbContext : DbContext
    {
        public DbSet<HappnEncounter> Encounters { get; set; }
        public DbSet<HappnStats> Stats { get; set; }
        
        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite("Data Source=Happn.db");
            
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<HappnStats>()
                .HasKey(s => s.Id);
            modelBuilder.Entity<HappnEncounter>()
                .HasKey(e => e.Id);
        }
    }

    public class HappnEncounter
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string FirstName { get; set; }
        public int Age { get; set; }
        public string Gender { get; set; }
        public string ResidenceCity { get; set; }
        public bool IsMatch { get; set; }
        public DateTime Date { get; set; }
    }

    public class HappnStats
    {
        public int Id { get; set; }
        public int TotalLikes { get; set; }
        public int TotalMatches { get; set; }
        public int TotalMessagesSent { get; set; }
        public int TotalConversations { get; set; }
        public int TotalCrushes { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}