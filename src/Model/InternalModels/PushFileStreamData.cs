using System;
using System.IO;
using Morph.Server.Sdk.Client;

namespace Morph.Server.Sdk.Model.InternalModels
{
    public sealed class PushFileStreamData
    {
        public PushStreamCallback PushCallback { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }

        /// <summary>
        /// Optional Tag with properties to check with optimistic concurrency control.
        /// If null, then no concurrency control is used.
        /// </summary>
        public ETag IfMatch { get; set; } = null;
    }
}