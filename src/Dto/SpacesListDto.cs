using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Dto
{
    [DataContract]
    internal class SpacesListDto
    {
        [DataMember(Name = "values")]
        public List<SpaceListItemDto> Values { get; set; }
        public SpacesListDto()
        {
            Values = new List<SpaceListItemDto>();
        }
    }
}
