using Morph.Server.Sdk.Dto;
using Morph.Server.Sdk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Mappers
{
    internal static class ServerStatusMapper
    {
        public static ServerStatus MapFromDto(ServerStatusDto dto)
        {
            return new ServerStatus()
            {
                StatusMessage = dto.StatusMessage,
                Version = Version.Parse(dto.Version),
                StatusCode = Parse(dto.StatusCode),
                InstanceRunId = !String.IsNullOrWhiteSpace(dto.InstanceRunId)? Guid.Parse(dto.InstanceRunId) : new Guid?()
            };
        }

        private static ServerStatusCode Parse(string value)
        {            
            if (value == null)
                return ServerStatusCode.Unknown;
            ServerStatusCode status;
            if(Enum.TryParse(value, out status))
            {
                return status;
            }
            return ServerStatusCode.Unknown;
        }
        
    }
}
