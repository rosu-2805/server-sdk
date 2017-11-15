using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Dto
{
    [DataContract]
    internal class SpaceListItemDto
    {
        [DataMember(Name = "spaceName")]
        public string SpaceName { get; set; }
        [DataMember(Name = "isPublic")]
        public bool IsPublic { get; set; }
    }
}
