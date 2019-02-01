using Morph.Server.Sdk.Helper;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Model
{
    public class ServerPushStreaming:IDisposable
    {
        internal readonly ContiniousSteamingContent steamingContent;
        Action onClose = null;

        //private Func<Stream, CancellationToken, Task> onWriteStream { get; set; }
        //private Action onClose { get; set; }

        internal ServerPushStreaming(ContiniousSteamingContent  steamingContent )
        {
            this.steamingContent = steamingContent;
        }   
        public void Dispose()
        {
            if (onClose != null)
            {
                onClose();
            }
        }

        public async Task WriteStream(Stream stream, CancellationToken cancellationToken)
        {
            //return onWriteStream(stream, cancellationToken); 
            await steamingContent.WriteStream(stream, cancellationToken);
        }

        internal void RegisterOnClose(Action onClose )
        {
            this.onClose = onClose;
                
        }
    }

    
}
