using Morph.Server.Sdk.Client;
using System;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Morph.Server.Sdk.Model.InternalModels
{
    internal class OpenSessionAuthenticatorContext
    {

        public OpenSessionAuthenticatorContext
            (ILowLevelApiClient lowLevelApiClient,
            IMorphServerApiClient morphServerApiClient,
            Func<HttpClientHandler, IRestClient> buildApiClient
            
            )
        {
            LowLevelApiClient = lowLevelApiClient ?? throw new ArgumentNullException(nameof(lowLevelApiClient));
            MorphServerApiClient = morphServerApiClient ?? throw new ArgumentNullException(nameof(morphServerApiClient));
            BuildApiClient = buildApiClient ?? throw new ArgumentNullException(nameof(buildApiClient));
         
        }

        public ILowLevelApiClient LowLevelApiClient { get; }
        public IMorphServerApiClient MorphServerApiClient { get; }
        public Func<HttpClientHandler, IRestClient> BuildApiClient { get; }
        
    }
}


