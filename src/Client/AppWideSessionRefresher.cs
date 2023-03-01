using System;
using System.Threading;

namespace Morph.Server.Sdk.Client
{
    /// <summary>
    ///     Provides one-per-AppDomain session refresher to be used as sensible default/fallback refresher
    /// </summary>
    internal static class AppWideSessionRefresher
    {
        private static readonly Lazy<ApiSessionRefresher> Provider = new Lazy<ApiSessionRefresher>(
            () => new ApiSessionRefresher(),
            LazyThreadSafetyMode.ExecutionAndPublication);

        public static ApiSessionRefresher Instance => Provider.Value;
    }
}