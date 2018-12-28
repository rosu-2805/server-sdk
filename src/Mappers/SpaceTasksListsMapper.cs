using Morph.Server.Sdk.Dto;
using Morph.Server.Sdk.Model;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Mappers
{


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
}
