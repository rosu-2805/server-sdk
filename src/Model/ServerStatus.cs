using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Model
{
    
    public class ServerStatus
    {
    
        public ServerStatusCode StatusCode { get; set; }
    
        public string StatusMessage { get; set; }
    
        public Version Version { get; set; }
    }


    public enum ServerStatusCode
    {
        Unknown,
        OK,
        NoLicense
    }
  

    
}
