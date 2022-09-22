using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Model
{
    /// <summary>
    /// Server status
    /// </summary>
    public class ServerStatus
    {    
        public ServerStatusCode StatusCode { get; set; }
    
        public string StatusMessage { get; set; }
    
        /// <summary>
        /// Server version
        /// </summary>
        public Version Version { get; set; }

        /// <summary>
        /// Server Instance RunId
        /// </summary>
        public Guid? InstanceRunId { get; set; } 
    }


    public enum ServerStatusCode
    {        
        OK,
        Initializing
    }
  

    
}
