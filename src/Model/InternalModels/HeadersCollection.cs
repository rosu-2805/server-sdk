using System;
using System.Net.Http.Headers;
using System.Collections.Generic;

namespace Morph.Server.Sdk.Model.InternalModels
{
    internal class HeadersCollection
    {
        private Dictionary<string, string> _headers = new Dictionary<string, string>();
        public HeadersCollection()
        {

        }
        

        public void Add(string header, string value)
        {
            if (header == null)
            {
                throw new ArgumentNullException(nameof(header));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            _headers[header] = value;
        }

        public void Fill(HttpRequestHeaders reqestHeaders)
        {
            if (reqestHeaders == null)
            {
                throw new ArgumentNullException(nameof(reqestHeaders));
            }
            foreach (var item in _headers)
            {
                reqestHeaders.Add(item.Key, item.Value);
            }
        }
    }


}



