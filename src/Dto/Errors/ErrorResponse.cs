using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Dto.Errors
{

    [DataContract]
    internal class ErrorResponse
    {
        [DataMember]
        public Error error { get; set; }
    }
   
}
