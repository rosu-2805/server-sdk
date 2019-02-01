using System;

namespace Morph.Server.Sdk.Helper
{
    internal class StreamProgressEventArgs : EventArgs
    {        
        public int BytesProcessed { get;  }             
        
        public StreamProgressEventArgs()
        {

        }
        public StreamProgressEventArgs(int bytesProcessed):this()
        {
            BytesProcessed = bytesProcessed;
        }
    }
}
