using System;
using System.Collections.Generic;

namespace Morph.Server.Sdk.Model
{
    public class RunningTaskStatus
    {
        public Guid Id { get; set; }
        public bool IsRunning { get; set; }
        public string ProjectName { get; set; }
        public List<ErrorInfo> Errors { get; set; } = new List<ErrorInfo>();

    }
    public class ErrorInfo
    {
        public string Description { get; set; }
        public string Location { get; set; }
    }


}
