using System;
using System.Collections.Generic;
using System.IO;

namespace Morph.Server.Sdk.Model
{
    public sealed class SpaceUploadFileRequest
    {
        public string ServerFolder { get; set; }
        public Stream DataStream { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public bool OverwriteExistingFile { get; set; } = false;
    }
}