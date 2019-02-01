using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Helper
{
    internal class ContiniousSteamingHttpContent : HttpContent
    {
        internal enum MessageType
        {
            None,
            NewStream,
            Close
        }

        private const int DefBufferSize = 4096;
        private readonly CancellationToken mainCancellation;

        //ManualResetEventSlim resetEventSlim = new ManualResetEventSlim(true);
        volatile SemaphoreSlim hasData = new SemaphoreSlim(0, 1);
        volatile SemaphoreSlim dataProcessed = new SemaphoreSlim(0, 1);

        volatile Stream _stream;
        volatile MessageType _messageType = MessageType.None;
        private CancellationToken cancellationToken;

        internal async Task WriteStream( Stream stream, CancellationToken cancellationToken)
        {

            this._stream = stream;
            this.cancellationToken = cancellationToken;
            this._messageType = MessageType.NewStream;
            hasData.Release(1); //has data->1

            
            await dataProcessed.WaitAsync(Timeout.Infinite, cancellationToken);
       //     dataProcessed.Release();
        }

        internal void CloseConnetion()
        {
            this._messageType = MessageType.Close;
            hasData.Release();
            dataProcessed.Wait(5000);
            //dataProcessed.Release();
        }

        public ContiniousSteamingHttpContent(CancellationToken mainCancellation)
        {
            this.mainCancellation = mainCancellation;
            
        }
        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            var buffer = new byte[DefBufferSize];
            bool canLoop = true;
            while (canLoop)
            {
                //TODO: add cancellation token
                await hasData.WaitAsync(Timeout.Infinite, mainCancellation);
                //  resetEventSlim.Wait();
                switch (this._messageType) {
                    case MessageType.NewStream:
                    using (this._stream)
                    {
                        int bytesRead;
                        while ((bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
                        {
                            await stream.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
                        }


                        dataProcessed.Release(1);



                        //var length = await _stream.ReadAsync(buffer, 0, buffer.Length, mainCancellation);

                        //if (length <= 0) {
                        //    dataProcessed.Release(); ;
                        //    // hasData.Release();
                        //    // resetEventSlim.Reset();
                        //    break;
                        //}

                        //await stream.WriteAsync(buffer, 0, length, mainCancellation);
                    }; break;
                    case MessageType.Close:
                        canLoop = false;
                      
                        break;

                }
                this._messageType = MessageType.None;

              //  this._stream = null;
                
            }
            dataProcessed.Release(1);

        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }
}
