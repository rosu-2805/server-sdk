using System.Runtime.Serialization;

namespace Morph.Server.Sdk.Dto
{
    [DataContract]
    public sealed class WorkflowResultErrorInfoDto
    {
        [DataMember(Name="description")]
        public string Description { get; set; }

        [DataMember(Name="location")]
        public string Location { get; set; }
    }
}