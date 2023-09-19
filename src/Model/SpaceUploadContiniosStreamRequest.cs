using System;
using System.Collections.Generic;
using System.IO;
using Morph.Server.Sdk.Model.InternalModels;

namespace Morph.Server.Sdk.Model
{
    public sealed class SpaceUploadContiniousStreamRequest
    {
        public string ServerFolder { get; set; }        
        public string FileName { get; set; }        
        public bool OverwriteExistingFile { get; set; } = false;
        
        /// <summary>
        /// Optional Tag with properties to check with optimistic concurrency control.
        /// If null, then no concurrency control is used.
        /// </summary>
        public ETag IfMatch { get; set; }
    }
}