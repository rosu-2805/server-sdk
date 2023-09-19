using System;

namespace Morph.Server.Sdk.Model.InternalModels
{
    public class ETag
    {
        public long? LastUpdatedUnixTime { get; set; }
        public long? Size { get; set; }
    }
}