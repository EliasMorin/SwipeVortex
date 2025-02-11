using Microsoft.EntityFrameworkCore;

namespace SwipeVortexWb
{
    public class BumbleDbContext : DbContext
    {
        public DbSet<Encounter> Encounters { get; set; }
        public DbSet<Stats> Stats { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite("Data Source=Bumble.db");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Stats>()
                .HasKey(s => s.Id);

            modelBuilder.Entity<Encounter>()
                .HasKey(e => e.Id);
        }
    }

    public class Encounter
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public bool IsMatch { get; set; }
        public DateTime Date { get; set; }
    }

    public class Stats
    {
        public int Id { get; set; }
        public int TotalLikes { get; set; }
        public int TotalMatches { get; set; }
        public int TotalMessagesSent { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}