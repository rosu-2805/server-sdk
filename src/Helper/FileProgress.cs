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
        private readonly Action<FileTransferProgressEventArgs> onProgress;

        //public event EventHandler<FileTransferProgressEventArgs> StateChanged;

        public long FileSize { get; private set; }
        public string FileName { get; private set; }        
        public long ProcessedBytes { get; private set; }
        public FileProgressState State { get; private set; }

        public void ChangeState(FileProgressState state)
        {
            State = state;
            onProgress?.Invoke(new FileTransferProgressEventArgs
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
            if(ProcessedBytes!= FileSize)
            {
                ChangeState(FileProgressState.Processing);
            }
            if(ProcessedBytes == FileSize && State !=FileProgressState.Finishing)
            {
                ChangeState(FileProgressState.Finishing);
            }
        }

        public FileProgress(string fileName, long fileSize, Action<FileTransferProgressEventArgs> onProgress)
        {
            FileName = fileName;
            FileSize = fileSize;
            this.onProgress = onProgress;
        }
    }
}
