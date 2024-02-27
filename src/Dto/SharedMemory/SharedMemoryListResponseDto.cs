using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Morph.Server.Sdk.Dto.SharedMemory
{
    [DataContract]
    internal class SharedMemoryListResponseDto
    {
        [DataMember(Name = "items")]
        public List<SharedMemoryListRecordDto> Items { get; set; } = new List<SharedMemoryListRecordDto>();
        
        [DataMember(Name = "hasMore")]
        public bool HasMore { get; set; }
    }
}