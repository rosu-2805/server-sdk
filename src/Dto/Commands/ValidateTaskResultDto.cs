using Morph.Server.Sdk.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Dto.Commands
{
    [DataContract()]
    internal class ValidateTasksResponseDto
    {
        [DataMember(Name = "failedTasks")]
        public List<FailedTaskInfoDto> FailedTasks { get; set; }
        [DataMember(Name = "message")]
        public string Message { get; set; }

        public ValidateTasksResponseDto()
        {
            FailedTasks = new List<FailedTaskInfoDto>();            
        }
    }
    [DataContract]
    public class FailedTaskInfoDto
    {
        [DataMember(Name = "taskId")]
        public string TaskId { get; set; }
        [DataMember(Name = "missingParameters")]
        public List<string> MissingParameters { get; set; }
        [DataMember(Name = "taskWebUrl")]
        public string TaskWebUrl { get; set; }
        [DataMember(Name = "taskApiUrl")]
        public string TaskApiUrl { get; set; }
        [DataMember(Name = "message")]
        public string Message { get; set; }

        public FailedTaskInfoDto()
        {
            MissingParameters = new List<string>();
        }
    }
}
