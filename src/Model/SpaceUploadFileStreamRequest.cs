using System.IO;

namespace Morph.Server.Sdk.Model
{
    /// <summary>
    /// Uploads specified data stream to the server space
    /// </summary>
    public sealed class SpaceUploadDataStreamRequest
    {
        /// <summary>
        /// Server folder to place data file
        /// </summary>
        public string ServerFolder { get; set; }
        /// <summary>
        /// Stream to send to
        /// </summary>
        public Stream DataStream { get; set; }
        /// <summary>
        /// Destination server file name
        /// </summary>
        public string FileName { get; set; }
        /// <summary>
        /// File size. required for process indication
        /// </summary>
        public long FileSize { get; set; }
        /// <summary>
        /// A flag to overwrite existing file. If flag is not set and file exists api will raise an exception
        /// </summary>
        public bool OverwriteExistingFile { get; set; } = false;
    }
}