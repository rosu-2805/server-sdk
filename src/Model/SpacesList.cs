using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Model
{
    public class SpacesList
    {
        public List<SpaceListItem> Items { get; internal set; } = new List<SpaceListItem>();
    }

    public class SpaceListItem
    {
        public string SpaceName { get; internal set; }
        public bool IsPublic { get; internal set; }
    }
}
