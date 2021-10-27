using System.Runtime.Serialization;

namespace Morph.Server.Sdk.Dto.Computations
{
    [DataContract]
    internal class ComputationStateDataDto
    {
        [DataMember(Name = "resultObtainingToken")]
        public string ResultObtainingToken { get; set; }

    }
}