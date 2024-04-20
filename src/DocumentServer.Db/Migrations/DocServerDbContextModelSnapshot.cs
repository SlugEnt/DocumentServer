﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SlugEnt.DocumentServer.Db;

#nullable disable

namespace SlugEnt.DocumentServer.Db.Migrations
{
    [DbContext(typeof(DocServerDbContext))]
    partial class DocServerDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("SlugEnt.DocumentServer.Models.Entities.Application", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("CreatedAtUTC")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<DateTime?>("ModifiedAtUTC")
                        .HasColumnType("datetime2");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(75)
                        .HasColumnType("nvarchar(75)");

                    b.Property<string>("Token")
                        .IsRequired()
                        .HasMaxLength(32)
                        .HasColumnType("nvarchar(32)");

                    b.HasKey("Id");

                    b.ToTable("Applications");
                });

            modelBuilder.Entity("SlugEnt.DocumentServer.Models.Entities.DocumentType", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int?>("ActiveStorageNode1Id")
                        .HasColumnType("int");

                    b.Property<int?>("ActiveStorageNode2Id")
                        .HasColumnType("int");

                    b.Property<bool>("AllowSameDTEKeys")
                        .HasColumnType("bit");

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

                    b.Property<byte>("InActiveLifeTime")
                        .HasColumnType("tinyint");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<DateTime?>("ModifiedAtUTC")
                        .HasColumnType("datetime2");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(75)
                        .HasColumnType("nvarchar(75)");

                    b.Property<int>("RootObjectId")
                        .HasColumnType("int");

                    b.Property<string>("StorageFolderName")
                        .IsRequired()
                        .HasMaxLength(10)
                        .HasColumnType("nvarchar(10)");

                    b.Property<byte>("StorageMode")
                        .HasColumnType("tinyint");

                    b.HasKey("Id");

                    b.HasIndex("ActiveStorageNode1Id");

                    b.HasIndex("ActiveStorageNode2Id");

                    b.HasIndex("ApplicationId");

                    b.HasIndex("ArchivalStorageNode1Id");

                    b.HasIndex("ArchivalStorageNode2Id");

                    b.HasIndex("RootObjectId");

                    b.ToTable("DocumentTypes");
                });

            modelBuilder.Entity("SlugEnt.DocumentServer.Models.Entities.ExpiringDocument", b =>
                {
                    b.Property<long>("StoredDocumentId")
                        .HasColumnType("bigint");

                    b.Property<DateTime>("ExpirationDateUtcDateTime")
                        .HasColumnType("datetime2");

                    b.HasKey("StoredDocumentId");

                    b.ToTable("ExpiringDocuments");
                });

            modelBuilder.Entity("SlugEnt.DocumentServer.Models.Entities.ReplicationTask", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<DateTime>("CreatedAtUTC")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<DateTime?>("ModifiedAtUTC")
                        .HasColumnType("datetime2");

                    b.Property<int>("ReplicateFromStorageNodeId")
                        .HasColumnType("int");

                    b.Property<int>("ReplicateToStorageNodeId")
                        .HasColumnType("int");

                    b.Property<long>("StoredDocumentId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("ReplicateFromStorageNodeId");

                    b.HasIndex("ReplicateToStorageNodeId");

                    b.HasIndex("StoredDocumentId");

                    b.ToTable("ReplicationTasks");
                });

            modelBuilder.Entity("SlugEnt.DocumentServer.Models.Entities.RootObject", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("ApplicationId")
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedAtUTC")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<DateTime?>("ModifiedAtUTC")
                        .HasColumnType("datetime2");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("ApplicationId");

                    b.ToTable("RootObjects");
                });

            modelBuilder.Entity("SlugEnt.DocumentServer.Models.Entities.ServerHost", b =>
                {
                    b.Property<short>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("smallint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<short>("Id"));

                    b.Property<DateTime>("CreatedAtUTC")
                        .HasColumnType("datetime2");

                    b.Property<string>("FQDN")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<DateTime?>("ModifiedAtUTC")
                        .HasColumnType("datetime2");

                    b.Property<string>("NameDNS")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Path")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("ServerHosts");
                });

            modelBuilder.Entity("SlugEnt.DocumentServer.Models.Entities.StorageNode", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("CreatedAtUTC")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(400)
                        .HasColumnType("nvarchar(400)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<bool>("IsTestNode")
                        .HasColumnType("bit");

                    b.Property<DateTime?>("ModifiedAtUTC")
                        .HasColumnType("datetime2");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("NodePath")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<short>("ServerHostId")
                        .HasColumnType("smallint");

                    b.Property<byte>("StorageNodeLocation")
                        .HasColumnType("tinyint");

                    b.Property<byte>("StorageSpeed")
                        .HasColumnType("tinyint");

                    b.HasKey("Id");

                    b.HasIndex("ServerHostId");

                    b.ToTable("StorageNodes");
                });

            modelBuilder.Entity("SlugEnt.DocumentServer.Models.Entities.StoredDocument", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<DateTime>("CreatedAtUTC")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(250)
                        .HasColumnType("nvarchar(250)");

                    b.Property<string>("DocTypeExternalKey")
                        .HasColumnType("nvarchar(450)");

                    b.Property<int>("DocumentTypeId")
                        .HasColumnType("int");

                    b.Property<string>("FileName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<bool>("IsAlive")
                        .HasColumnType("bit");

                    b.Property<bool>("IsArchived")
                        .HasColumnType("bit");

                    b.Property<DateTime>("LastAccessedUTC")
                        .HasColumnType("datetime2");

                    b.Property<int>("MediaType")
                        .HasColumnType("int");

                    b.Property<DateTime?>("ModifiedAtUTC")
                        .HasColumnType("datetime2");

                    b.Property<int>("NumberOfTimesAccessed")
                        .HasColumnType("int");

                    b.Property<int?>("PrimaryStorageNodeId")
                        .HasColumnType("int");

                    b.Property<string>("RootObjectExternalKey")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<int?>("SecondaryStorageNodeId")
                        .HasColumnType("int");

                    b.Property<int>("SizeInKB")
                        .HasColumnType("int");

                    b.Property<byte>("Status")
                        .HasColumnType("tinyint");

                    b.Property<string>("StorageFolder")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("DocumentTypeId");

                    b.HasIndex("PrimaryStorageNodeId");

                    b.HasIndex("SecondaryStorageNodeId");

                    b.HasIndex(new[] { "RootObjectExternalKey", "DocTypeExternalKey" }, "IDX_Ext_Keys");

                    b.ToTable("StoredDocuments");
                });

            modelBuilder.Entity("SlugEnt.DocumentServer.Models.Entities.VitalInfo", b =>
                {
                    b.Property<string>("Id")
                        .HasMaxLength(32)
                        .HasColumnType("nvarchar(32)");

                    b.Property<DateTime>("LastUpdateUtc")
                        .HasColumnType("datetime2");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<long>("ValueLong")
                        .HasColumnType("bigint");

                    b.Property<string>("ValueString")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("VitalInfos");

                    b.HasData(
                        new
                        {
                            Id = "LastKeyEntityUpdate",
                            LastUpdateUtc = new DateTime(1, 1, 1, 0, 0, 0, 1, DateTimeKind.Unspecified),
                            Name = "Last Update to Key Entities",
                            ValueLong = 0L,
                            ValueString = ""
                        });
                });

            modelBuilder.Entity("SlugEnt.DocumentServer.Models.Entities.DocumentType", b =>
                {
                    b.HasOne("SlugEnt.DocumentServer.Models.Entities.StorageNode", "ActiveStorageNode1")
                        .WithMany("ActiveNode1DocumentTypes")
                        .HasForeignKey("ActiveStorageNode1Id");

                    b.HasOne("SlugEnt.DocumentServer.Models.Entities.StorageNode", "ActiveStorageNode2")
                        .WithMany("ActiveNode2DocumentTypes")
                        .HasForeignKey("ActiveStorageNode2Id");

                    b.HasOne("SlugEnt.DocumentServer.Models.Entities.Application", "Application")
                        .WithMany("DocumentTypes")
                        .HasForeignKey("ApplicationId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("SlugEnt.DocumentServer.Models.Entities.StorageNode", "ArchivalStorageNode1")
                        .WithMany("ArchivalNode1DocumentTypes")
                        .HasForeignKey("ArchivalStorageNode1Id");

                    b.HasOne("SlugEnt.DocumentServer.Models.Entities.StorageNode", "ArchivalStorageNode2")
                        .WithMany("ArchivalNode2DocumentTypes")
                        .HasForeignKey("ArchivalStorageNode2Id");

                    b.HasOne("SlugEnt.DocumentServer.Models.Entities.RootObject", "RootObject")
                        .WithMany("DocumentTypes")
                        .HasForeignKey("RootObjectId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("ActiveStorageNode1");

                    b.Navigation("ActiveStorageNode2");

                    b.Navigation("Application");

                    b.Navigation("ArchivalStorageNode1");

                    b.Navigation("ArchivalStorageNode2");

                    b.Navigation("RootObject");
                });

            modelBuilder.Entity("SlugEnt.DocumentServer.Models.Entities.ReplicationTask", b =>
                {
                    b.HasOne("SlugEnt.DocumentServer.Models.Entities.StorageNode", "ReplicateFromStorageNode")
                        .WithMany("ReplicationTaskFromNodes")
                        .HasForeignKey("ReplicateFromStorageNodeId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("SlugEnt.DocumentServer.Models.Entities.StorageNode", "ReplicateToStorageNode")
                        .WithMany("ReplicationTaskToNodes")
                        .HasForeignKey("ReplicateToStorageNodeId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("SlugEnt.DocumentServer.Models.Entities.StoredDocument", "StoredDocument")
                        .WithMany("StoredDocumentsNeedingReplication")
                        .HasForeignKey("StoredDocumentId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ReplicateFromStorageNode");

                    b.Navigation("ReplicateToStorageNode");

                    b.Navigation("StoredDocument");
                });

            modelBuilder.Entity("SlugEnt.DocumentServer.Models.Entities.RootObject", b =>
                {
                    b.HasOne("SlugEnt.DocumentServer.Models.Entities.Application", "Application")
                        .WithMany()
                        .HasForeignKey("ApplicationId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Application");
                });

            modelBuilder.Entity("SlugEnt.DocumentServer.Models.Entities.StorageNode", b =>
                {
                    b.HasOne("SlugEnt.DocumentServer.Models.Entities.ServerHost", "ServerHost")
                        .WithMany()
                        .HasForeignKey("ServerHostId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ServerHost");
                });

            modelBuilder.Entity("SlugEnt.DocumentServer.Models.Entities.StoredDocument", b =>
                {
                    b.HasOne("SlugEnt.DocumentServer.Models.Entities.DocumentType", "DocumentType")
                        .WithMany()
                        .HasForeignKey("DocumentTypeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("SlugEnt.DocumentServer.Models.Entities.StorageNode", "PrimaryStorageNode")
                        .WithMany("PrimaryNodeStoredDocuments")
                        .HasForeignKey("PrimaryStorageNodeId");

                    b.HasOne("SlugEnt.DocumentServer.Models.Entities.StorageNode", "SecondaryStorageNode")
                        .WithMany("SecondaryNodeStoredDocuments")
                        .HasForeignKey("SecondaryStorageNodeId");

                    b.Navigation("DocumentType");

                    b.Navigation("PrimaryStorageNode");

                    b.Navigation("SecondaryStorageNode");
                });

            modelBuilder.Entity("SlugEnt.DocumentServer.Models.Entities.Application", b =>
                {
                    b.Navigation("DocumentTypes");
                });

            modelBuilder.Entity("SlugEnt.DocumentServer.Models.Entities.RootObject", b =>
                {
                    b.Navigation("DocumentTypes");
                });

            modelBuilder.Entity("SlugEnt.DocumentServer.Models.Entities.StorageNode", b =>
                {
                    b.Navigation("ActiveNode1DocumentTypes");

                    b.Navigation("ActiveNode2DocumentTypes");

                    b.Navigation("ArchivalNode1DocumentTypes");

                    b.Navigation("ArchivalNode2DocumentTypes");

                    b.Navigation("PrimaryNodeStoredDocuments");

                    b.Navigation("ReplicationTaskFromNodes");

                    b.Navigation("ReplicationTaskToNodes");

                    b.Navigation("SecondaryNodeStoredDocuments");
                });

            modelBuilder.Entity("SlugEnt.DocumentServer.Models.Entities.StoredDocument", b =>
                {
                    b.Navigation("StoredDocumentsNeedingReplication");
                });
#pragma warning restore 612, 618
        }
    }
}
