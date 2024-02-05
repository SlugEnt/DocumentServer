using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using DocumentServer.Core;
using DocumentServer.Db;
using NSubstitute;
using SlugEnt;

namespace DocumentServer_Test.SupportObjects;

/// <summary>
///     This sets up a Test Data class of common items that are needed for each test scenario
///     * A Mock File System
///     * A Database Connection to a Unit Test Database
///     * The DocumentServerEngine
///     * Some Support Methods that can be used during testing
/// </summary>
public class SupportMethods
{
    private readonly MockLogger<DocumentServerEngine> _logger = Substitute.For<MockLogger<DocumentServerEngine>>();


    /// <summary>
    ///     Constructore.  Builds Mock Filesystem, Mock Logger, and the DocServerEngine
    /// </summary>
    /// <param name="databaseSetupTest"></param>
    public SupportMethods(DatabaseSetup_Test databaseSetupTest)
    {
        DB = databaseSetupTest.CreateContext();

        FileSystem.Directory.CreateDirectory("test");
        FileSystem.Directory.CreateDirectory(@"test\primary");

        DocumentServerEngine = new DocumentServerEngine(_logger, DB, FileSystem);
    }


    /// <summary>
    ///     Returns the DB Context
    /// </summary>
    public DocServerDbContext DB { get; }


    /// <summary>
    ///     Return the DocumentServerEngine
    /// </summary>
    public DocumentServerEngine DocumentServerEngine { get; }


    /// <summary>
    ///     Many of the Mock File System commands needs a FileDataAcceesor.  This provides that.
    /// </summary>
    public IMockFileDataAccessor FileDataAccessor => FileSystem;


    /// <summary>
    ///     Returns the Mocked File System
    /// </summary>
    public MockFileSystem FileSystem { get; } = new();

    // Some Folders 
    public string Folder_Test => @"test";

    public string Folder_Test_Primary => @"test\primary";
    public string Folder_Test_Secondary => @"test\secondary";


    /// <summary>
    ///     Writes a random file out with a random filename
    /// </summary>
    /// <param name="fileSystem"></param>
    /// <param name="path"></param>
    /// <param name="extension"></param>
    /// <param name="sizeInKB"></param>
    /// <returns></returns>
    public string WriteRandomFile(IFileSystem fileSystem,
                                  string path,
                                  string extension,
                                  int sizeInKB)
    {
        int        fileSize   = sizeInKB * 1024;
        UniqueKeys uniqueKeys = new("");
        string     filePart   = uniqueKeys.GetKey("abc");
        string     fileName   = filePart + "." + extension;
        string     fullPath   = Path.Combine(path, fileName);

        byte[] data = new byte[1024];
        Random rng  = new();

        // IMockFileDataAccessor fileDataAccessor = (IMockFileDataAccessor)_fileSystem;

        MockFileStream stream = new(FileDataAccessor, fullPath, FileMode.Create);
        for (int i = 0; i < sizeInKB; i++)
        {
            rng.NextBytes(data);
            stream.Write(data, 0, data.Length);
        }

        return fileName;
    }
}