using Morph.Server.Sdk.Events;
using Morph.Server.Sdk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Helper
{

    internal class FileProgress : IFileProgress
    {
        public event EventHandler<FileTransferProgressEventArgs> StateChanged;

        public long FileSize { get; private set; }
        public string FileName { get; private set; }        
        public long ProcessedBytes { get; private set; }
        public FileProgressState State { get; private set; }

        public void ChangeState(FileProgressState state)
        {
            State = state;
            StateChanged?.Invoke(this, new FileTransferProgressEventArgs
            {
                ProcessedBytes = ProcessedBytes,
                State = state,
                FileName = FileName,
                FileSize = FileSize

            });
        }
        public void SetProcessedBytes(long np)
        {
            ProcessedBytes = np;
        }

        public FileProgress(string fileName, long fileSize)
        {
            FileName = fileName;
            FileSize = fileSize;            
        }
    }
}
