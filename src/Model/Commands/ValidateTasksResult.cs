using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Model.Commands
{

    public class ValidateTasksResult
    {
        /// <summary>
        /// List of failed tasks
        /// </summary>
        public List<FailedTaskInfo> FailedTasks { get; set; }
        public string Message { get; set; }

        public ValidateTasksResult()
        {
            FailedTasks = new List<FailedTaskInfo>();
        }
    }

   

}
