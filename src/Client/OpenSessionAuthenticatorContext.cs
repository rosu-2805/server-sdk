using System;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Morph.Server.Sdk.Client
{
    internal class OpenSessionAuthenticatorContext
    {

        public OpenSessionAuthenticatorContext
            (ILowLevelApiClient lowLevelApiClient,
            IMorphServerApiClient morphServerApiClient,
            Func<HttpClientHandler, IApiClient> buildApiClient
            
            )
        {
            LowLevelApiClient = lowLevelApiClient ?? throw new ArgumentNullException(nameof(lowLevelApiClient));
            MorphServerApiClient = morphServerApiClient ?? throw new ArgumentNullException(nameof(morphServerApiClient));
            BuildApiClient = buildApiClient ?? throw new ArgumentNullException(nameof(buildApiClient));
         
        }

        public ILowLevelApiClient LowLevelApiClient { get; }
        public IMorphServerApiClient MorphServerApiClient { get; }
        public Func<HttpClientHandler, IApiClient> BuildApiClient { get; }
        
    }
}


