using Morph.Server.Sdk.Helper;
using Morph.Server.Sdk.Model.InternalModels;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Model.InternalModels
{
    internal class ServerPushStreaming : IDisposable
    {
        internal readonly ContiniousSteamingHttpContent steamingContent;
        
        private bool _closed = false;
        volatile SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(0, 1);

        internal ServerPushStreaming(ContiniousSteamingHttpContent steamingContent)
        {
            this.steamingContent = steamingContent;
        }
        public void Dispose()
        {
            Close();
        }

        private void Close()
        {
            if (_closed)
                return;
            try
            {

                this.steamingContent.CloseConnetion();
              

                SemaphoreSlim.Wait(10000);
                if (DataException != null)
                {
                    throw DataException;
                }
                
            }
            finally
            {
                SemaphoreSlim.Dispose();
                SemaphoreSlim = null;
                _closed = true;
            }
        }

        public async Task WriteStreamAsync(Stream stream, CancellationToken cancellationToken)
        {         
            await steamingContent.WriteStreamAsync(stream, cancellationToken);
        }

        public Exception DataException { get; private set; }

        private object dataResult = null;

        public TResult CloseAndGetData<TResult>()
        {
            Close();

            if (dataResult is TResult f)
            {
                return f;
            }
            else
            {
                return default(TResult);
            }
        }

        internal void SetApiResult<TResult>(ApiResult<TResult> apiResult) where TResult : new()
        {
            if (apiResult.IsSucceed)
            {
                dataResult = apiResult.Data;

            }
            else
            {
                DataException = apiResult.Error;
            }

            SemaphoreSlim.Release();
        }
    }




}
