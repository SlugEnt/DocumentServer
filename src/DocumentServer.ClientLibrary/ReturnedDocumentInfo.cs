namespace SlugEnt.DocumentServer.ClientLibrary
{
    /// <summary>
    /// Used by DocumentServer  when returning a document to the caller.  Provides some needed information.
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
    }
}