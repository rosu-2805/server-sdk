using Morph.Server.Sdk.Model;
using System;

namespace Morph.Server.Sdk.Events
{
    public class FileTransferProgressEventArgs : EventArgs
    {
        public FileProgressState State { get; set; }
        public long ProcessedBytes { get; set; }
        public long FileSize { get; set; }
        //public Guid? Guid { get; set; }
        public string FileName { get; set; }
        public double Percent
        {
            get
            {
                if (FileSize == 0)
                    return 0;
                return Math.Round((ProcessedBytes * 100.0 / FileSize), 2);
            }
        }
        public FileTransferProgressEventArgs()
        {


        }

    }
}
