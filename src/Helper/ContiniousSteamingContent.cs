using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Helper
{
    internal class ContiniousSteamingHttpContent : HttpContent
    {
        /// <summary>
        ///  message type for processing
        /// </summary>
        internal enum MessageType
        {
            /// <summary>
            /// no new messages
            /// </summary>
            None,
            /// <summary>
            /// push new stream to server
            /// </summary>
            NewStream,
            /// <summary>
            /// Close connection
            /// </summary>
            CloseConnection
        }

        private const int DefBufferSize = 4096;
        private readonly CancellationToken mainCancellation;

        
        /// <summary>
        /// cross-thread flag, that new message need to be processed
        /// </summary>
        volatile SemaphoreSlim hasData = new SemaphoreSlim(0, 1);
        /// <summary>
        /// cross-thread flag that new message has been processed
        /// </summary>
        volatile SemaphoreSlim dataProcessed = new SemaphoreSlim(0, 1);

        /// <summary>
        /// stream to process
        /// </summary>
        volatile Stream _stream;
        /// <summary>
        /// Message to process
        /// </summary>
        volatile MessageType _currentMessage = MessageType.None;
        private CancellationToken cancellationToken;

        internal async Task WriteStreamAsync( Stream stream, CancellationToken cancellationToken)
        {
            if(_currentMessage != MessageType.None)
            {
                throw new System.Exception("Another message is processing by the ContiniousSteamingHttpContent handler. ");
            }

            this._stream = stream;
            this.cancellationToken = cancellationToken;
            this._currentMessage = MessageType.NewStream;

            // set flag that new data has been arrived
            hasData.Release(1); //has data->1         
            // await till all data will be send by another thread. Another thread will trigger dataProcessed semaphore
            await dataProcessed.WaitAsync(Timeout.Infinite, cancellationToken);
       
        }

        internal void CloseConnetion()
        {
            // if cancellation token has been requested, it is not necessary to send message CloseConnection
            if (!mainCancellation.IsCancellationRequested)
            {
                this._currentMessage = MessageType.CloseConnection;
                // send message that data is ready
                hasData.Release();
                // wait until it has been processed
                dataProcessed.Wait(5000);
            }
            
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
                
                // await new data
                await hasData.WaitAsync(Timeout.Infinite, mainCancellation);
                // data has been arrived. check _currentMessage field
                switch (this._currentMessage) {
                    // upload stream 
                    case MessageType.NewStream:
                    using (this._stream)
                    {
                        int bytesRead;
                        while ((bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
                        {
                            await stream.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
                        }

                        // set that data has been processed
                        dataProcessed.Release(1);

                    }; break;
                    case MessageType.CloseConnection:
                        // close loop. dataProcessed flag will be triggered at the end of this function.
                        canLoop = false;
                      
                        break;

                }
                this._currentMessage = MessageType.None;              
                
            }
            dataProcessed.Release(1);

        }

        protected override bool TryComputeLength(out long length)
        {
            // continuous stream length is unknown
            length = 0;
            return false;
        }
    }
}
