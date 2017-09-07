using Morph.Server.Sdk.Dto.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Dto
{
    [DataContract]
    internal class InnerError
    {
        [DataMember]
        public string code { get; set; }
        [DataMember]
        public InnerError innererror { get; set; }
    }
}
