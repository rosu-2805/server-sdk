using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Morph.Server.Sdk.Helper;
using Morph.Server.Sdk.Model.InternalModels;

namespace Morph.Server.Sdk.Model
{
    public class PushStreamingConnection: IDisposable
    {
        private readonly ContiniousSteamingHttpContent _serverPushStreaming;

        internal PushStreamingConnection(ContiniousSteamingHttpContent serverPushStreaming)
        {
            _serverPushStreaming = serverPushStreaming;
        }

        public void Dispose()
        {
            _serverPushStreaming?.CloseConnetion();
            _serverPushStreaming?.Dispose();
        }

        public async Task WriteStreamAsync(Stream stream, CancellationToken cancellationToken)
        {
            await _serverPushStreaming.WriteStreamAsync(stream, cancellationToken);
        }

    }
}