namespace DocumentServer.Models.Enums;

/// <summary>
/// The mode the document should be stored as. 
/// </summary>
public enum EnumStorageMode
{
    /// <summary>
    /// It is a temporary file and will be cleaned up via Document Server according to temporary rules
    /// </summary>
    Temporary = 1,

    /// <summary>
    /// The file can be edited.  Meaning it can be overwritten as well as deleted
    /// </summary>
    Editable = 50,


    /// <summary>
    /// The document can be replaced with another "version" of the document.  The prior one is deleted.
    /// </summary>
    Replaceable = 100,


    /// <summary>
    /// A Document that can retain a certain number of versions.
    /// </summary>
    Versioned = 150,

    /// <summary>
    /// The document can only be written once.  It can only be deleted if it has exceeded the lifetime rules
    /// </summary>
    WriteOnceReadMany = 200
}