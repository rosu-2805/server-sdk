using Morph.Server.Sdk.Model;

namespace Morph.Server.Sdk.Client
{
    public static class ApiSessionExtension
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



