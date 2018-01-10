using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Morph.Server.Sdk.Dto
{
    [DataContract]
    internal class SpaceTasksListDto
    {
        [DataMember(Name = "values")]
        public List<SpaceTasksListItemDto> Values { get; set; }
        public SpaceTasksListDto()
        {
            Values = new List<SpaceTasksListItemDto>();
        }
    }
}
