using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Reflection;

namespace Morph.Server.Sdk.Client
{
    public static class MorphServerApiClientGlobalConfig
    {
#if NETSTANDARD2_0
        public static Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> ServerCertificateCustomValidationCallback { get; set; }
#endif

        private const string DefaultClientType = "EMS-SDK";

        /// <summary>
        /// Default operation execution timeout
        /// </summary>
        public static TimeSpan OperationTimeout { get; set; } = TimeSpan.FromSeconds(30);
        /// <summary>
        /// Default File transfer operation timeout
        /// </summary>
        public static TimeSpan FileTransferTimeout { get; set; } = TimeSpan.FromHours(3);

        /// <summary>
        /// HttpClient Timeout
        /// </summary>
        public static TimeSpan HttpClientTimeout { get; set; } = TimeSpan.FromHours(24);

        // additional parameter for  client identification
        public static string ClientId { get; set; } = string.Empty;
        public static string ClientType { get; set; } = DefaultClientType;

        // "Morph.Server.Sdk/x.x.x.x"
        internal static string SDKVersionString { get; }


        static MorphServerApiClientGlobalConfig()
        {
            // set sdk version string 
            // "Morph.Server.Sdk/x.x.x.x"
            Assembly thisAssem = typeof(MorphServerApiClientGlobalConfig).Assembly;
            var assemblyVersion = thisAssem.GetName().Version;
            SDKVersionString = "Morph.Server.Sdk/" + assemblyVersion.ToString();

        }



    }
}


