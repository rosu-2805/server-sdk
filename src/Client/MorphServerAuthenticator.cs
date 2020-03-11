using Morph.Server.Sdk.Dto;
using Morph.Server.Sdk.Helper;
using Morph.Server.Sdk.Model;
using Morph.Server.Sdk.Model.InternalModels;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Client
{
    internal static class MorphServerAuthenticator
    {

        public static async Task<ApiSession> OpenSessionMultiplexedAsync(
            SpaceEnumerationItem desiredSpace,
            OpenSessionAuthenticatorContext context,
            OpenSessionRequest openSessionRequest,             
            CancellationToken cancellationToken)
        {
            // space access restriction is supported since server 3.9.2
            // for previous versions api will return SpaceAccessRestriction.NotSupported 
            // a special fall-back mechanize need to be used to open session in such case
            switch (desiredSpace.SpaceAccessRestriction)
            {
                // anon space
                case SpaceAccessRestriction.None:
                    return ApiSession.Anonymous(context.MorphServerApiClient, openSessionRequest.SpaceName);

                // password protected space                
                case SpaceAccessRestriction.BasicPassword:
                    return await OpenSessionViaSpacePasswordAsync(context, openSessionRequest.SpaceName, openSessionRequest.Password, cancellationToken);

                // windows authentication
                case SpaceAccessRestriction.WindowsAuthentication:
                    return await OpenSessionViaWindowsAuthenticationAsync(context, openSessionRequest.SpaceName, cancellationToken);

                // fallback
                case SpaceAccessRestriction.NotSupported:

                    //  if space is public or password is not set - open anon session
                    if (desiredSpace.IsPublic || string.IsNullOrWhiteSpace(openSessionRequest.Password))
                    {
                        return ApiSession.Anonymous(context.MorphServerApiClient, openSessionRequest.SpaceName);
                    }
                    // otherwise open session via space password
                    else
                    {
                        return await OpenSessionViaSpacePasswordAsync(context, openSessionRequest.SpaceName, openSessionRequest.Password, cancellationToken);
                    }

                default:
                    throw new Exception("Space access restriction method is not supported by this client.");
            }
        }


        static async Task<ApiSession> OpenSessionViaWindowsAuthenticationAsync(OpenSessionAuthenticatorContext context, string spaceName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(spaceName))
            {
                throw new ArgumentException("Space name is not set", nameof(spaceName));
            }
            // handler will be disposed automatically
            HttpClientHandler aHandler = new HttpClientHandler()
            {
                ClientCertificateOptions = ClientCertificateOption.Automatic,
                // required for automatic NTML/Negotiate challenge
                UseDefaultCredentials = true,
#if NETSTANDARD2_0
                ServerCertificateCustomValidationCallback = context.MorphServerApiClient.Config.ServerCertificateCustomValidationCallback        
#endif


    };

            // build a new low level client based on specified handler
            using (var ntmlRestApiClient = context.BuildApiClient(aHandler))
            {
                var serverNonce = await internalGetAuthNonceAsync(ntmlRestApiClient, cancellationToken);
                var token = await internalAuthExternalWindowAsync(ntmlRestApiClient, spaceName, serverNonce, cancellationToken);

                return new ApiSession(context.MorphServerApiClient)
                {
                    AuthToken = token,
                    IsAnonymous = false,
                    IsClosed = false,
                    SpaceName = spaceName
                };
            }
        }
        static async Task<string> internalGetAuthNonceAsync(IRestClient apiClient, CancellationToken cancellationToken)
        {
            var url = "auth/nonce";
            var response = await apiClient.PostAsync<GenerateNonceRequestDto, GenerateNonceResponseDto>
                (url, new GenerateNonceRequestDto(), null, new HeadersCollection(), cancellationToken);
            response.ThrowIfFailed();
            return response.Data.Nonce;            
        }

        static async Task<string> internalAuthExternalWindowAsync(IRestClient apiClient, string spaceName, string serverNonce, CancellationToken cancellationToken)
        {
            var url = "auth/external/windows";
            var requestDto = new WindowsExternalLoginRequestDto
            {
                RequestToken = serverNonce,
                SpaceName = spaceName
            };

            var apiResult = await apiClient.PostAsync<WindowsExternalLoginRequestDto, LoginResponseDto>(url, requestDto, null, new HeadersCollection(), cancellationToken);
            apiResult.ThrowIfFailed();
            return apiResult.Data.Token;
            
        }



        /// <summary>
        /// Open a new authenticated session via password
        /// </summary>
        /// <param name="spaceName">space name</param>
        /// <param name="password">space password</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        static async Task<ApiSession> OpenSessionViaSpacePasswordAsync(OpenSessionAuthenticatorContext context, string spaceName, string password, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(spaceName))
            {
                throw new ArgumentException("Space name is not set.", nameof(spaceName));
            }

            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }

            var passwordSha256 = CryptographyHelper.CalculateSha256HEX(password);            
            var serverNonceApiResult = await context.LowLevelApiClient.AuthGenerateNonce(cancellationToken);
            serverNonceApiResult.ThrowIfFailed();
            var serverNonce = serverNonceApiResult.Data.Nonce;
            var clientNonce = ConvertHelper.ByteArrayToHexString(CryptographyHelper.GenerateRandomSequence(16));
            var all = passwordSha256 + serverNonce + clientNonce;
            var composedHash = CryptographyHelper.CalculateSha256HEX(all);
            

            var requestDto = new LoginRequestDto
            {
                ClientSeed = clientNonce,
                Password = composedHash,
                Provider = "Space",
                UserName = spaceName,
                RequestToken = serverNonce
            };
            var authApiResult = await context.LowLevelApiClient.AuthLoginPasswordAsync(requestDto, cancellationToken);
            authApiResult.ThrowIfFailed();
            var token = authApiResult.Data.Token;           
            

            return new ApiSession(context.MorphServerApiClient)
            {
                AuthToken = token,
                IsAnonymous = false,
                IsClosed = false,
                SpaceName = spaceName
            };
        }



    }
}


