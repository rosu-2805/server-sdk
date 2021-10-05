using Morph.Server.Sdk.Dto;
using Morph.Server.Sdk.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Morph.Server.Sdk.Mappers
{
    internal static class SpaceTaskMapper
    {

       
        public static SpaceTasksListItem MapItem(TaskShortDto dto)
        {
            return new SpaceTasksListItem
            {
                Enabled = dto.Enabled,
                Id = Guid.Parse(dto.Id),
                TaskName = dto.Name,
                Note = dto.Note,
                ProjectPath = dto.ProjectFile
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
