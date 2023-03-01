using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Morph.Server.Sdk.Dto.Errors;
using Morph.Server.Sdk.Helper;
using Morph.Server.Sdk.Model;
using Morph.Server.Sdk.Model.InternalModels;

namespace Morph.Server.Sdk.Client
{
    /// <summary>
    /// Service that provides seamless API token refresh
    /// </summary>
    public class ApiSessionRefresher : IApiSessionRefresher
    {
        /// <summary>
        /// DTO for '/user/me' endpoint. That endpoint returns more info, but here we only interested in one field to check whether
        /// we're authenticated
        /// </summary>
        [DataContract]
        private class UserMeDtoStub
        {
            [DataMember(Name = "isAuthenticated")] public bool? IsAuthenticated { get; set; }
        }

        private class Container<T>
        {
            public readonly T Value;
            public readonly DateTime Time;

            public Container(T value)
            {
                Value = value;
                Time = DateTime.UtcNow;
            }
        }

        private readonly ConcurrentDictionary<string, Container<Authenticator>> _authenticators =
            new ConcurrentDictionary<string, Container<Authenticator>>();

        private readonly ConcurrentDictionary<ApiSession, DateTime> _sessions =
            new ConcurrentDictionary<ApiSession, DateTime>();

        private readonly ConcurrentDictionary<string, Container<Task<ApiSession>>> _refreshTasks =
            new ConcurrentDictionary<string, Container<Task<ApiSession>>>();

        private readonly IJsonSerializer _serializer = new MorphDataContractJsonSerializer();

        public async Task<bool> RefreshSessionAsync(HeadersCollection headers, CancellationToken token)
        {
            var expiredSessionToken = headers.GetValueOrDefault(ApiSession.AuthHeaderName);

            if (string.IsNullOrEmpty(expiredSessionToken))
                return false;

            var authenticator = _authenticators.TryRemove(expiredSessionToken, out var value) ? value : null;

            try
            {
                // Here we utilize the natural behavior of TPL/async that fits our needs perfectly.
                // We want the first user that wants a replacement for session token 'A' to initiate re-auth process and receive 'B'
                // (for example) as a new token, but we also want subsequent requests for 'A' replacement to just return 'B',
                // without extra server-trips that would entail new tokens being generated, which in turn would result in
                // session proliferation.

                // A dictionary with Tasks and a generator would do just that:
                // first client to request new session token to replace 'A' would create an actual Task<ApiSession> that would yield
                // a new ApiSession instance, and subsequent clients would just get that same Task<ApiSession> instance, which
                // upon being awaited would yield the same ApiSession instance.

                var result = await _refreshTasks.GetOrAdd(expiredSessionToken,
                    oldToken => new Container<Task<ApiSession>>(RefreshSessionCore(oldToken, authenticator, token))).Value;

                result = UpdateSessionToRecent(result);

                if (string.IsNullOrWhiteSpace(result?.AuthToken))
                {
                    //This is not expected behavior and at least we don't want to cache 'null' session as a replacement
                    //for 'expiredSessionToken'. Remove cached task, re-add authenticator.
                    //Same logic should be applied if authenticator results in exception.

                    if (authenticator != null)
                        _authenticators.TryAdd(expiredSessionToken, authenticator);

                    _refreshTasks.TryRemove(expiredSessionToken, out _);
                    return false;
                }

                headers.Set(ApiSession.AuthHeaderName, result.AuthToken);

                foreach (var pair in _sessions.Where(s =>
                             string.Equals(s.Key?.AuthToken, expiredSessionToken, StringComparison.Ordinal)))
                {
                    pair.Key.FillFrom(result);

                    // Update last session access time.
                    _sessions.TryUpdate(pair.Key, DateTime.UtcNow, pair.Value);
                }

                return true;
            }
            catch (Exception)
            {
                //Revert authenticator to original state for possible retry.
                if (authenticator != null)
                    _authenticators.TryAdd(expiredSessionToken, authenticator);

                //Do not cache faulted/cancelled tasks
                _refreshTasks.TryRemove(expiredSessionToken, out _);
                throw;
            }
        }

        private async Task<ApiSession> RefreshSessionCore(string oldToken, Container<Authenticator> authenticator, CancellationToken token)
        {
            if (null == authenticator)
                return null;

            var freshSession = await authenticator.Value(token);

            if (null == freshSession)
                return null;

            // Find tasks that are completed and have previously returned 'oldToken' as their result and replace them with task
            // that returns our fresh session.
            //
            // This way, if we for example had token 'A' exchanged for 'B' and then 'B' got invalidated and exchanged for 'C', then
            // when something that was still holding token 'A' tries to refresh it, it will get good 'C' instead of already invalid 'B'.

            var toRedirect = _refreshTasks
                .Where(x => x.Value.Value.Status == TaskStatus.RanToCompletion &&
                            string.Equals(x.Value.Value.Result?.AuthToken, oldToken, StringComparison.Ordinal))
                .ToArray();

            foreach (var kvp in toRedirect)
                _refreshTasks.TryUpdate(kvp.Key, new Container<Task<ApiSession>>(Task.FromResult(freshSession)), kvp.Value);

            return freshSession;
        }


