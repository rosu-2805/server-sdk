using Morph.Server.Sdk.Model;
using System;

namespace Morph.Server.Sdk.Events
{
    public class FileTransferProgressEventArgs : EventArgs
    {
        public FileProgressState State { get; set; }
        public long ProcessedBytes { get; set; }
        public long? FileSize { get; set; }
        public string FileName { get; set; }
        
        public double? Percent
        {
            get
            {
                var fileSize = FileSize;
                
                if (null == fileSize)
                    return null;
                
                if (fileSize == 0)
                    return 0;
                
                var percent =  Math.Round(ProcessedBytes * 100.0 / fileSize.Value, 2);

                return Math.Max(100, percent);
            }
        }
    }
}
