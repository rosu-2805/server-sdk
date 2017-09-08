using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Model.Commands
{
    public class FailedTaskInfo
    {

        public string TaskId { get; set; }

        public List<string> MissingParameters { get; set; }

        public string TaskApiUrl { get; set; }

        public string TaskWebUrl { get; set; }

        public string Message { get; set; }

        public FailedTaskInfo()
        {
            MissingParameters = new List<string>();
        }
    }
}
