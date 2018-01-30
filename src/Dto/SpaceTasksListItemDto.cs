using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Morph.Server.Sdk.Dto
{
    [DataContract]
    internal class SpaceTasksListItemDto
    {
        [DataMember(Name = "jobId")]
        public string JobId { get; set; }
        [DataMember(Name = "projectFile")]
        public string ProjectFile { get; set; } = string.Empty;
        [DataMember(Name = "note")]
        public string Note { get; set; } = string.Empty;
        [DataMember(Name = "jobParameters")]
        public List<TaskParameterResponseDto> JobParameters { get; set; }
        [DataMember(Name = "enabled")]
        public bool Enabled { get; set; } = false;
        [DataMember(Name = "status")]
        public string Status { get; set; } = string.Empty;
        [DataMember(Name = "name")]
        public string Name { get; set; } = string.Empty;
        [DataMember(Name = "statusText")]
        public string StatusText { get; set; } = string.Empty;
        [DataMember(Name = "isRunning")]
        public bool IsRunning { get; set; } = false;
    }
}
