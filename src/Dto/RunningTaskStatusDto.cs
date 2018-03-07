using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Dto
{
    [DataContract]
    internal class RunningTaskStatusDto
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }
        [DataMember(Name = "isRunning")]
        public bool IsRunning { get; set; }
        [DataMember(Name = "projectName")]
        public string ProjectName { get; set; }
        [DataMember(Name = "jobExecutionId")]
        public string JobExecutionId { get; set; }
        [DataMember(Name = "errors")]
        public List<RunningTaskErrorInfoDto> Errors { get; set; } = new List<RunningTaskErrorInfoDto>();
    }

    [DataContract]
    internal class RunningTaskErrorInfoDto
    {
        [DataMember(Name = "description")]
        public string Description { get; set; }
        [DataMember(Name = "location")]
        public string Location { get; set; }
    }


    
}
