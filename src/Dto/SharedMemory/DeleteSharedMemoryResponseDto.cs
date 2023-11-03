using System.Runtime.Serialization;

namespace Morph.Server.Sdk.Dto.SharedMemory
{
    [DataContract]
    public class DeleteSharedMemoryResponseDto
    {
        [DataMember(Name = "deletedCount")]
        public int DeletedCount { get; set; }
    }
}