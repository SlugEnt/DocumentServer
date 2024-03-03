namespace SlugEnt.DocumentServer.ClientLibrary
{
    /// <summary>
    /// Used to Send Information about a given file
    /// </summary>
    public class FileInfo
    {
        /// <summary>
        /// The Extension that the file should have
        /// </summary>
        public string Extension { get; set; }

        /// <summary>
        /// The actual bytes that make up the file
        /// </summary>
        public byte[] FileInBytes { get; set; }

        /// <summary>
        /// Size in Bytes
        /// </summary>
        public long Size { get; set; }
    }
}