using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Dto
{
    [DataContract]
    internal class LoginRequestDto
    {
        [DataMember(Name = "requestToken")]
        public string RequestToken { get; set; }
        [DataMember(Name = "clientSeed")]
        public string ClientSeed { get; set; }
        [DataMember(Name = "password")]
        public string Password{ get; set; }
        [DataMember(Name = "userName")]
        public string UserName { get; set; }
        [DataMember(Name = "provider")]
        public string Provider { get; set; }        
    }
}
