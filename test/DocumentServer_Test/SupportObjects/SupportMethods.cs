using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentServer.Core;
using DocumentServer.Db;
using NSubstitute;

namespace DocumentServer_Test.SupportObjects;

/// <summary>
/// This sets up a Test Data class of common items that are needed for each test scenario
/// </summary>
public class SupportMethods
{
    MockFileSystem                   _fileSystem = new();
    MockLogger<DocumentServerEngine> _logger     = Substitute.For<MockLogger<DocumentServerEngine>>();
    private DocumentServerEngine     _docServerEngine;
    private DocServerDbContext       _docDbContext;


    /// <summary>
    /// Constructore.  Builds Mock Filesystem, Mock Logger, and the DocServerEngine
    /// </summary>
    /// <param name="databaseSetupTest"></param>
    public SupportMethods(DatabaseSetup_Test databaseSetupTest)
    {
        _docDbContext = databaseSetupTest.CreateContext();

        _fileSystem.Directory.CreateDirectory("test");
        _fileSystem.Directory.CreateDirectory(@"test\primary");

        _docServerEngine = new DocumentServerEngine(_logger, _docDbContext, _fileSystem);
    }


    /// <summary>
    /// Returns the DB Context
    /// </summary>
    public DocServerDbContext DB => _docDbContext;


    /// <summary>
    /// Returns the Mocked File System
    /// </summary>
    public MockFileSystem FileSystem => _fileSystem;
}