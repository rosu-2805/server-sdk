using Morph.Server.Sdk.Dto;
using Morph.Server.Sdk.Model;
using System;
using System.Linq;

namespace Morph.Server.Sdk.Mappers
{
    internal static class SpacesEnumerationMapper
    {
        public static SpacesEnumerationList MapFromDto(SpacesEnumerationDto dto)
        {
            return new SpacesEnumerationList()
            {
                Items = dto.Values?.Select(Map)?.ToList()                
            };
        }

       
        private static SpaceEnumerationItem Map(SpaceEnumerationItemDto dto)
        {
            return new SpaceEnumerationItem
            {
                IsPublic = dto.IsPublic,
                SpaceName = dto.SpaceName,
                SpaceAccessRestriction = ParseSpaceAccessRestriction(dto.SpaceAccessRestriction)
            };
        }
        internal static SpaceAccessRestriction ParseSpaceAccessRestriction(string value)
        {
            SpaceAccessRestriction parsed;
            if (value != null && Enum.TryParse(value, true, out parsed))
            {
                return parsed;
            }
            else
            {
                return SpaceAccessRestriction.NotSupported;
            }
        }
    }
}
