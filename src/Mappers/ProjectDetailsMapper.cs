using System;
using Morph.Server.Sdk.Dto;
using Morph.Server.Sdk.Model;

namespace Morph.Server.Sdk.Mappers
{
    internal static class ProjectDetailsMapper
    {
        public static ProjectDetailsInfo FromDto(ProjectDetailsInfoDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            return new ProjectDetailsInfo()
            {
                ProjectName = dto.ProjectName,
                ProjectPath = dto.ProjectPath,
                ProjectLastEdited = !string.IsNullOrEmpty(dto.ProjectLastEdited)
                    ? DateTime.Parse(dto.ProjectLastEdited)
                    : (DateTime?) null
            };
        }
    }
}