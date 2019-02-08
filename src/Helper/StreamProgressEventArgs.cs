using System;

namespace Morph.Server.Sdk.Helper
{
    internal class StreamProgressEventArgs : EventArgs
    {        
        public long TotalBytesRead { get;  }
        public int BytesRead { get; }

        public StreamProgressEventArgs()
        {

        }
        public StreamProgressEventArgs(long totalBytesRead, int bytesRead) :this()
        {
            TotalBytesRead = totalBytesRead;
            BytesRead = bytesRead;
        }
    }
}
