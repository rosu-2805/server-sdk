using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Dto
{
    [DataContract]
    internal class TaskStartRequestDto
    {
        [DataMember(Name = "taskParameters")]
        public IList<TaskParameterDto> TaskParameters { get; set; } = new List<TaskParameterDto>();
    }
}
