﻿// <auto-generated />
using System;
using DocumentServer.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace DocumentServer.Db.Migrations
{
    [DbContext(typeof(DocServerDbContext))]
    [Migration("20240205115729_Initial")]
    partial class Initial
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("DocumentServer.Models.Entities.Application", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(75)
                        .HasColumnType("nvarchar(75)");

                    b.HasKey("Id");

                    b.ToTable("Applications");
                });

            modelBuilder.Entity("DocumentServer.Models.Entities.DocumentType", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<int?>("ActiveStorageNode1Id")
                        .HasColumnType("int");

                    b.Property<int?>("ActiveStorageNode2Id")
                        .HasColumnType("int");

                    b.Property<int>("ApplicationId")
                        .HasColumnType("int");

                    b.Property<int?>("ArchivalStorageNode1Id")
                        .HasColumnType("int");

                    b.Property<int?>("ArchivalStorageNode2Id")
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedAtUTC")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(250)
                        .HasColumnType("nvarchar(250)");

                    b.Property<DateTime>("ModifiedAtUTC")
                        .HasColumnType("datetime2");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(75)
                        .HasColumnType("nvarchar(75)");

                    b.Property<byte>("StorageMode")
                        .HasColumnType("tinyint");

                    b.HasKey("Id");

                    b.HasIndex("ActiveStorageNode1Id");

                    b.HasIndex("ActiveStorageNode2Id");

                    b.HasIndex("ApplicationId");

                    b.HasIndex("ArchivalStorageNode1Id");

                    b.HasIndex("ArchivalStorageNode2Id");

                    b.ToTable("DocumentTypes");
                });

            modelBuilder.Entity("DocumentServer.Models.Entities.StorageNode", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(400)
                        .HasColumnType("nvarchar(400)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<bool>("IsTestNode")
                        .HasColumnType("bit");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("NodePath")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<byte>("StorageNodeLocation")
                        .HasColumnType("tinyint");

                    b.Property<byte>("StorageSpeed")
                        .HasColumnType("tinyint");

                    b.HasKey("Id");

                    b.ToTable("StorageNodes");
                });

            modelBuilder.Entity("DocumentServer.Models.Entities.StoredDocument", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAtUTC")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(250)
                        .HasColumnType("nvarchar(250)");

                    b.Property<long>("DocumentTypeId")
                        .HasColumnType("bigint");

                    b.Property<bool>("IsArchived")
                        .HasColumnType("bit");

                    b.Property<DateTime>("LastAccessedUTC")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("ModifiedAtUTC")
                        .HasColumnType("datetime2");

                    b.Property<int>("NumberOfTimesAccessed")
                        .HasColumnType("int");

                    b.Property<int?>("PrimaryStorageNodeId")
                        .HasColumnType("int");

                    b.Property<int?>("SecondaryStorageNodeId")
                        .HasColumnType("int");

                    b.Property<byte>("Status")
                        .HasColumnType("tinyint");

                    b.Property<string>("StorageFolder")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("sizeInKB")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("DocumentTypeId");

                    b.HasIndex("PrimaryStorageNodeId");

                    b.HasIndex("SecondaryStorageNodeId");

                    b.ToTable("StoredDocuments");
                });

            modelBuilder.Entity("DocumentServer.Models.Entities.DocumentType", b =>
                {
                    b.HasOne("DocumentServer.Models.Entities.StorageNode", "ActiveStorageNode1")
                        .WithMany("ActiveNode1DocumentTypes")
                        .HasForeignKey("ActiveStorageNode1Id");

                    b.HasOne("DocumentServer.Models.Entities.StorageNode", "ActiveStorageNode2")
                        .WithMany("ActiveNode2DocumentTypes")
                        .HasForeignKey("ActiveStorageNode2Id");

                    b.HasOne("DocumentServer.Models.Entities.Application", "Application")
                        .WithMany()
                        .HasForeignKey("ApplicationId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("DocumentServer.Models.Entities.StorageNode", "ArchivalStorageNode1")
                        .WithMany("ArchivalNode1DocumentTypes")
                        .HasForeignKey("ArchivalStorageNode1Id");

                    b.HasOne("DocumentServer.Models.Entities.StorageNode", "ArchivalStorageNode2")
                        .WithMany("ArchivalNode2DocumentTypes")
                        .HasForeignKey("ArchivalStorageNode2Id");

                    b.Navigation("ActiveStorageNode1");

                    b.Navigation("ActiveStorageNode2");

                    b.Navigation("Application");

                    b.Navigation("ArchivalStorageNode1");

                    b.Navigation("ArchivalStorageNode2");
                });

            modelBuilder.Entity("DocumentServer.Models.Entities.StoredDocument", b =>
                {
                    b.HasOne("DocumentServer.Models.Entities.DocumentType", "DocumentType")
                        .WithMany()
                        .HasForeignKey("DocumentTypeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("DocumentServer.Models.Entities.StorageNode", "PrimaryStorageNode")
                        .WithMany("PrimaryNodeStoredDocuments")
                        .HasForeignKey("PrimaryStorageNodeId");

                    b.HasOne("DocumentServer.Models.Entities.StorageNode", "SecondaryStorageNode")
                        .WithMany("SecondaryNodeStoredDocuments")
                        .HasForeignKey("SecondaryStorageNodeId");

                    b.Navigation("DocumentType");

                    b.Navigation("PrimaryStorageNode");

                    b.Navigation("SecondaryStorageNode");
                });

            modelBuilder.Entity("DocumentServer.Models.Entities.StorageNode", b =>
                {
                    b.Navigation("ActiveNode1DocumentTypes");

                    b.Navigation("ActiveNode2DocumentTypes");

                    b.Navigation("ArchivalNode1DocumentTypes");

                    b.Navigation("ArchivalNode2DocumentTypes");

                    b.Navigation("PrimaryNodeStoredDocuments");

                    b.Navigation("SecondaryNodeStoredDocuments");
                });
#pragma warning restore 612, 618
        }
    }
}