        /// <summary>
        /// Given session instance, returns most recent replacement for it, if any.
        /// </summary>
        /// <param name="source">Session that was retrieved as re-auth result</param>
        /// <returns>Updated session or original <see cref="source"/></returns>
        private ApiSession UpdateSessionToRecent(ApiSession source)
        {
            if (string.IsNullOrWhiteSpace(source?.AuthToken))
                return source;

            // There is (some) possibility that during the slow~ish resolution of session A->B another concurrent
            // request had already exchanged B for C. In this case we would like the request for A to get the most recent
            // session token 'C', not 'B'.

            // UML sequence diagram below to clarify desired behavior.
            // Both Client 1 and 2 start with shared session token 'A' and then both want to refresh it ⬇
            //
            //     ┌───────┐           ┌────────────────┐                        ┌───────┐
            //     │Client1│           │SessionRefresher│                        │Client2│
            //     └───┬───┘           └───────┬────────┘                        └───┬───┘
            //         │      refresh 'A'      │                                     │
            //         │───────────────────────>                                     │
            //         │                       │                                     │
            //         │                       │             refresh 'A'             │
            //         │                       │ <───────────────────────────────────│
            //         │                       │                                     │
            //         │take 'B' instead of 'A'│                                     │
            //         │<───────────────────────                                     │
            //         │                       │                                     │
            //         │      refresh 'B'      │                                     │
            //         │───────────────────────>                                     │
            //         │                       │                                     │
            //         │take 'C' instead of 'B'│                                     │
            //         │<───────────────────────                                     │
            //         │                       │                                     │
            //         │                       │ should be 'C', not 'B' or (new) 'D' │
            //         │                       │ ───────────────────────────────────>│
            //
            //
            // Code below checks whether there are tasks that have already completed and were stemmed from the
            // session token we're trying to refresh. If there are, we take the most recent one and return it.

            return _refreshTasks
                .Where(x => x.Value.Value.Status == TaskStatus.RanToCompletion && x.Key == source?.AuthToken)
                .Select(x => x.Value.Value.Result).FirstOrDefault() ?? source;
        }

        public void AssociateAuthenticator(ApiSession session, Authenticator authenticator)
        {
            if (null == session?.AuthToken)
                return;

            _authenticators.TryAdd(session.AuthToken, new Container<Authenticator>(authenticator));
            _sessions.TryAdd(session, DateTime.UtcNow);

            PruneCache();
        }

        private void PruneCache()
        {
            var removeBefore = DateTime.UtcNow - TimeSpan.FromHours(5);

            // Remove everything that was last touched 5 hours ago.

            foreach (var pair in _authenticators.Where(c => c.Value.Time < removeBefore))
                _authenticators.TryRemove(pair.Key, out _);

            foreach (var pair in _refreshTasks.Where(c => c.Value.Time < removeBefore))
                _refreshTasks.TryRemove(pair.Key, out _);

            foreach (var pair in _sessions.Where(c => c.Value < removeBefore))
                _sessions.TryRemove(pair.Key, out _);
        }

        public async Task<bool> IsSessionLostResponse(HeadersCollection headersCollection, string path, HttpContent httpContent, HttpResponseMessage response)
        {
            // Session-lost or expired errors have 403 error code, not 401, due to some EMS/http.sys error handling workarounds
            if (response.StatusCode != System.Net.HttpStatusCode.Forbidden)
                return false;

            // Anonymous sessions are not refreshable.
            if (!headersCollection.Contains(ApiSession.AuthHeaderName))
                return false;

            // Can't refresh if something fails during refresh.
            if (path.StartsWith("auth/", StringComparison.OrdinalIgnoreCase))
                return false;

            //Cannot retry if request had streaming content (can't rewind it).
            if (httpContent is StreamContent)
                return false;

            if (httpContent is MultipartContent multipartContent)
            {
                if (multipartContent.Any(part =>
                        part is ContiniousSteamingHttpContent
                        || part is StreamContent
                        || part is ProgressStreamContent))
                    return false;
            }

            // Session-lost error body should be json
            if(!string.Equals(response.Content?.Headers.ContentType.MediaType, "application/json", StringComparison.OrdinalIgnoreCase))
                return false;

            var content = await response.Content.ReadAsStringAsync();

            if(string.IsNullOrWhiteSpace(content))
                return false;

            try
            {
                var responseModel = _serializer.Deserialize<ErrorResponse>(content);

                return string.Equals(responseModel?.error.code, ReadableErrorTopCode.Unauthorized, StringComparison.Ordinal);
            }
            catch (Exception)
            {
                //Not a valid JSON or doesn't match error schema - not our case
                return false;
            }
        }

        public async Task EnsureSessionValid(MorphServerRestClient restClient, HeadersCollection headersCollection,
            CancellationToken token)
        {
            //This is an anonymous session -- we can't refresh it.
            if (!headersCollection.Contains(ApiSession.AuthHeaderName))
                return;

            var userMe = await restClient.GetAsync<UserMeDtoStub>(UrlHelper.JoinUrl("user", "me"), null, headersCollection, token);

            if (userMe?.Data.IsAuthenticated == true)
                return;

            await RefreshSessionAsync(headersCollection, token);
        }


    }
}