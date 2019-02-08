using Morph.Server.Sdk.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Model
{

    public class ApiSession : IDisposable
    {
        protected readonly string _defaultSpaceName = "default";
        internal const string AuthHeaderName = "X-EasyMorph-Auth";

        internal bool IsClosed { get; set; }
        public string SpaceName
        {
            get =>
string.IsNullOrWhiteSpace(_spaceName) ? _defaultSpaceName : _spaceName.ToLower();
            internal set => _spaceName = value;
        }
        internal string AuthToken { get; set; }
        internal bool IsAnonymous { get; set; }

        ICanCloseSession _client;
        private string _spaceName;
        private SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        internal ApiSession(ICanCloseSession client)
        {
            _client = client;
            IsClosed = false;
            IsAnonymous = false;

        }


        internal static ApiSession Anonymous(string spaceName)
        {

            if (string.IsNullOrWhiteSpace(spaceName))
            {
                throw new ArgumentException("Value is empty {0}", nameof(spaceName));
            }

            return new ApiSession(null)
            {
                IsAnonymous = true,
                IsClosed = false,
                SpaceName = spaceName
            };

        }


        public async Task CloseSessionAsync(CancellationToken cancellationToken)
        {

            await _lock.WaitAsync(cancellationToken);
            try
            {
                await _InternalCloseSessionAsync(cancellationToken);
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task _InternalCloseSessionAsync(CancellationToken cancellationToken)
        {
            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                linkedCts.CancelAfter(TimeSpan.FromSeconds(5));
                await _client.CloseSessionAsync(this, linkedCts.Token);

                // don't dispose client implicitly, just remove link to client
                if (_client.Config.AutoDisposeClientOnSessionClose)
                {
                    _client.Dispose();
                }
                _client = null;

                IsClosed = true;
            }
        }

        public void Dispose()
        {
            try
            {
                if (_lock != null)
                {
                    _lock.Wait(5000);
                    try
                    {
                        if (!IsClosed && _client != null)
                        {
                            Task.Run(async () =>
                            {
                                try
                                {
                                    await _InternalCloseSessionAsync(CancellationToken.None);
                                }
                                catch (Exception ex)
                                {

                                }
                            }).Wait(TimeSpan.FromSeconds(5));


                        }

                    }
                    finally
                    {
                        _lock.Release();
                        _lock.Dispose();
                        _lock = null;
                    }
                }
            }
            catch (Exception)
            {

            }
        }
    }
}