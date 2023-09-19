using System;

namespace Morph.Server.Sdk.Dto.Upload
{
    public class UploadedFileETagDto
    {
        public long? LastUpdatedUnixTime { get; set; }
        public long? Size { get; set; }
        
        public static UploadedFileETagDto None => new UploadedFileETagDto();
    }
}