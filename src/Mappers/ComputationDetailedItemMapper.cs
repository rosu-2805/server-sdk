using System;
using System.Collections.Generic;
using System.Linq;
using Morph.Server.Sdk.Dto;
using Morph.Server.Sdk.Model;

namespace Morph.Server.Sdk.Mappers
{
    internal static class WorkflowResultDetailsMapper
    {
        public static WorkflowResultDetails FromDto(WorkflowResultDetailsDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            return new WorkflowResultDetails()
            {
                Errors = dto.Errors?.Select(WorkflowResultErrorInfoMapper.FromDto)?.ToList()
            };
        }
    }
    
    
    internal static class ComputationDetailedItemMapper
    {
        public static ComputationDetailedItem FromDto(ComputationDetailedItemDto dto)
        {
            return new ComputationDetailedItem
            {
                ComputationId = dto.ComputationId,
                ProjectDetails = ProjectDetailsMapper.FromDto(dto.projectDetails),
                State = ComputationStateMapper.FromDto(dto.State),
                SpaceName = dto.SpaceName,
                StartTimestamp = DateTime.Parse(dto.StartTimestamp),
                StatusText = dto.StatusText
            };
        }
    }
}