using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Morph.Server.Sdk.Dto
{
    [DataContract]
    internal class SpaceTaskChangeModeRequestDto
    {
        [DataMember(Name = "taskEnabled")]
        public bool? TaskEnabled { get; set; } = null;
    }
}
