USE [master]
GO
/****** Object:  Database [Tst_DocumentServer]    Script Date: 3/6/2024 8:12:15 AM ******/
CREATE DATABASE [Tst_DocumentServer]
 CONTAINMENT = NONE
 ON  PRIMARY 
( NAME = N'Tst_DocumentServer', FILENAME = N'/var/opt/mssql/data/Tst_DocumentServer.mdf' , SIZE = 8192KB , MAXSIZE = UNLIMITED, FILEGROWTH = 65536KB )
 LOG ON 
( NAME = N'Tst_DocumentServer_log', FILENAME = N'/var/opt/mssql/data/Tst_DocumentServer_log.ldf' , SIZE = 8192KB , MAXSIZE = 2048GB , FILEGROWTH = 65536KB )
 WITH CATALOG_COLLATION = DATABASE_DEFAULT
GO
ALTER DATABASE [Tst_DocumentServer] SET COMPATIBILITY_LEVEL = 150
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [Tst_DocumentServer].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [Tst_DocumentServer] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [Tst_DocumentServer] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [Tst_DocumentServer] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [Tst_DocumentServer] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [Tst_DocumentServer] SET ARITHABORT OFF 
GO
ALTER DATABASE [Tst_DocumentServer] SET AUTO_CLOSE OFF 
GO
ALTER DATABASE [Tst_DocumentServer] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [Tst_DocumentServer] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [Tst_DocumentServer] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [Tst_DocumentServer] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [Tst_DocumentServer] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [Tst_DocumentServer] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [Tst_DocumentServer] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [Tst_DocumentServer] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [Tst_DocumentServer] SET  ENABLE_BROKER 
GO
ALTER DATABASE [Tst_DocumentServer] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [Tst_DocumentServer] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [Tst_DocumentServer] SET TRUSTWORTHY OFF 
GO
ALTER DATABASE [Tst_DocumentServer] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO
ALTER DATABASE [Tst_DocumentServer] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [Tst_DocumentServer] SET READ_COMMITTED_SNAPSHOT ON 
GO
ALTER DATABASE [Tst_DocumentServer] SET HONOR_BROKER_PRIORITY OFF 
GO
ALTER DATABASE [Tst_DocumentServer] SET RECOVERY FULL 
GO
ALTER DATABASE [Tst_DocumentServer] SET  MULTI_USER 
GO
ALTER DATABASE [Tst_DocumentServer] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [Tst_DocumentServer] SET DB_CHAINING OFF 
GO
ALTER DATABASE [Tst_DocumentServer] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO
ALTER DATABASE [Tst_DocumentServer] SET TARGET_RECOVERY_TIME = 60 SECONDS 
GO
ALTER DATABASE [Tst_DocumentServer] SET DELAYED_DURABILITY = DISABLED 
GO
ALTER DATABASE [Tst_DocumentServer] SET ACCELERATED_DATABASE_RECOVERY = OFF  
GO
EXEC sys.sp_db_vardecimal_storage_format N'Tst_DocumentServer', N'ON'
GO
ALTER DATABASE [Tst_DocumentServer] SET QUERY_STORE = OFF
GO
USE [Tst_DocumentServer]
GO
/****** Object:  Table [dbo].[__EFMigrationsHistory]    Script Date: 3/6/2024 8:12:15 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[__EFMigrationsHistory](
	[MigrationId] [nvarchar](150) NOT NULL,
	[ProductVersion] [nvarchar](32) NOT NULL,
 CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY CLUSTERED 
(
	[MigrationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Applications]    Script Date: 3/6/2024 8:12:15 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Applications](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](75) NOT NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedAtUTC] [datetime2](7) NOT NULL,
	[ModifiedAtUTC] [datetime2](7) NULL,
 CONSTRAINT [PK_Applications] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DocumentTypes]    Script Date: 3/6/2024 8:12:15 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DocumentTypes](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ActiveStorageNode1Id] [int] NULL,
	[ActiveStorageNode2Id] [int] NULL,
	[ArchivalStorageNode1Id] [int] NULL,
	[ArchivalStorageNode2Id] [int] NULL,
	[Description] [nvarchar](250) NOT NULL,
	[InActiveLifeTime] [tinyint] NOT NULL,
	[Name] [nvarchar](75) NOT NULL,
	[StorageFolderName] [nvarchar](10) NOT NULL,
	[StorageMode] [tinyint] NOT NULL,
	[RootObjectId] [int] NOT NULL,
	[ApplicationId] [int] NOT NULL,
	[AllowSameDTEKeys] [bit] NOT NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedAtUTC] [datetime2](7) NOT NULL,
	[ModifiedAtUTC] [datetime2](7) NULL,
 CONSTRAINT [PK_DocumentTypes] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ExpiringDocuments]    Script Date: 3/6/2024 8:12:15 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ExpiringDocuments](
	[StoredDocumentId] [bigint] NOT NULL,
	[ExpirationDateUtcDateTime] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_ExpiringDocuments] PRIMARY KEY CLUSTERED 
(
	[StoredDocumentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[RootObjects]    Script Date: 3/6/2024 8:12:15 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RootObjects](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ApplicationId] [int] NOT NULL,
	[Description] [nvarchar](max) NOT NULL,
	[Name] [nvarchar](max) NOT NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedAtUTC] [datetime2](7) NOT NULL,
	[ModifiedAtUTC] [datetime2](7) NULL,
 CONSTRAINT [PK_RootObjects] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ServerHosts]    Script Date: 3/6/2024 8:12:15 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ServerHosts](
	[Id] [smallint] IDENTITY(1,1) NOT NULL,
	[NameDNS] [nvarchar](max) NOT NULL,
	[Path] [nvarchar](max) NOT NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedAtUTC] [datetime2](7) NOT NULL,
	[ModifiedAtUTC] [datetime2](7) NULL,
	[FQDN] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_ServerHosts] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[StorageNodes]    Script Date: 3/6/2024 8:12:15 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[StorageNodes](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Description] [nvarchar](400) NOT NULL,
	[IsActive] [bit] NOT NULL,
	[IsTestNode] [bit] NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
	[NodePath] [nvarchar](500) NOT NULL,
	[ServerHostId] [smallint] NOT NULL,
	[StorageNodeLocation] [tinyint] NOT NULL,
	[StorageSpeed] [tinyint] NOT NULL,
	[CreatedAtUTC] [datetime2](7) NOT NULL,
	[ModifiedAtUTC] [datetime2](7) NULL,
 CONSTRAINT [PK_StorageNodes] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[StoredDocuments]    Script Date: 3/6/2024 8:12:15 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[StoredDocuments](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[Description] [nvarchar](250) NOT NULL,
	[DocTypeExternalKey] [nvarchar](450) NULL,
	[DocumentTypeId] [int] NOT NULL,
	[FileName] [nvarchar](max) NOT NULL,
	[IsAlive] [bit] NOT NULL,
	[IsArchived] [bit] NOT NULL,
	[LastAccessedUTC] [datetime2](7) NOT NULL,
	[MediaType] [int] NOT NULL,
	[NumberOfTimesAccessed] [int] NOT NULL,
	[PrimaryStorageNodeId] [int] NULL,
	[RootObjectExternalKey] [nvarchar](450) NOT NULL,
	[SecondaryStorageNodeId] [int] NULL,
	[SizeInKB] [int] NOT NULL,
	[Status] [tinyint] NOT NULL,
	[StorageFolder] [nvarchar](max) NOT NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedAtUTC] [datetime2](7) NOT NULL,
	[ModifiedAtUTC] [datetime2](7) NULL,
 CONSTRAINT [PK_StoredDocuments] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Index [IX_DocumentTypes_ActiveStorageNode1Id]    Script Date: 3/6/2024 8:12:15 AM ******/
