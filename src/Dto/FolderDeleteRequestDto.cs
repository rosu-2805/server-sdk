using System.Runtime.Serialization;

namespace Morph.Server.Sdk.Dto
{
    [DataContract]
    internal sealed class FolderDeleteRequestDto
    {
        [DataMember(Name = "folderPath")]
        public string FolderPath { get; set; }

        [DataMember(Name = "failIfNotFound")]
        public bool? FailIfNotFound { get; set; }
    }
}