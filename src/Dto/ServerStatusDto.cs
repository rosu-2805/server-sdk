using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Dto
{
    [DataContract]
    internal class ServerStatusDto
    {
        [DataMember(Name = "statusCode")]
        public string StatusCode { get; set; }
        [DataMember(Name = "statusMessage")]
        public string StatusMessage { get; set; }
        [DataMember(Name = "version")]
        public string Version { get; set; }
        [DataMember(Name = "instanceRunId")]
        public string InstanceRunId { get; set; }
    }
}
