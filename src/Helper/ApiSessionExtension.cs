using Morph.Server.Sdk.Model;
using Morph.Server.Sdk.Model.InternalModels;

namespace Morph.Server.Sdk.Helper
{
    internal static class ApiSessionExtension
    {
        public static HeadersCollection ToHeadersCollection(this ApiSession apiSession)
        {
            var collection = new HeadersCollection();
            if (apiSession != null && !apiSession.IsAnonymous && !apiSession.IsClosed)
            {
                collection.Add(ApiSession.AuthHeaderName, apiSession.AuthToken);
            }
            return collection;
        }
    }


}



