using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Morph.Server.Sdk.Model;
using Morph.Server.Sdk.Model.InternalModels;

namespace Morph.Server.Sdk.Client
{

    /// <summary>
    /// Delegate that upon being invoked performs server-side authentication and returns a fresh valid API Session
    /// </summary>
    public delegate Task<ApiSession> Authenticator(CancellationToken token);

    /// <summary>
    /// Service that provides seamless API token refresh
    /// </summary>
    public interface IApiSessionRefresher
    {
        /// <summary>
        /// Re-authenticates current session (if any) and seamlessly updates existing <see cref="ApiSession"/> instances that were associated
        /// with the previous session via <see cref="AssociateAuthenticator"/> method.
        /// </summary>
        /// <param name="headers">Request headers</param>
        /// <param name="token">Cancellation token</param>
        /// <returns></returns>
        Task<bool> RefreshSessionAsync(HeadersCollection headers, CancellationToken token);

        /// <summary>
        /// Save <see cref="authenticator"/> for <see cref="session"/>. T
        /// his is required for <see cref="ApiSession"/> object tracking and for <see cref="RefreshSessionAsync"/> to work.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="authenticator"></param>
        void AssociateAuthenticator(ApiSession session, Authenticator authenticator);

        /// <summary>
        /// Returns true if <see cref="response"/> indicates that the session has expired
        /// </summary>
        /// <param name="headersCollection"></param>
        /// <param name="path"></param>
        /// <param name="httpContent"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        Task<bool> IsSessionLostResponse(HeadersCollection headersCollection, string path, HttpContent httpContent, HttpResponseMessage response);

        /// <summary>
        ///     Ensures that current session is valid and refreshes it if needed
        /// </summary>
        /// <param name="restClient"></param>
        /// <param name="headersCollection"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task EnsureSessionValid(MorphServerRestClient restClient, HeadersCollection headersCollection, CancellationToken token);
    }
}