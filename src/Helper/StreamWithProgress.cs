using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Helper
{
    internal class StreamWithProgress : Stream
    {
        internal enum TokenCancellationReason
        {
            HttpTimeoutToken,
            OperationCancellationToken
        }

        private readonly Stream stream;
        private readonly long? streamLength;
        private readonly Action<StreamProgressEventArgs> onReadProgress;
        private readonly Action<StreamProgressEventArgs> onWriteProgress = null;
        private readonly Action onDisposed;
        private readonly Action<TokenCancellationReason, CancellationToken> onTokenCancelled;
        private readonly CancellationToken httpTimeoutToken;
        private DateTime _lastUpdate = DateTime.MinValue;
        private long _readPosition = 0;

        private bool _disposed = false;
        
        public StreamWithProgress(Stream httpStream,
            long? streamLength,
            CancellationToken mainTokem,
            Action<StreamProgressEventArgs> onReadProgress = null,
            Action onDisposed = null,
            Action<TokenCancellationReason, CancellationToken> onTokenCancelled = null)
        {
            this.stream = httpStream ?? throw new ArgumentNullException(nameof(httpStream));
            this.streamLength = streamLength;
            this.onReadProgress = onReadProgress;

            this.onDisposed = onDisposed;
            this.onTokenCancelled = onTokenCancelled;
            this.httpTimeoutToken = mainTokem;
        }
        
        
        public override bool CanRead => stream.CanRead;

        public override bool CanSeek => stream.CanSeek;

        public override bool CanWrite => stream.CanWrite;

        public override long Length => streamLength ?? 1;

        public override long Position { get => _readPosition; set => throw new NotImplementedException(); }

        public override void Flush()
        {
            stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (httpTimeoutToken.IsCancellationRequested)
            {
                onTokenCancelled(TokenCancellationReason.HttpTimeoutToken, httpTimeoutToken);
            }
            var bytesRead = stream.Read(buffer, offset, count);

            IncrementReadProgress(bytesRead);
            return bytesRead;
        }

        private void IncrementReadProgress(int bytesRead)
        {
            _readPosition += bytesRead;
            var totalBytesRead = _readPosition;

            if (onReadProgress != null)
            {
                if (DateTime.Now - _lastUpdate > TimeSpan.FromMilliseconds(500) || bytesRead == 0)
                {
                    _lastUpdate = DateTime.Now;
                    var args = new StreamProgressEventArgs(totalBytesRead, bytesRead);
                    onReadProgress(args);

                }
            }
        }
        private void RaiseOnWriteProgress(int bytesWrittens)
        {
            //if (onWriteProgress != null)
            //{
            //    var args = new StreamProgressEventArgs(bytesWrittens);
            //    onWriteProgress(args);
            //}
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            stream.Write(buffer, offset, count);
            RaiseOnWriteProgress(count);
        }
        public override bool CanTimeout => stream.CanTimeout;
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
            // return stream.BeginRead(buffer, offset, count, callback, state);
        }
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
            //return stream.BeginWrite(buffer, offset, count, callback, state);
        }
        public override void Close()
        {
            stream.Close();
            base.Close();
        }
        public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[bufferSize];
            int bytesRead;
            while ((bytesRead = await ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
            {
                if (httpTimeoutToken.IsCancellationRequested)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        onTokenCancelled(TokenCancellationReason.OperationCancellationToken, cancellationToken);
                    }
                    else
                    {
                        onTokenCancelled(TokenCancellationReason.HttpTimeoutToken, httpTimeoutToken);
                    }
                }
                await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
            }
        }
        public override int EndRead(IAsyncResult asyncResult)
        {
            return stream.EndRead(asyncResult);
        }
        public override void EndWrite(IAsyncResult asyncResult)
        {
            stream.EndWrite(asyncResult);
        }
        public override bool Equals(object obj)
        {
            return stream.Equals(obj);
        }
        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return stream.FlushAsync(cancellationToken);
        }
        public override int GetHashCode()
        {
            return stream.GetHashCode();
        }
        public override object InitializeLifetimeService()
        {
            return stream.InitializeLifetimeService();
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (httpTimeoutToken.IsCancellationRequested)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    onTokenCancelled(TokenCancellationReason.OperationCancellationToken, cancellationToken);
                }
                else
                {
                    onTokenCancelled(TokenCancellationReason.HttpTimeoutToken, httpTimeoutToken);
                }
            }
            var bytesRead = await stream.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
            IncrementReadProgress(bytesRead);
            return bytesRead;
        }
        public override int ReadByte()
        {
            var @byte = stream.ReadByte();
            if (@byte != -1)
            {
                IncrementReadProgress(1);
            }
            return @byte;
        }
        public override int ReadTimeout { get => stream.ReadTimeout; set => stream.ReadTimeout = value; }
        public override string ToString()
        {
            return stream.ToString();
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await stream.WriteAsync(buffer, offset, count, cancellationToken);
            RaiseOnWriteProgress(count);
        }
        public override void WriteByte(byte value)
        {
            stream.WriteByte(value);
            RaiseOnWriteProgress(1);
        }
        public override int WriteTimeout { get => stream.WriteTimeout; set => stream.WriteTimeout = value; }

        protected override void Dispose(bool disposing)
        {
            
            if (disposing)
            {
                if (!_disposed)
                {
                    _disposed = true;

                    stream.Dispose();
                    if (onDisposed != null)
                    {
                        onDisposed();
                    }
                }
            }
        }


    }
}
