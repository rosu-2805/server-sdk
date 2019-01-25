using System;

namespace Morph.Server.Sdk.Client
{
    public class ApiResult<T>
    {
        public T Data { get; set; } = default(T);
        public Exception Error { get; set; } = default(Exception);
        public bool IsSucceed { get { return Error == null; } }
        public static ApiResult<T> Fail(Exception exception)
        {
            return new ApiResult<T>()
            {
                Data = default(T),
                Error = exception
            };
        }

        public static ApiResult<T> Ok(T data)
        {
            return new ApiResult<T>()
            {
                Data = data,
                Error = null
            };
        }

        public void ThrowIfFailed()
        {
            if (!IsSucceed && Error != null)
            {
                throw Error;
            }
        }
    }


}


