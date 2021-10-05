using System.Runtime.Serialization;

namespace Morph.Server.Sdk.Dto.Computations
{
    [DataContract]
    internal class ComputationStateDto
    {
        /// <summary>
        /// see ComputationState enum
        /// </summary>
        [DataMember(Name = "type")] 
        public string Type { get; set; }
        [DataMember(Name = "data")]
        public ComputationStateDataDto Data { get; set; }

    }
}