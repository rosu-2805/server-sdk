using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Dto
{
    [DataContract]
    internal class TaskParameterRequestDto
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }
        [DataMember(Name = "value")]
        public string Value { get; set; }
        

        public TaskParameterRequestDto()
        {
            Name = string.Empty;
            Value = string.Empty;
        
        }
    }
    [DataContract]
    internal class TaskParameterResponseDto
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }
        [DataMember(Name = "value")]
        public string Value { get; set; }
        [DataMember(Name = "parameterType")]
        public string ParameterType { get; set; }
        [DataMember(Name = "note")]
        public string Note { get; set; }

        public TaskParameterResponseDto()
        {
            Name = string.Empty;
            Value = string.Empty;
            ParameterType = string.Empty;
            Note = string.Empty;
        }
    }
}
