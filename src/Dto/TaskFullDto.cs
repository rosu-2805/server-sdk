using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Morph.Server.Sdk.Dto
{
    [DataContract]
    internal class TaskFullDto
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }
        [DataMember(Name = "projectFile")]
        public string ProjectFile { get; set; } = string.Empty;
        [DataMember(Name = "note")]
        public string Note { get; set; } = string.Empty;
        [DataMember(Name = "parameters")]
        public List<TaskParameterResponseDto> Parameters { get; set; }
        [DataMember(Name = "enabled")]
        public bool Enabled { get; set; } = false;
        
        [DataMember(Name = "name")]
        public string Name { get; set; } = string.Empty;
        
        
    }
}
