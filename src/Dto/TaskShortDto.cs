using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Morph.Server.Sdk.Dto
{
    [DataContract]
    internal class TaskShortDto
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }
        
        [DataMember(Name = "projectFile")]
        public string ProjectFile { get; set; } = string.Empty;
        
        [DataMember(Name = "note")]
        public string Note { get; set; } = string.Empty;
        
        [DataMember(Name = "enabled")]
        public bool Enabled { get; set; } = false;
        [DataMember(Name = "status")]
        public string Status { get; set; } = string.Empty;
        [DataMember(Name = "name")]
        public string Name { get; set; } = string.Empty;
        
        [DataMember(Name="nextRunText")]
        public string NextRunText { get; set; } = "";
     
    }
}
