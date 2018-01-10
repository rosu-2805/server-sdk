using Morph.Server.Sdk.Dto;
using Morph.Server.Sdk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Mappers
{
    internal static class SpacesListMapper
    {
        public static SpacesList MapFromDto(SpacesListDto dto)
        {
            return new SpacesList()
            {
                Items = dto.Values?.Select(Map)?.ToList()                
            };
        }

       
        private static SpaceListItem Map(SpaceListItemDto dto)
        {
            return new SpaceListItem
            {
                IsPublic = dto.IsPublic,
                SpaceName = dto.SpaceName
            };
        }
    }


    internal static class SpaceTasksListsMapper
    {
        public static SpaceTasksList MapFromDto(SpaceTasksListDto dto)
        {
            return new SpaceTasksList()
            {
                Items = dto.Values?.Select(SpaceTaskMapper.MapItem)?.ToList()
            };
        }       
    }

    internal static class SpaceTaskMapper
    {
        public static SpaceTasksListItem MapItem(SpaceTasksListItemDto dto)
        {
            return new SpaceTasksListItem
            {
                Enabled = dto.Enabled,
                Id = Guid.Parse(dto.JobId),
                IsRunning = dto.IsRunning,
                Name = dto.Name,
                Note = dto.Note,
                ProjectPath = dto.ProjectFile,
                StatusText = dto.StatusText,
                TaskState = TaskStatusMapper.ParseTaskState(dto.Status)
            };
        }

        public static SpaceTasksListItem MapFull(SpaceTasksListItemDto dto)
        {
            return new SpaceTasksListItem
            {
                Enabled = dto.Enabled,
                Id = Guid.Parse(dto.JobId),
                IsRunning = dto.IsRunning,
                Name = dto.Name,
                Note = dto.Note,
                ProjectPath = dto.ProjectFile,
                StatusText = dto.StatusText,
                TaskState = TaskStatusMapper.ParseTaskState(dto.Status)
            };
        }
    }
}
