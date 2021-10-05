using System;
using Morph.Server.Sdk.Mappers;

namespace Morph.Server.Sdk.Model
{


    public enum WorkflowResultCode
    {
        //"Success"
        Success,
        //"Failure"
        Failure ,
        // "TimedOut"
        TimedOut,
        // "Canceled.By_User"
        CanceledByUser,
        
    }


    public class ComputationDetailedItem
    {
        
        
        public string ComputationId { get; set; }
        
        public DateTime StartTimestamp { get; set; }
        
        public string SpaceName { get; set; }
        
        public ProjectDetailsInfo ProjectDetails { get; set; }
        
        public ComputationState State { get; set; }
        
        public string StatusText { get; set; }
        
        
        
        
        //public List<ErrorInfo> Errors { get; set; } = new List<ErrorInfo>();

    }
}
