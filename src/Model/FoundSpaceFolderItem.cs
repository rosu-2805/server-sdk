using System;

namespace Morph.Server.Sdk.Model
{
    public class FoundSpaceFolderItem
    {
        /// <summary>
        /// Folder name.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Folder's last modified date.
        /// </summary>
        public DateTime LastModified { get; set; }

        /// <summary>
        /// Folder path to the specified base folder (Public folder).
        /// </summary>

        public string Path { get; set; }
        /// <summary>
        /// Found files
        /// </summary>        
        public FoundSpaceFileItem[] Files { get; set; }
    }
}
