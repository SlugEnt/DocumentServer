﻿using System.IO.Abstractions;

namespace SlugEnt.DocumentServer.ClientLibrary;

/// <summary>
/// Container used to Send and retrieve information about documents, back and forth.
/// </summary>
public class DocumentContainer
{
    public TransferDocumentDto Info { get; set; }


    /// <summary>
    /// Information about the file contained in this object.
    /// </summary>
    public FileInfo? FileInfo { get; set; }


    public IFormFile File { get; set; }



#region "SupportMethods"

    /// <summary>
    /// Creates a FormFile object from a physical file
    /// </summary>
    /// <param name="fullFileName"></param>
    /// <returns></returns>
    public static IFormFile GetFormFile(string fullFileName,
                                        string mediaTypeNameValue,
                                        IFileSystem fileSystem = null)
    {
        IFormFile file;
        Stream    stream = new FileStream(fullFileName, FileMode.Open);


        file = new FormFile(stream,
                            0,
                            stream.Length,
                            null,
                            Path.GetFileName(fullFileName))
        {
            Headers     = new HeaderDictionary(),
            ContentType = mediaTypeNameValue,
        };


        return file;
    }


    /// <summary>
    /// Creates a FormFile from a Byte Array
    /// </summary>
    /// <param name="fileBytes"></param>
    /// <param name="mediaType">The media type the file is</param>
    /// <returns></returns>
    public static IFormFile GetFormFile(byte[] fileBytes,
                                        string mediaTypeNameValue)
    {
        FormFile     file;
        MemoryStream ms = new MemoryStream(fileBytes);
        file = new FormFile(ms,
                            0,
                            ms.Length,
                            null,
                            "somefile.pdf")
        {
            Headers     = new HeaderDictionary(),
            ContentType = mediaTypeNameValue,
        };

        ms.Seek(0, SeekOrigin.Begin);


        file.OpenReadStream();
        return file;
    }

#endregion
}