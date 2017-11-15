using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Dto
{
    [DataContract]
    internal class GenerateNonceResponseDto
    {
        [DataMember(Name = "nonce")]
        public string Nonce{ get; set; }
    }
}