CREATE NONCLUSTERED INDEX [IX_DocumentTypes_ActiveStorageNode1Id] ON [dbo].[DocumentTypes]
(
	[ActiveStorageNode1Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_DocumentTypes_ActiveStorageNode2Id]    Script Date: 3/6/2024 8:12:15 AM ******/
CREATE NONCLUSTERED INDEX [IX_DocumentTypes_ActiveStorageNode2Id] ON [dbo].[DocumentTypes]
(
	[ActiveStorageNode2Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_DocumentTypes_ApplicationId]    Script Date: 3/6/2024 8:12:15 AM ******/
CREATE NONCLUSTERED INDEX [IX_DocumentTypes_ApplicationId] ON [dbo].[DocumentTypes]
(
	[ApplicationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_DocumentTypes_ArchivalStorageNode1Id]    Script Date: 3/6/2024 8:12:15 AM ******/
CREATE NONCLUSTERED INDEX [IX_DocumentTypes_ArchivalStorageNode1Id] ON [dbo].[DocumentTypes]
(
	[ArchivalStorageNode1Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_DocumentTypes_ArchivalStorageNode2Id]    Script Date: 3/6/2024 8:12:15 AM ******/
CREATE NONCLUSTERED INDEX [IX_DocumentTypes_ArchivalStorageNode2Id] ON [dbo].[DocumentTypes]
(
	[ArchivalStorageNode2Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_DocumentTypes_RootObjectId]    Script Date: 3/6/2024 8:12:15 AM ******/
CREATE NONCLUSTERED INDEX [IX_DocumentTypes_RootObjectId] ON [dbo].[DocumentTypes]
(
	[RootObjectId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_RootObjects_ApplicationId]    Script Date: 3/6/2024 8:12:15 AM ******/
CREATE NONCLUSTERED INDEX [IX_RootObjects_ApplicationId] ON [dbo].[RootObjects]
(
	[ApplicationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_StorageNodes_ServerHostId]    Script Date: 3/6/2024 8:12:15 AM ******/
CREATE NONCLUSTERED INDEX [IX_StorageNodes_ServerHostId] ON [dbo].[StorageNodes]
(
	[ServerHostId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IDX_Ext_Keys]    Script Date: 3/6/2024 8:12:15 AM ******/
CREATE NONCLUSTERED INDEX [IDX_Ext_Keys] ON [dbo].[StoredDocuments]
(
	[RootObjectExternalKey] ASC,
	[DocTypeExternalKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_StoredDocuments_DocumentTypeId]    Script Date: 3/6/2024 8:12:15 AM ******/
CREATE NONCLUSTERED INDEX [IX_StoredDocuments_DocumentTypeId] ON [dbo].[StoredDocuments]
(
	[DocumentTypeId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_StoredDocuments_PrimaryStorageNodeId]    Script Date: 3/6/2024 8:12:15 AM ******/
CREATE NONCLUSTERED INDEX [IX_StoredDocuments_PrimaryStorageNodeId] ON [dbo].[StoredDocuments]
(
	[PrimaryStorageNodeId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_StoredDocuments_SecondaryStorageNodeId]    Script Date: 3/6/2024 8:12:15 AM ******/
CREATE NONCLUSTERED INDEX [IX_StoredDocuments_SecondaryStorageNodeId] ON [dbo].[StoredDocuments]
(
	[SecondaryStorageNodeId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[ServerHosts] ADD  DEFAULT (N'') FOR [FQDN]
GO
ALTER TABLE [dbo].[DocumentTypes]  WITH CHECK ADD  CONSTRAINT [FK_DocumentTypes_Applications_ApplicationId] FOREIGN KEY([ApplicationId])
REFERENCES [dbo].[Applications] ([Id])
GO
ALTER TABLE [dbo].[DocumentTypes] CHECK CONSTRAINT [FK_DocumentTypes_Applications_ApplicationId]
GO
ALTER TABLE [dbo].[DocumentTypes]  WITH CHECK ADD  CONSTRAINT [FK_DocumentTypes_RootObjects_RootObjectId] FOREIGN KEY([RootObjectId])
REFERENCES [dbo].[RootObjects] ([Id])
GO
ALTER TABLE [dbo].[DocumentTypes] CHECK CONSTRAINT [FK_DocumentTypes_RootObjects_RootObjectId]
GO
ALTER TABLE [dbo].[DocumentTypes]  WITH CHECK ADD  CONSTRAINT [FK_DocumentTypes_StorageNodes_ActiveStorageNode1Id] FOREIGN KEY([ActiveStorageNode1Id])
REFERENCES [dbo].[StorageNodes] ([Id])
GO
ALTER TABLE [dbo].[DocumentTypes] CHECK CONSTRAINT [FK_DocumentTypes_StorageNodes_ActiveStorageNode1Id]
GO
ALTER TABLE [dbo].[DocumentTypes]  WITH CHECK ADD  CONSTRAINT [FK_DocumentTypes_StorageNodes_ActiveStorageNode2Id] FOREIGN KEY([ActiveStorageNode2Id])
REFERENCES [dbo].[StorageNodes] ([Id])
GO
ALTER TABLE [dbo].[DocumentTypes] CHECK CONSTRAINT [FK_DocumentTypes_StorageNodes_ActiveStorageNode2Id]
GO
ALTER TABLE [dbo].[DocumentTypes]  WITH CHECK ADD  CONSTRAINT [FK_DocumentTypes_StorageNodes_ArchivalStorageNode1Id] FOREIGN KEY([ArchivalStorageNode1Id])
REFERENCES [dbo].[StorageNodes] ([Id])
GO
ALTER TABLE [dbo].[DocumentTypes] CHECK CONSTRAINT [FK_DocumentTypes_StorageNodes_ArchivalStorageNode1Id]
GO
ALTER TABLE [dbo].[DocumentTypes]  WITH CHECK ADD  CONSTRAINT [FK_DocumentTypes_StorageNodes_ArchivalStorageNode2Id] FOREIGN KEY([ArchivalStorageNode2Id])
REFERENCES [dbo].[StorageNodes] ([Id])
GO
ALTER TABLE [dbo].[DocumentTypes] CHECK CONSTRAINT [FK_DocumentTypes_StorageNodes_ArchivalStorageNode2Id]
GO
ALTER TABLE [dbo].[RootObjects]  WITH CHECK ADD  CONSTRAINT [FK_RootObjects_Applications_ApplicationId] FOREIGN KEY([ApplicationId])
REFERENCES [dbo].[Applications] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[RootObjects] CHECK CONSTRAINT [FK_RootObjects_Applications_ApplicationId]
GO
ALTER TABLE [dbo].[StorageNodes]  WITH CHECK ADD  CONSTRAINT [FK_StorageNodes_ServerHosts_ServerHostId] FOREIGN KEY([ServerHostId])
REFERENCES [dbo].[ServerHosts] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[StorageNodes] CHECK CONSTRAINT [FK_StorageNodes_ServerHosts_ServerHostId]
GO
ALTER TABLE [dbo].[StoredDocuments]  WITH CHECK ADD  CONSTRAINT [FK_StoredDocuments_DocumentTypes_DocumentTypeId] FOREIGN KEY([DocumentTypeId])
REFERENCES [dbo].[DocumentTypes] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[StoredDocuments] CHECK CONSTRAINT [FK_StoredDocuments_DocumentTypes_DocumentTypeId]
GO
ALTER TABLE [dbo].[StoredDocuments]  WITH CHECK ADD  CONSTRAINT [FK_StoredDocuments_StorageNodes_PrimaryStorageNodeId] FOREIGN KEY([PrimaryStorageNodeId])
REFERENCES [dbo].[StorageNodes] ([Id])
GO
ALTER TABLE [dbo].[StoredDocuments] CHECK CONSTRAINT [FK_StoredDocuments_StorageNodes_PrimaryStorageNodeId]
GO
ALTER TABLE [dbo].[StoredDocuments]  WITH CHECK ADD  CONSTRAINT [FK_StoredDocuments_StorageNodes_SecondaryStorageNodeId] FOREIGN KEY([SecondaryStorageNodeId])
REFERENCES [dbo].[StorageNodes] ([Id])
GO
ALTER TABLE [dbo].[StoredDocuments] CHECK CONSTRAINT [FK_StoredDocuments_StorageNodes_SecondaryStorageNodeId]
GO
USE [master]
GO
ALTER DATABASE [Tst_DocumentServer] SET  READ_WRITE 
GO
