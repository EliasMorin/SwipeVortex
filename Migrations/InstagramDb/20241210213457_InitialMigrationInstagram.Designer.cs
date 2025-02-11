﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SwipeVortexWb.Instagram;

#nullable disable

namespace SwipeVortexWb.Migrations.InstagramDb
{
    [DbContext(typeof(InstagramDbContext))]
    [Migration("20241210213457_InitialMigrationInstagram")]
    partial class InitialMigrationInstagram
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "9.0.0");

            modelBuilder.Entity("SwipeVortexWb.Instagram.CategoryScore", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("CategoryName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("MediaDataId")
                        .HasColumnType("INTEGER");

                    b.Property<double>("Score")
                        .HasColumnType("REAL");

                    b.HasKey("Id");

                    b.HasIndex("MediaDataId");

                    b.ToTable("CategoryScores");
                });

            modelBuilder.Entity("SwipeVortexWb.Instagram.InstagramHashtagAnalysis", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Hashtag")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("ScrapeDate")
                        .HasColumnType("TEXT");

                    b.Property<long>("TotalMediaCount")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("HashtagAnalyses");
                });

            modelBuilder.Entity("SwipeVortexWb.Instagram.InstagramStats", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("LastAnalysisDate")
                        .HasColumnType("TEXT");

                    b.Property<long>("TotalHashtagsAnalyzed")
                        .HasColumnType("INTEGER");

                    b.Property<long>("TotalPostsProcessed")
                        .HasColumnType("INTEGER");

                    b.Property<long>("TotalRelatedHashtags")
                        .HasColumnType("INTEGER");

                    b.Property<long>("TotalUniqueUsers")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Stats");
                });

            modelBuilder.Entity("SwipeVortexWb.Instagram.MediaData", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Caption")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<long>("CommentsCount")
                        .HasColumnType("INTEGER");

                    b.Property<double>("FinalScore")
                        .HasColumnType("REAL");

                    b.Property<int?>("InstagramHashtagAnalysisId")
                        .HasColumnType("INTEGER");

                    b.Property<long>("LikesCount")
                        .HasColumnType("INTEGER");

                    b.Property<string>("MediaCode")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("MediaType")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<double>("PenetrationRate")
                        .HasColumnType("REAL");

                    b.Property<string>("PostUrl")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("PostedAt")
                        .HasColumnType("TEXT");

                    b.Property<double>("TopicRelevance")
                        .HasColumnType("REAL");

                    b.Property<int?>("UserId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("InstagramHashtagAnalysisId");

                    b.HasIndex("UserId");

                    b.ToTable("Medias");
                });

            modelBuilder.Entity("SwipeVortexWb.Instagram.RelatedHashtagData", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int?>("InstagramHashtagAnalysisId")
                        .HasColumnType("INTEGER");

                    b.Property<long>("MediaCount")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("InstagramHashtagAnalysisId");

                    b.ToTable("RelatedHashtags");
                });

            modelBuilder.Entity("SwipeVortexWb.Instagram.UserData", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<long>("FollowerCount")
                        .HasColumnType("INTEGER");

                    b.Property<string>("FullName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsVerified")
                        .HasColumnType("INTEGER");

                    b.Property<string>("UserName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("SwipeVortexWb.Instagram.CategoryScore", b =>
                {
                    b.HasOne("SwipeVortexWb.Instagram.MediaData", "MediaData")
                        .WithMany()
                        .HasForeignKey("MediaDataId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("MediaData");
                });

            modelBuilder.Entity("SwipeVortexWb.Instagram.MediaData", b =>
                {
                    b.HasOne("SwipeVortexWb.Instagram.InstagramHashtagAnalysis", null)
                        .WithMany("Medias")
                        .HasForeignKey("InstagramHashtagAnalysisId");

                    b.HasOne("SwipeVortexWb.Instagram.UserData", "User")
                        .WithMany()
                        .HasForeignKey("UserId");

                    b.Navigation("User");
                });

            modelBuilder.Entity("SwipeVortexWb.Instagram.RelatedHashtagData", b =>
                {
                    b.HasOne("SwipeVortexWb.Instagram.InstagramHashtagAnalysis", null)
                        .WithMany("RelatedHashtags")
                        .HasForeignKey("InstagramHashtagAnalysisId");
                });

            modelBuilder.Entity("SwipeVortexWb.Instagram.InstagramHashtagAnalysis", b =>
                {
                    b.Navigation("Medias");

                    b.Navigation("RelatedHashtags");
                });
#pragma warning restore 612, 618
        }
    }
}
