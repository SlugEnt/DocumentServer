using SlugEnt.DocumentServer.Models.Enums;

namespace SlugEnt.DocumentServer.ClientLibrary
{
    /// <summary>
    /// This object contains information returned to the caller of a Document Retrieval operation (GetStoredDocument, etc)
    /// </summary>
    public class ReturnedDocumentInfo
    {
        /// <summary>
        /// The Extension that the file should have
        /// </summary>
        public string? Extension { get; set; }

        /// <summary>
        /// The actual bytes that make up the file
        /// </summary>
        public byte[]? FileInBytes { get; set; }

        /// <summary>
        /// Size in Bytes
        /// </summary>
        public long? Size { get; set; }

        /// <summary>
        /// The description stored on the Document.
        /// </summary>
        public string? Description { get; set; }

        public EnumMediaTypes MediaType { get; set; }

        public string ContentType { get; set; }
    }
}