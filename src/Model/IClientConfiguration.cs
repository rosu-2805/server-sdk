using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using Morph.Server.Sdk.Client;

namespace Morph.Server.Sdk.Model
{
    public interface IClientConfiguration
    {
        TimeSpan OperationTimeout { get; }
        TimeSpan FileTransferTimeout { get; }
        TimeSpan HttpClientTimeout { get; }
        TimeSpan SessionOpenTimeout { get; }

        string ClientId { get; }
        string ClientType { get; }

        bool AutoDisposeClientOnSessionClose { get; }        

        Uri ApiUri { get; }
        HttpSecurityState HttpSecurityState { get; }

#if NETSTANDARD2_0
        Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> ServerCertificateCustomValidationCallback { get; }
#endif
    }
}


