using System.Collections.Generic;
using System.Runtime.Serialization;
using Morph.Server.Sdk.Dto;

namespace Morph.Server.Sdk.Dto.Computations
{


    [DataContract]
    internal class ComputationDto
    {
        [DataMember(Name = "computationId")] 
        public string ComputationId { get; set; }

        [DataMember(Name = "startTimestamp")] 
        public string StartTimestamp { get; set; }

        [DataMember(Name = "state")] 
        public ComputationStateDto State { get; set; }
        
        [DataMember(Name = "statusText")] 
        public string StatusText { get; set; } = string.Empty;
        
    }
}