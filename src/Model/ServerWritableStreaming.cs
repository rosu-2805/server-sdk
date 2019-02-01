using Morph.Server.Sdk.Helper;
using Morph.Server.Sdk.Model.InternalModels;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Model
{
    public class ServerPushStreaming : IDisposable
    {
        internal readonly ContiniousSteamingHttpContent steamingContent;
        //Action onClose = null;
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
                //if (onClose != null)
                //{

                //    onClose();
                SemaphoreSlim.Wait(10000);
                if (DataException != null)
                {
                    throw DataException;
                }
                //}
            }
            finally
            {
                SemaphoreSlim.Dispose();
                SemaphoreSlim = null;
                _closed = true;
            }
        }

        public async Task WriteStream(Stream stream, CancellationToken cancellationToken)
        {
            //return onWriteStream(stream, cancellationToken); 
            await steamingContent.WriteStream(stream, cancellationToken);
        }

        //internal void RegisterOnClose(Action onClose)
        //{
        //    this.onClose = onClose;

        //}

        public Exception DataException { get; private set; }

        private object dataResult = null;

        public TResult GetData<TResult>()
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
