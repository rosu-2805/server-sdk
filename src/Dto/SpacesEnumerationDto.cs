using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Dto
{
    [DataContract]
    internal class SpacesEnumerationDto
    {
        [DataMember(Name = "values")]
        public List<SpaceEnumerationItemDto> Values { get; set; }
        public SpacesEnumerationDto()
        {
            Values = new List<SpaceEnumerationItemDto>();
        }
    }
}
