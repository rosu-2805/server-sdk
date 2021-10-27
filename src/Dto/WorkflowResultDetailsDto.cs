using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Morph.Server.Sdk.Dto
{
    [DataContract]
    public sealed class WorkflowResultDetailsDto
    {
        [DataMember(Name="journalEntryId")]
        public string JournalEntryId { get; set; }
        
        [DataMember(Name="journalEntryUrl")]
        public string JournalEntryUrl { get; set;}

        [DataMember(Name="finishedTime")]
        public string FinishedTime { get;set; }
        [DataMember(Name="spaceName")]
        public string SpaceName { get; set;}
        
        [DataMember(Name="result")]
        public string Result { get;set; }

        [DataMember(Name="errors")]
        public List<WorkflowResultErrorInfoDto> Errors { get; set; } = new List<WorkflowResultErrorInfoDto>();
        
    }
}