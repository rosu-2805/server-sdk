using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Reflection;
using System.Net;

namespace Morph.Server.Sdk.Client
{
    public static class MorphServerApiClientGlobalConfig
    {

        private static object obj = new object();
        public static Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> ServerCertificateCustomValidationCallback { get; set; } =
            (httpRequestMessage, xcert, xchain, sslerror) =>
            {
                if (ServicePointManager.ServerCertificateValidationCallback != null)
                {
                    return ServicePointManager.ServerCertificateValidationCallback(obj, xcert, xchain, sslerror);
                }
                else
                {
                    return false;
                }
            };            

        private const string DefaultClientType = "EMS-SDK";

        public static TimeSpan SessionOpenTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Default operation execution timeout
        /// </summary>
        public static TimeSpan OperationTimeout { get; set; } = TimeSpan.FromMinutes(2);
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

        /// <summary>
        /// dispose client when session is closed
        /// </summary>
        public static bool AutoDisposeClientOnSessionClose { get; set; } = true;


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


