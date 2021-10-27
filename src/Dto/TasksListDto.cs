using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Morph.Server.Sdk.Dto
{
    [DataContract]
    internal class TasksListDto
    {
        [DataMember(Name = "values")]
        public List<TaskShortDto> Values { get; set; }
        public TasksListDto()
        {
            Values = new List<TaskShortDto>();
        }
    }
}
