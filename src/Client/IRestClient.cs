using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.IO;
using Morph.Server.Sdk.Model.InternalModels;
using Morph.Server.Sdk.Events;
using Morph.Server.Sdk.Model;

namespace Morph.Server.Sdk.Client
{
    public delegate Task PushStreamCallback(PushStreamingConnection sink, CancellationToken token);

    /// <summary>
    ///  Additional data for sending with file stream as multipart/form-data
    /// </summary>
    public class FormValueData
    {
        public class FormValueItem
        {
            public string Name { get; set; }
            public bool ShouldBeSerialized { get; set; }
            public object Payload { get; set; }
        }
        
        private readonly List<FormValueItem> _values = new List<FormValueItem>();
        
        /// <summary>
        /// Add string value to form data
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <exception cref="ArgumentException"></exception>
        public FormValueData Add(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Value cannot be null or empty.", nameof(key));
            
            _values.Add(new FormValueItem { Name = key, ShouldBeSerialized = false, Payload = value });

            return this;
        }

        /// <summary>
        /// Add DTO to form data, will be serialized to json
        /// </summary>
        /// <param name="key"></param>
        /// <param name="dto"></param>
        /// <typeparam name="TDto"></typeparam>
        /// <exception cref="ArgumentException"></exception>
        public FormValueData AddDto<TDto>(string key, TDto dto)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Value cannot be null or empty.", nameof(key));
            
            _values.Add(new FormValueItem { Name = key, ShouldBeSerialized = true, Payload = dto });

            return this;
        }

        /// <summary>
        /// Get all values
        /// </summary>
        /// <returns></returns>
        public IEnumerable<FormValueItem> GetValues() => _values;
        
        public static FormValueData OfDto<TDto>(string key, TDto value) => new FormValueData().AddDto(key, value);
        public static FormValueData Of(string key, string value) => new FormValueData().Add(key, value);
    }
    
    public interface IRestClient : IDisposable
    {

        HttpSecurityState HttpSecurityState { get; }
        HttpClient HttpClient { get; }
        Task<ApiResult<TResult>> GetAsync<TResult>(string url, NameValueCollection urlParameters, HeadersCollection headersCollection, CancellationToken cancellationToken)
          where TResult : new();
        Task<ApiResult<TResult>> HeadAsync<TResult>(string url, NameValueCollection urlParameters, HeadersCollection headersCollection, CancellationToken cancellationToken)
          where TResult : new();
        Task<ApiResult<TResult>> PostAsync<TModel, TResult>(string url, TModel model, NameValueCollection urlParameters, HeadersCollection headersCollection, CancellationToken cancellationToken)
          where TResult : new();
        Task<ApiResult<TResult>> PutAsync<TModel, TResult>(string url, TModel model, NameValueCollection urlParameters, HeadersCollection headersCollection, CancellationToken cancellationToken)
              where TResult : new();
        Task<ApiResult<TResult>> DeleteAsync<TResult>(string url, NameValueCollection urlParameters, HeadersCollection headersCollection, CancellationToken cancellationToken)
          where TResult : new();
        Task<ApiResult<TResult>> PutFileStreamAsync<TResult>(string url, SendFileStreamData sendFileStreamData, NameValueCollection urlParameters, HeadersCollection headersCollection, Action<FileTransferProgressEventArgs> onSendProgress, CancellationToken cancellationToken)
              where TResult : new();
        Task<ApiResult<TResult>> PostFileStreamAsync<TResult>(string url, SendFileStreamData sendFileStreamData, NameValueCollection urlParameters, HeadersCollection headersCollection, Action<FileTransferProgressEventArgs> onSendProgress, CancellationToken cancellationToken)
              where TResult : new();

        Task<ApiResult<FetchFileStreamData>> RetrieveFileGetAsync(string url, NameValueCollection urlParameters, HeadersCollection headersCollection, Action<FileTransferProgressEventArgs> onReceiveProgress, CancellationToken cancellationToken);


        Task<ApiResult<TResult>> PushStreamAsync<TResult>(HttpMethod httpMethod, string path,
            PushFileStreamData pushFileStreamData, NameValueCollection urlParameters,
            HeadersCollection headersCollection, CancellationToken cancellationToken)
            where TResult : new();
        
        Task<ApiResult<TResult>> PushStreamAsync<TResult>(HttpMethod httpMethod, string path,
            PushFileStreamData pushFileStreamData, NameValueCollection urlParameters,
            HeadersCollection headersCollection, FormValueData formValueData,
            CancellationToken cancellationToken)
            where TResult : new();
        
        
        [Obsolete("Obsolete due to flaw in response checking. Will be removed in the next major release.")]
        Task<ApiResult<ServerPushStreaming>> PushContiniousStreamingDataAsync<TResult>(
            HttpMethod httpMethod, string path, ContiniousStreamingRequest startContiniousStreamingRequest, NameValueCollection urlParameters, HeadersCollection headersCollection,
            CancellationToken cancellationToken)
            where TResult : new();


    }



}