using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Dto.SpaceFilesSearch
{

    [DataContract] 
    internal sealed class SpaceFilesQuickSearchResponseDto

    {

        [DataMember(Name = "hasMore")]
        public bool HasMore { get; set; }

        [DataMember(Name = "values")]
        public FoundSpaceFolderItemDto[] Values { get; set; }


    }
}
