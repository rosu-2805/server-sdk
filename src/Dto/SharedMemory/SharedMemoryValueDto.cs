using System.Runtime.Serialization;

namespace Morph.Server.Sdk.Dto.SharedMemory
{
    [DataContract]
    internal class SharedMemoryValueDto
    {
        public static class TypeCodes
        {
            public const string Error = "Error";
            public const string Nothing = "Nothing";
            public const string Boolean = "Boolean";
            public const string Text = "Text";
            public const string Number = "Number";
        }
        
        [DataMember(Name = "contents")]
        public string Contents { get; set; }
        
        /// <summary>
        /// One of <see cref="SharedMemoryValueDto.TypeCodes"/>
        /// </summary>
        [DataMember(Name = "typeCode")]
        public string TypeCode { get; set; }   
    }
}