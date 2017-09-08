using Morph.Server.Sdk.Dto;
using Morph.Server.Sdk.Dto.Commands;
using Morph.Server.Sdk.Model;
using Morph.Server.Sdk.Model.Commands;
using Morph.Server.Sdk.Model.Errors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Mappers
{
    internal static class ValidateTasksResponseMapper
    {
        public static ValidateTasksResult MapFromDto(ValidateTasksResponseDto dto)
        {
            return new ValidateTasksResult()
            {
                FailedTasks = dto.FailedTasks?.Select(Map).ToList(),
                Message = dto.Message                
            };
        }

        private static FailedTaskInfo Map(FailedTaskInfoDto dto)
        {
            return new FailedTaskInfo
            {
                MissingParameters = dto.MissingParameters,
                TaskId = dto.TaskId,
                TaskWebUrl = dto.TaskApiUrl,
                TaskApiUrl = dto.TaskWebUrl,
                Message = dto.Message
            };
        }
       
    }
}
