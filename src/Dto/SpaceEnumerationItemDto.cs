using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Dto
{
    [DataContract]
    internal class SpaceEnumerationItemDto
    {
        [DataMember(Name = "spaceName")]
        public string SpaceName { get; set; }
        [DataMember(Name = "isPublic")]
        public bool IsPublic { get; set; }
        [DataMember(Name = "spaceAccessRestriction")]
        public string SpaceAccessRestriction { get; set; } 

    }
}
