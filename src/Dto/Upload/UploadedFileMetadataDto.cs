using System;

namespace Morph.Server.Sdk.Dto.Upload
{
    public class UploadedFileMetadataDto
    {
        public UploadedFileETagDto IfMatch { get; set; } = null;
    }
}