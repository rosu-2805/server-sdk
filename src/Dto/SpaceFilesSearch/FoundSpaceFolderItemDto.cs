using System.Runtime.Serialization;

namespace Morph.Server.Sdk.Dto.SpaceFilesSearch
{
    [DataContract]
    internal sealed class FoundSpaceFolderItemDto
    {
        /// <summary>
        /// Folder name.
        /// </summary>
        [DataMember(Name = "name")]
        public string Name { get; set; }
        /// <summary>
        /// Folder's last modified date.
        /// </summary>
        [DataMember(Name = "lastModified")]
        public string LastModified { get; set; }
        
        /// <summary>
        /// Folder path to the specified base folder (Public folder).
        /// </summary>
        
        [DataMember(Name = "path")]
        public string Path { get; set; }
        /// <summary>
        /// Found files
        /// </summary>
        [DataMember(Name = "files")]
        public FoundSpaceFileItemDto[] Files { get; set; }
    }
}
