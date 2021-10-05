using Morph.Server.Sdk.Dto;
using Morph.Server.Sdk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Mappers
{
    // internal static class TaskStatusMapper
    // {
    //     public static Model.TaskStatus MapFromDto(TaskStatusDto dto)
    //     {
    //         return new Model.TaskStatus()
    //         {
    //             TaskName = dto.TaskName,
    //             StatusText = dto.StatusText,
    //             TaskState = ParseTaskState(dto.Status),
    //             IsRunning = dto.IsRunning,
    //             Errors = dto.Errors?.Select(SpaceTaskMapper.MapFromRunningTaskErrorInfoDto)?.ToList() ?? new List<ErrorInfo>()
    //         };
    //     }
    //
    //     internal static TaskState ParseTaskState(string value)
    //     {            
    //         TaskState status;
    //         if(value != null &&  Enum.TryParse(value, true, out status))
    //         {
    //             return status;
    //         }
    //         throw new Exceptions.ResponseParseException("Unable to parse " + value + " as valid TaskState");
    //     }
    //     
    // }
}
