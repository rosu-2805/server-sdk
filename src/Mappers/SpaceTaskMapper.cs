using Morph.Server.Sdk.Dto;
using Morph.Server.Sdk.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Morph.Server.Sdk.Mappers
{
    internal static class SpaceTaskMapper
    {
        private static readonly TaskSchedule NoSchedule = new TaskSchedule
        {
            ScheduleType = "NoSchedule",
            ScheduleDescription = "Not scheduled"
        };
       
        public static SpaceTasksListItem MapItem(TaskShortDto dto)
        {
            return new SpaceTasksListItem
            {
                Enabled = dto.Enabled,
                Id = Guid.Parse(dto.Id),
                TaskName = dto.Name,
                Note = dto.Note,
                ProjectPath = dto.ProjectFile,
                Schedules = dto.Schedules?
                    .Select(sdto => new TaskSchedule { ScheduleType = sdto.ScheduleType, ScheduleDescription = sdto.ScheduleAsText })
                    .ToArray() ?? new[] { NoSchedule }
            };
        }

        public static SpaceTask MapFull(TaskFullDto fullDto)
        {
            var rsult =  new SpaceTask
            {
                Enabled = fullDto.Enabled,
                Id = Guid.Parse(fullDto.Id),
                TaskName = fullDto.Name,
                Note = fullDto.Note,
                ProjectPath = fullDto.ProjectFile,
            };
            if(fullDto.Parameters  != null)
            {
                rsult.TaskParameters = fullDto.Parameters.Select(TaskParameterMapper.FromDto).ToList();
            }
            return rsult;            
        }
    }
}