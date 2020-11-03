using System;
using System.IO;

namespace Morph.Server.Sdk.Model.InternalModels
{
    public sealed class FetchFileStreamData 
    {
        public FetchFileStreamData(Stream stream, string fileName, long? fileSize)
        {
            Stream = stream ?? throw new ArgumentNullException(nameof(stream));
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            FileSize = fileSize;
        }

        public Stream Stream { get;}
        public string FileName { get; }
        public long? FileSize { get; }

    }



}