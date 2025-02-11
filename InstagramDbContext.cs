using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace SwipeVortexWb.Instagram
{
    public class InstagramDbContext : DbContext
    {
        public DbSet<TopPostsAnalysis> TopPostsAnalyses { get; set; }
        public DbSet<InstagramStats> Stats { get; set; }
        public DbSet<InstagramHashtagAnalysis> HashtagAnalyses { get; set; }
        public DbSet<MediaData> Medias { get; set; }
        public DbSet<UserData> Users { get; set; }
        public DbSet<RelatedHashtagData> RelatedHashtags { get; set; }
        public DbSet<CategoryScore> CategoryScores { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite("Data Source=Instagram.db");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure relationships and constraints
            modelBuilder.Entity<InstagramStats>().HasKey(s => s.Id);
            modelBuilder.Entity<InstagramHashtagAnalysis>().HasKey(h => h.Id);
            
            // Explicitly specify foreign key for MediaData-UserData relationship
            modelBuilder.Entity<MediaData>()
                .HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId); // Add a UserId property to MediaData

            // Configure CategoryScore relationship
            modelBuilder.Entity<CategoryScore>()
                .HasOne(cs => cs.MediaData)
                .WithMany()
                .HasForeignKey(cs => cs.MediaDataId);

            // Fully configure many-to-many-like relationships
            modelBuilder.Entity<InstagramHashtagAnalysis>()
                .HasMany(h => h.Medias)
                .WithOne();

            modelBuilder.Entity<InstagramHashtagAnalysis>()
                .HasMany(h => h.RelatedHashtags)
                .WithOne();
        }
    }

    public class TopPostsAnalysis
    {
        public int Id { get; set; }
        public string Hashtag { get; set; }
        public DateTime AnalysisDate { get; set; }
        public List<int> TopMediaDataIds { get; set; } // Références aux IDs des MediaData
        public int AnalysisRank { get; set; } // Pour permettre le tri/classement des analyses
        public double AverageFinalScore { get; set; }
        public long TotalImpressions { get; set; }
        public long TotalLikes { get; set; }
    }

    public class InstagramStats
    {
        public int Id { get; set; }
        public long TotalHashtagsAnalyzed { get; set; }
        public long TotalPostsProcessed { get; set; }
        public long TotalUniqueUsers { get; set; }
        public long TotalRelatedHashtags { get; set; }
        public DateTime LastAnalysisDate { get; set; }
    }

    public class InstagramHashtagAnalysis
    {
        public int Id { get; set; }
        public string Hashtag { get; set; }
        public DateTime ScrapeDate { get; set; }
        public long TotalMediaCount { get; set; }
        public List<MediaData> Medias { get; set; }
        public List<RelatedHashtagData> RelatedHashtags { get; set; }
    }

    public class MediaData
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string MediaCode { get; set; }
        public string MediaType { get; set; }
        public DateTime PostedAt { get; set; }
        public long LikesCount { get; set; }
        public long CommentsCount { get; set; }
        public string Caption { get; set; }
        public UserData User { get; set; }
        public double TopicRelevance { get; set; }
        public double PenetrationRate { get; set; }
        public double FinalScore { get; set; }
        public string PostUrl { get; set; }
    }

    public class UserData
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public bool IsVerified { get; set; }
        public long FollowerCount { get; set; }
    }

    public class RelatedHashtagData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public long MediaCount { get; set; }
    }

    public class CategoryScore
    {
        public int Id { get; set; }
        public string CategoryName { get; set; }
        public double Score { get; set; }
        public int MediaDataId { get; set; }
        public MediaData MediaData { get; set; }
    }
}