using System.Collections.Generic;

namespace Morph.Server.Sdk.Model
{
    public sealed class SpacesLookupRequest
    {
        public List<string> SpaceNames { get; set; } = new List<string>();        
    }


    public sealed class SpacesLookupResponse
    {
        public List<SpacesLookupResult> Values { get; set; } = new List<SpacesLookupResult>();
    }

    public sealed class SpacesLookupResult : LookupResultItem<SpaceEnumerationItem>
    {
        public string SpaceName { get; set; }
    }

}
