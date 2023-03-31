using System.Runtime.Serialization;

namespace Morph.Server.Sdk.Dto
{
    [DataContract]
    internal sealed class FileRenameRequestDto
    {
        /// <summary>
        /// Containing folder path
        /// </summary>
        [DataMember(Name = "folderPath")]
        public string FolderPath { get; set; }

        /// <summary>
        /// Old file name
        /// </summary>
        [DataMember(Name = "name")]
        public string OldName { get; set; }

        /// <summary>
        /// New file name
        /// </summary>
        [DataMember(Name = "newName")]
        public string NewName { get; set; }
    }
}