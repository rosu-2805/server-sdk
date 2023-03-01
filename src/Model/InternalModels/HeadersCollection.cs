using System;
using System.Net.Http.Headers;
using System.Collections.Generic;

namespace Morph.Server.Sdk.Model.InternalModels
{
    public class HeadersCollection
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

        /// <summary>
        /// Gets the value of the header with the specified name.
        /// </summary>
        /// <param name="headerName"></param>
        /// <returns></returns>
        public string GetValueOrDefault(string headerName) => _headers.TryGetValue(headerName, out var value) ? value : null;

        /// <summary>
        /// Sets the value of the header with the specified name.
        /// </summary>
        /// <param name="headerName">Header name.</param>
        /// <param name="headerValue">Header value.</param>
        public void Set(string headerName, string headerValue)
        {
            _headers[headerName] = headerValue;
        }

        /// <summary>
        ///  Checks if the header with the specified name exists.
        /// </summary>
        /// <param name="header">Header name.</param>
        /// <returns></returns>
        public bool Contains(string header)
        {
            return _headers.ContainsKey(header);
        }
    }


}