using System.Collections.Generic;

namespace Morph.Server.Sdk.Model
{
    public sealed class SpaceTasksList
    {
        public List<SpaceTasksListItem> Items { get; internal set; } = new List<SpaceTasksListItem>();
    }

}
