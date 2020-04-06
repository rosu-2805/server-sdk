using Morph.Server.Sdk.Dto;
using Morph.Server.Sdk.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Morph.Server.Sdk.Mappers
{
    internal static class SpacesLookupMapper
    {
        public static SpacesLookupResponse MapFromDto(SpacesLookupResponseDto dto)
        {
            return new SpacesLookupResponse()
            {
                Values = dto.Values?.Select(Map)?.ToList()
            };
        }
        private static SpacesLookupResult Map(SpacesLookupResultDto dto)
        {
            var result = new SpacesLookupResult
            {
                SpaceName = dto.SpaceName
            };
            if (dto.Data != null)
            {
                result.Data = SpacesEnumerationMapper.MapItemFromDto(dto.Data);
            };
            if (dto.Error != null)
            {
                result.Error = ErrorModelMapper.MapFromDto(dto.Error);
            }
            return result;
        }

        internal static SpacesLookupRequestDto ToDto(SpacesLookupRequest request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return new SpacesLookupRequestDto
            {
                SpaceNames = request.SpaceNames?.ToList() ?? (new List<string>())
            };
        } 
    }

}
