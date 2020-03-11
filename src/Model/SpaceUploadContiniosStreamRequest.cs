using System;
using System.Collections.Generic;
using System.IO;

namespace Morph.Server.Sdk.Model
{
    public sealed class SpaceUploadContiniousStreamRequest
    {
        public string ServerFolder { get; set; }        
        public string FileName { get; set; }        
        public bool OverwriteExistingFile { get; set; } = false;
    }
}