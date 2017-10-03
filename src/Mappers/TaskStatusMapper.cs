using Morph.Server.Sdk.Dto;
using Morph.Server.Sdk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Mappers
{
    internal static class TaskStatusMapper
    {
        public static Model.TaskStatus MapFromDto(TaskStatusDto dto)
        {
            return new Model.TaskStatus()
            {
                TaskName = dto.TaskName,
                StatusText = dto.StatusText,
                TaskState = Parse(dto.Status),
                IsRunning = dto.IsRunning
            };
        }

        private static TaskState Parse(string value)
        {            
            TaskState status;
            if(value != null &&  Enum.TryParse(value, true, out status))
            {
                return status;
            }
            throw new Exceptions.ParseResponseException("Unable to parse " + value + " as valid TaskState");
        }
        
    }
}
