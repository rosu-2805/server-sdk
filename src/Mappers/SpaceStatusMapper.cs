using Morph.Server.Sdk.Dto;
using Morph.Server.Sdk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Mappers
{
    internal static class SpaceStatusMapper
    {
        public static SpaceStatus MapFromDto(SpaceStatusDto dto)
        {
            return new SpaceStatus()
            {
                IsPublic = dto.IsPublic,
                SpaceName = dto.SpaceName,
                SpacePermissions = dto.UserPermissions?.Select(MapPermission)?.Where(x => x.HasValue)?.Select(x => x.Value)?.ToList().AsReadOnly()

            };
        }

        private static SpacePermission? MapPermission(string permission)
        {
            if (Enum.TryParse<SpacePermission>(permission, true, out var p))
            {
                return p;
            }
            return null;
        }


    }
}
