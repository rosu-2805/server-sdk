using System;

namespace Morph.Server.Sdk.Model.SharedMemory
{
    /// <summary>
    /// Represents an element in shared memory list response
    /// </summary>
    public class SharedMemoryListRecord
    {
        /// <summary>
        /// Key of the record
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Value of the record
        /// </summary>
        public SharedMemoryValue Value { get; set; }

        /// <summary>
        /// Author of the record, if available
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// Date and time of creation of the record, if available
        /// </summary>
        public DateTime? Modified { get; set; }
    }
}