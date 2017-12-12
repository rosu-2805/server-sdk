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
}
