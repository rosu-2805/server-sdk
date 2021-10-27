using System;
using System.Collections.Generic;

namespace Morph.Server.Sdk.Model
{
    public class WorkflowResultDetails
    {
       
        public string JournalEntryId { get; set; }

        public string JournalEntryUrl { get; set;}

        public DateTime FinishedTime { get;set; }
        
        public string SpaceName { get; set;}

        public WorkflowResultCode Result { get;set; }

        public List<ErrorInfo> Errors { get; set; } = new List<ErrorInfo>();
    }
}