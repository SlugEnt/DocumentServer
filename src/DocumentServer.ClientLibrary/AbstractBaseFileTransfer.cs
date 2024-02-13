using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentServer.ClientLibrary
{
    public abstract class AbstractBaseFileTransfer
    {
        /// <summary>
        /// The File in Base64 format
        /// </summary>
        public string FileInBase64Format { get; set; }

        /// <summary>
        ///  File Extension of the document
        /// </summary>
        public string FileExtension { get; set; } = "";


        /// <summary>
        /// Reads the specified file into the FileInBase64Format property.
        /// </summary>
        /// <param name="fileName">Full path and name of file to read in</param>
        /// <param name="fileSystem">Should be set to Mock File System in Unit Testing.  Leave null or empty for real use scenarios</param>
        /// <returns></returns>
        public bool ReadFileIn(string fileName,
                               IFileSystem fileSystem = null)
        {
            try
            {
                FileInBase64Format = Convert.ToBase64String(File.ReadAllBytes(fileName));
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading file - {0}", fileName);
            }

            return false;
        }


        /// <summary>
        /// Saves the file.  The Extension is automatically added
        /// </summary>
        /// <param name="pathToSaveTo"></param>
        /// <param name="fileName"></param>
        /// <param name="fileSystem"></param>
        /// <returns></returns>
        public bool SaveFile(string pathToSaveTo,
                             string fileName,
                             IFileSystem fileSystem)
        {
            try
            {
                byte[] binaryFile = Convert.FromBase64String(FileInBase64Format);
                if (FileExtension != string.Empty)
                    fileName = fileName + "." + FileExtension;
                string fullFileName = Path.Combine(pathToSaveTo, fileName);
                fileSystem.File.WriteAllBytesAsync(fullFileName, binaryFile);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error Saving file - {0}", ex.Message);
                return false;
            }
        }
    }
}