﻿// <auto-generated />
using System;
using CMS.API.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CMS.API.Migrations
{
    [DbContext(typeof(PgDbContext))]
    [Migration("20230306083129_init")]
    partial class init
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasDefaultSchema("cms_schema")
                .HasAnnotation("ProductVersion", "7.0.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("CMS.API.Models.DomainModels.Command", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("text");

                    b.Property<string>("ErrorMessage")
                        .HasColumnType("text");

                    b.Property<DateTime>("EventDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Status")
                        .HasColumnType("text");

                    b.Property<string>("entryId")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("entryId");

                    b.ToTable("command", "cms_schema");
                });

            modelBuilder.Entity("CMS.API.Models.DomainModels.Entry", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<string>("Version")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("entry", "cms_schema");
                });

            modelBuilder.Entity("CMS.API.Models.DomainModels.Command", b =>
                {
                    b.HasOne("CMS.API.Models.DomainModels.Entry", "Entry")
                        .WithMany("Commands")
                        .HasForeignKey("entryId");

                    b.Navigation("Entry");
                });

            modelBuilder.Entity("CMS.API.Models.DomainModels.Entry", b =>
                {
                    b.Navigation("Commands");
                });
#pragma warning restore 612, 618
        }
    }
}
