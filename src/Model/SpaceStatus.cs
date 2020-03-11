using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Model
{
    public class SpaceStatus
    {
        public string SpaceName { get; internal set; }
        public bool IsPublic { get; internal set; }
        public IReadOnlyList<UserSpacePermission> UserPermissions { get; internal set; }
    }
}
