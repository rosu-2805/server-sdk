using System.Runtime.Serialization;

namespace Morph.Server.Sdk.Dto.SpaceFilesSearch
{
    [DataContract] 
    internal sealed class FoundSpaceFileItemDto
    {
        /// <summary>
        /// Represents a file name with an extension.
        /// </summary>
        
        [DataMember(Name = "name")]
        public string Name { get; set; }
        /// <summary>
        /// File extension only.
        /// Value starting with a dot.
        /// </summary>
        [DataMember(Name = "extension")]
        public string Extension { get; set; }
        /// <summary>
        ///  File size in bytes
        /// </summary>
        [DataMember(Name = "fileSizeBytes")]
        public long FileSizeBytes { get; set; }
        /// <summary>
        /// Last file modification date
        /// </summary>
        [DataMember(Name = "lastModified")]
        public string LastModified { get; set; }

        /// <summary>
        /// Highlights. A plain array of tuples.
        /// The first item in the tuple is a zero-based string index to start highlighting.
        /// The second is a string length for highlighting.
        /// Data may overlap.
        /// </summary>
        [DataMember(Name = "hl")]
        public int[] Highlights { get; set; }
    }
}
