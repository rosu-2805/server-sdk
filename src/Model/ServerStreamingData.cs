using System;
using System.IO;

namespace Morph.Server.Sdk.Model
{
    public sealed class ServerStreamingData : IDisposable
    {
        public ServerStreamingData(Stream stream, string fileName, long? fileSize)
        {
            Stream = stream ?? throw new ArgumentNullException(nameof(stream));
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            FileSize = fileSize;
        }

        public Stream Stream { get; private set; }
        public string FileName { get; }
        public long? FileSize { get; }
        public void Dispose()
        {
            if (Stream != null)
            {
                Stream.Dispose();
                Stream = null;
            }
        }
    }
}