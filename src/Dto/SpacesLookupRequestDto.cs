using Morph.Server.Sdk.Dto.Errors;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Morph.Server.Sdk.Dto
{
    [DataContract]
    internal class SpacesLookupRequestDto
    {
        [DataMember(Name = "spaceNames")]
        public List<string> SpaceNames { get; set; } = new List<string>();
    }

    [DataContract]
    internal class SpacesLookupResponseDto
    {
        [DataMember(Name = "values")]
        public List<SpacesLookupResultDto> Values { get; set; } = new List<SpacesLookupResultDto>();
    }


    [DataContract]
    internal class LookupResultItemDto<T>
    {

        [DataMember(Name = "error")]
        public Error Error { get; set; }
        [DataMember(Name = "data")]
        public T Data { get; set; }
    }

    [DataContract]
    internal class SpacesLookupResultDto: LookupResultItemDto<SpaceEnumerationItemDto>
    {
        [DataMember(Name = "spaceName")]
        public string SpaceName { get; set; }
    }
}
