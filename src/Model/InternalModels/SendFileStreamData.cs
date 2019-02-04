using System;
using System.IO;

namespace Morph.Server.Sdk.Model.InternalModels
{
    internal sealed class SendFileStreamData
    {
        public SendFileStreamData(Stream stream, string fileName, long fileSize)
        {
            Stream = stream ?? throw new ArgumentNullException(nameof(stream));
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            FileSize = fileSize;
        }

        public Stream Stream { get; }
        public string FileName { get; }
        public long FileSize { get; }
    }

    



}