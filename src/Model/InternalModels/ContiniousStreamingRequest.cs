using System;

namespace Morph.Server.Sdk.Model.InternalModels
{
    internal class ContiniousStreamingRequest
    {
        public ContiniousStreamingRequest(string fileName)
        {
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        }

        public string FileName { get; }

        
    }



}