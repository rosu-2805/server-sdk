using System;
using System.Net.Http.Headers;

namespace Morph.Server.Sdk.Model.InternalModels
{
    /// <summary>
    /// Represents api result of DTO Model or Error (Exception)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ApiResult<T>
    {
        public virtual T Data { get; protected set; } = default(T);
        public virtual Exception Error { get; protected set; } = default(Exception);
        public virtual bool IsSucceed { get { return Error == null; } }

        public virtual HttpContentHeaders ResponseHeaders { get; protected set; } =  default(HttpContentHeaders);
        public static ApiResult<T> Fail(Exception exception, HttpContentHeaders httpContentHeaders)
        {
            return new ApiResult<T>()
            {
                Data = default(T),
                Error = exception,
                ResponseHeaders = httpContentHeaders
            };

        }

        public static ApiResult<T> Ok(T data, HttpContentHeaders httpContentHeaders)
        {
            return new ApiResult<T>()
            {
                Data = data,
                Error = null,
                ResponseHeaders = httpContentHeaders
            };
        }

        public virtual void ThrowIfFailed()
        {
            if (!IsSucceed && Error != null)
            {
                throw Error;
            }
        }
    }


}


