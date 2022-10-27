using System.Runtime.Serialization;

namespace Morph.Server.Sdk.Dto
{
    [DataContract]
    internal sealed class FolderRenameRequestDto
    {
        /// <summary>
        /// Container folder path
        /// </summary>
        [DataMember(Name = "folderPath")]
        public string FolderPath { get; set; }

        /// <summary>
        /// Old folder name
        /// </summary>
        [DataMember(Name = "name")]
        public string Name { get; set; }

        /// <summary>
        /// New folder name
        /// </summary>
        [DataMember(Name = "newName")]
        public string NewName { get; set; }

        [DataMember(Name = "failIfExists")]
        public bool FailIfExists { get; set; }
    }
}