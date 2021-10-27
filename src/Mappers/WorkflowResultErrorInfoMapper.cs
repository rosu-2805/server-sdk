using Morph.Server.Sdk.Dto;
using Morph.Server.Sdk.Model;

namespace Morph.Server.Sdk.Mappers
{
    internal static class WorkflowResultErrorInfoMapper{
        public static ErrorInfo FromDto(WorkflowResultErrorInfoDto dto)
        {
            return new ErrorInfo
            {
                Description = dto.Description,
                Location = dto.Location
            };
        }
    
    }
}