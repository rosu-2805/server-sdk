using System.Runtime.Serialization;

namespace Morph.Server.Sdk.Dto.SharedMemory
{
    [DataContract]
    internal class SetSharedMemoryValueDto
    {
        public static class BehaviorCodes
        {
            public const string Overwrite = "overwrite";
            public const string ThrowIfExists = "fail";
            public const string IgnoreIfExists = "ignore";
        }
        
        [DataMember(Name = "key")]
        public string Key { get; set; }
        
        [DataMember(Name = "value")]
        public SharedMemoryValueDto Value { get; set; }
        
        /// <summary>
        /// One of <see cref="SetSharedMemoryValueDto.BehaviorCodes"/>
        /// (corresponds to Morph.CSContracts.MutableStateProvider.OverwriteBehavior)
        /// </summary>
        [DataMember(Name = "overwriteBehavior")]
        public string OverwriteBehavior { get; set; }
    }
}