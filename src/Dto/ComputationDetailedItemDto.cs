using System.Runtime.Serialization;
using Morph.Server.Sdk.Dto.Computations;

namespace Morph.Server.Sdk.Dto
{
    [DataContract]
    internal class ComputationDetailedItemDto
    {
        [DataMember(Name = "computationId")]
        public string ComputationId { get; set; }
        [DataMember(Name = "startTimestamp")]
        public string StartTimestamp { get; set; }
        [DataMember(Name = "spaceName")]
        public string SpaceName { get; set; }
        [DataMember(Name = "projectDetails")]
        public ProjectDetailsInfoDto projectDetails { get; set; }
        [DataMember(Name="state")]
        public ComputationStateDto State { get; set; }
        [DataMember(Name="statusText")]
        public string StatusText { get; set; }

       
    }
}