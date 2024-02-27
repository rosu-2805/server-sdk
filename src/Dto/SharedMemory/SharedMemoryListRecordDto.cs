using System;
using System.Runtime.Serialization;

namespace Morph.Server.Sdk.Dto.SharedMemory
{
    [DataContract]
    internal class SharedMemoryListRecordDto
    {
        /// <summary>
        /// Key of the record
        /// </summary>
        [DataMember(Name = "key")]
        public string Key { get; set; }

        /// <summary>
        /// Value of the record
        /// </summary>
        [DataMember(Name = "value")]
        public SharedMemoryValueDto Value { get; set; }

        /// <summary>
        /// Author of the record, if available
        /// </summary>
        [DataMember(Name = "author")]
        public string Author { get; set; }

        /// <summary>
        /// Date and time of creation of the record, if available
        /// </summary>
        [DataMember(Name = "modified")]
        public string Modified { get; set; }
    }
}