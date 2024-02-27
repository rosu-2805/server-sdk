using Morph.Server.Sdk.Model.InternalModels;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Morph.Server.Sdk.Helper;

namespace Morph.Server.Sdk.Model
{
    public class ContiniousStreamingConnection: IDisposable
    {
        private readonly ServerPushStreaming serverPushStreaming;

        internal ContiniousStreamingConnection(ServerPushStreaming serverPushStreaming)
        {
            this.serverPushStreaming = serverPushStreaming;
        }

        public void Dispose()
        {
            serverPushStreaming.Dispose();
        }

        public async Task WriteStreamAsync(Stream stream, CancellationToken cancellationToken)
        {
            await serverPushStreaming.WriteStreamAsync(stream, cancellationToken);
        }

    }




}
