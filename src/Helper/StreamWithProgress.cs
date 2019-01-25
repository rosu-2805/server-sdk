using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Helper
{
    internal class StreamWithProgress : Stream
    {
        private readonly Stream stream;
        private readonly Action<StreamProgressEventArgs> onReadProgress;
        private readonly Action<StreamProgressEventArgs> onWriteProgress;

        public StreamWithProgress(Stream stream,
            Action<StreamProgressEventArgs> onReadProgress = null,
            Action<StreamProgressEventArgs> onWriteProgress = null)
        {
            this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
            this.onReadProgress = onReadProgress;
            this.onWriteProgress = onWriteProgress;
        }
        public override bool CanRead => stream.CanRead;

        public override bool CanSeek => stream.CanSeek;

        public override bool CanWrite => stream.CanWrite;

        public override long Length => stream.Length;

        public override long Position { get => stream.Position; set => stream.Position = value; }

        public override void Flush()
        {
            stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var bytesRead = stream.Read(buffer, offset, count);
            RaiseOnReadProgress(bytesRead);
            return bytesRead;            
        }

        private void RaiseOnReadProgress(int bytesRead)
        {
            if (onReadProgress != null)
            {
                var args = new StreamProgressEventArgs(bytesRead , stream.Length, stream.Position);
                onReadProgress(args);
            }
        }
        private void RaiseOnWriteProgress(int bytesWrittens)
        {
            if (onWriteProgress != null)
            {
                var args = new StreamProgressEventArgs(bytesWrittens, stream.Length, stream.Position);
                onWriteProgress(args);
            }
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
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            return stream.CopyToAsync(destination, bufferSize, cancellationToken);
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
            var bytesRead = await stream.ReadAsync(buffer, offset, count, cancellationToken);
            RaiseOnReadProgress(bytesRead);
            return bytesRead;
        }
        public override int ReadByte()
        {
            return stream.ReadByte();
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
                stream.Dispose();
            }
        }


    }
}
