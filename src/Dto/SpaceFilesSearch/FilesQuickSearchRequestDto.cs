using System.Runtime.Serialization;

namespace Morph.Server.Sdk.Dto.SpaceFilesSearch
{
    [DataContract]
    internal sealed class SpaceFilesQuickSearchRequestDto


    {
        /// <summary>
        /// Lookup string. It may include multiple search terms split by space char.
        /// A search is performed only in the file name.
        /// </summary>
        [DataMember(Name = "lookupString")]
        public string LookupString { get; set; }

        /// <summary>
        /// Limit search by file extensions.
        /// An empty array means no limitations by file extension.
        /// The file extension must start with a dot character.
        /// An empty string value is used to look up files with no extensions.
        /// </summary>
        [DataMember(Name = "fileExtensions")]
        public string[] FileExtensions { get; set; }

        /// <summary>
        /// The folder path for lookup startup. Search will be run only in this folder and all nested folders.
        /// </summary>
        [DataMember(Name = "folderPath")]
        public string FolderPath { get; set; }
    }
}
