using Morph.Server.Sdk.Dto.SpaceFilesSearch;
using Morph.Server.Sdk.Model;
using System;

namespace Morph.Server.Sdk.Mappers
{
    internal static class SpaceFilesQuickSearchRequestMapper
    {
        internal static SpaceFilesQuickSearchRequestDto ToDto(SpaceFilesQuickSearchRequest request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return new SpaceFilesQuickSearchRequestDto
            {
                FileExtensions = request.FileExtensions ?? Array.Empty<string>(),
                FolderPath = request.FolderPath,
                LookupString = request.LookupString
            };
        }

    }
}
