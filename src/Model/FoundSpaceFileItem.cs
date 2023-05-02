using System;

namespace Morph.Server.Sdk.Model
{
    public class FoundSpaceFileItem
    {
        /// <summary>
        /// Represents a file name with an extension.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// File extension only.
        /// Value starting with a dot.
        /// </summary>
        public string Extension { get; set; }
        /// <summary>
        ///  File size in bytes
        /// </summary>
        public long FileSizeBytes { get; set; }
        /// <summary>
        /// Last file modification date
        /// </summary>
        public DateTime LastModified { get; set; }

        /// <summary>
        /// Highlights. A plain array of tuples.
        /// The first item in the tuple is a zero-based string index to start highlighting.
        /// The second is a string length for highlighting.
        /// Data may overlap.
        /// </summary>
        public int[] Highlights { get; set; }
    }
}
