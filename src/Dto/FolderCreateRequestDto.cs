using System.Runtime.Serialization;

namespace Morph.Server.Sdk.Dto
{
    [DataContract]
    internal sealed class FolderCreateRequestDto
    {
        /// <summary>
        /// Containing folder path
        /// </summary>
        [DataMember(Name = "folderPath")]
        public string FolderPath { get; set; }

        /// <summary>
        /// New folder name
        /// </summary>
        [DataMember(Name = "folderName")]
        public string FolderName { get; set; }

        /// <summary>
        /// True to fail if folder already exists, 'false' to suppress the error
        /// </summary>
        [DataMember(Name = "failIfExists")]
        public bool FailIfExists { get; set; }
    }
}