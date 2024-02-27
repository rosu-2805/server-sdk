using System.Collections.Generic;

namespace Morph.Server.Sdk.Model.SharedMemory
{
    public class SharedMemoryListResponse
    {
        public List<SharedMemoryListRecord> Items { get; set; } = new List<SharedMemoryListRecord>();
        
        public bool HasMore { get; set; }
    }
}