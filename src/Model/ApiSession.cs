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
    /// <summary>
    /// Disposable api session
    /// </summary>
    public class ApiSession : IDisposable
    {
        protected readonly string _defaultSpaceName = "default";
        public const string AuthHeaderName = "X-EasyMorph-Auth";

        public bool IsClosed { get; internal set; }
        public string SpaceName
        {
            get =>
string.IsNullOrWhiteSpace(_spaceName) ? _defaultSpaceName : _spaceName.ToLower();
            internal set => _spaceName = value;
        }
        public string AuthToken { get;  internal set; }
        public bool IsAnonymous { get; internal set; }

        ICanCloseSession _client;
        private string _spaceName;
        private SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

       /// <summary>
       /// Api session constructor
       /// </summary>
       /// <param name="client">reference to client </param>
        internal ApiSession(ICanCloseSession client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            IsClosed = false;
            IsAnonymous = false;

        }


        internal static ApiSession Anonymous(ICanCloseSession client, string spaceName)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (string.IsNullOrWhiteSpace(spaceName))
            {
                throw new ArgumentException("Value is empty {0}", nameof(spaceName));
            }

            return new ApiSession(client)
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
            // don't close if session is already closed or anon.            
            if(IsClosed || _client == null || IsAnonymous)
            {
                return;
            }
            try
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
            catch (Exception)
            {
                // 
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