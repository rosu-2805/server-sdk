using System.Runtime.Serialization;

namespace Morph.Server.Sdk.Dto
{
    [DataContract]
    internal class WindowsExternalLoginRequestDto
    {
        [DataMember(Name = "requestToken")]
        public string RequestToken { get; set; }
        [DataMember(Name = "spaceName")]
        public string SpaceName { get; set; }
    }
}
