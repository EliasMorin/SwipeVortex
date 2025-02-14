﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SwipeVortexWb;

#nullable disable

namespace SwipeVortexWb.Migrations.HappnDb
{
    [DbContext(typeof(HappnDbContext))]
    [Migration("20241201210812_InitialMigrationHappn")]
    partial class InitialMigrationHappn
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "9.0.0");

            modelBuilder.Entity("SwipeVortexWb.HappnEncounter", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("Age")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("Date")
                        .HasColumnType("TEXT");

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Gender")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsMatch")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ResidenceCity")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Encounters");
                });

            modelBuilder.Entity("SwipeVortexWb.HappnStats", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("LastUpdated")
                        .HasColumnType("TEXT");

                    b.Property<int>("TotalConversations")
                        .HasColumnType("INTEGER");

                    b.Property<int>("TotalCrushes")
                        .HasColumnType("INTEGER");

                    b.Property<int>("TotalLikes")
                        .HasColumnType("INTEGER");

                    b.Property<int>("TotalMatches")
                        .HasColumnType("INTEGER");

                    b.Property<int>("TotalMessagesSent")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Stats");
                });
#pragma warning restore 612, 618
        }
    }
}
