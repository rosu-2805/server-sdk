using System;
using Morph.Server.Sdk.Dto.Computations;
using Morph.Server.Sdk.Model;

namespace Morph.Server.Sdk.Mappers
{
    internal static class ComputationStateMapper
    {
        public static ComputationState FromDto(ComputationStateDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            switch (dto.Type)
            {
                case "starting": return new ComputationState.Starting();
                case "running": return new ComputationState.Running();
                case "stopping": return new ComputationState.Stopping();
                case "retrying": return new ComputationState.Retrying();
                case "finished": return new ComputationState.Finished(dto.Data.ResultObtainingToken);
                default:
                    return new ComputationState.Unknown(dto.Type);
            }
        }
    }
}