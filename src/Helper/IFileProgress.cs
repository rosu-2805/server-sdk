using Morph.Server.Sdk.Events;
using Morph.Server.Sdk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Helper
{
    internal interface IFileProgress
    {
        event EventHandler<FileTransferProgressEventArgs> StateChanged;
        FileProgressState State { get; }
        long FileSize { get; }
        string FileName { get; }
        void SetProcessedBytes(long np);
        void ChangeState(FileProgressState state);



    }
}
