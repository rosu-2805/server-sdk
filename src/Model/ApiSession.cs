using Morph.Server.Sdk.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Model
{


    public class ApiSession : IDisposable
    {
        protected readonly string _defaultSpaceName = "default";
        internal const string AuthHeaderName = "X-EasyMorph-Auth";

        internal bool IsClosed { get; set; }
        public string SpaceName { get => 
                string.IsNullOrWhiteSpace(_spaceName) ? _defaultSpaceName : _spaceName.ToLower();
                internal set => _spaceName = value; }
        internal string AuthToken { get; set; }
        internal bool IsAnonymous { get; set; }

        IMorphServerApiClient _client;
        private string _spaceName;

        internal ApiSession(IMorphServerApiClient client)
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



        public void Dispose()
        {
            try
            {
                if (!IsClosed && _client!=null)
                {
                    Task.Run(async () =>
                    {
                        var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(5));
                        await _client.CloseSessionAsync(this, cts.Token);
                        // don't dispose client implicitly, just remove link to client
                        //_client.Dispose();
                        _client = null;
                    }

                        ).Wait(TimeSpan.FromSeconds(5));

                    this.IsClosed = true;
                }
            }
            catch (Exception ex)
            {

            }
        }
    }
}
