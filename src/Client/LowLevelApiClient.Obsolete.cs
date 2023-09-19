using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Morph.Server.Sdk.Dto;
using Morph.Server.Sdk.Events;
using Morph.Server.Sdk.Helper;
using Morph.Server.Sdk.Model;
using Morph.Server.Sdk.Model.InternalModels;

namespace Morph.Server.Sdk.Client
{
    internal partial class LowLevelApiClient : ILowLevelApiClient
    {
        
        [Obsolete("Obsolete due to flaw in response checking. Use WebFilesPushPostFileStreamAsync instead.")]
        public Task<ApiResult<ServerPushStreaming>> WebFilesOpenContiniousPostStreamAsync(ApiSession apiSession, string serverFolder, string fileName, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }
            var spaceName = apiSession.SpaceName;
            var url = UrlHelper.JoinUrl("space", spaceName, "files", serverFolder);

            return apiClient.PushContiniousStreamingDataAsync<NoContentResult>(HttpMethod.Post, url, new ContiniousStreamingRequest(fileName), null, apiSession.ToHeadersCollection(), cancellationToken);
        }

        [Obsolete("Obsolete due to flaw in response checking. Use WebFilesPushPutFileStreamAsync instead.")]
        public Task<ApiResult<ServerPushStreaming>> WebFilesOpenContiniousPutStreamAsync(ApiSession apiSession, string serverFolder, string fileName, CancellationToken cancellationToken)
        {
            if (apiSession == null)
            {
                throw new ArgumentNullException(nameof(apiSession));
            }
            var spaceName = apiSession.SpaceName;
            var url = UrlHelper.JoinUrl("space", spaceName, "files", serverFolder);

            return apiClient.PushContiniousStreamingDataAsync<NoContentResult>(HttpMethod.Put, url, new ContiniousStreamingRequest(fileName), null, apiSession.ToHeadersCollection(), cancellationToken);
        }
    }
}