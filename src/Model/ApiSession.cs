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
        internal bool IsClosed { get; set; }
        public string SpaceName { get; internal set; }
        internal string AuthToken { get; set; }
        internal bool IsAnonymous { get; set; }

        WeakReference<MorphServerApiClient> _client;
        internal ApiSession(MorphServerApiClient client)
        {
            _client = new WeakReference<MorphServerApiClient>(client);
            IsClosed = false;
            IsAnonymous = false;

        }


        public static ApiSession Anonymous(string spaceName)
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
                if (_client.TryGetTarget(out var target))
                {
                    Task.Run(async () =>
                    {
                        var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(5));
                        await target.CloseSession(this, cts.Token);
                    }

                        ).Wait(TimeSpan.FromSeconds(5));

                    this.IsClosed = true;
                }
            }
            catch (Exception)
            {

            }
        }
    }
}
